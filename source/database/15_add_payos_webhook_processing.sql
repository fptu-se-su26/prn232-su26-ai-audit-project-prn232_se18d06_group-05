-- Adds the atomic, idempotent server-side transition used by the verified
-- PayOS webhook. Run after 12_add_payment_lifecycle.sql (and after 13/14 when
-- applying the complete lifecycle migration set).

BEGIN;

CREATE UNIQUE INDEX IF NOT EXISTS ux_payments_provider_transaction_id
    ON public.payments (payment_method, provider_transaction_id)
    WHERE provider_transaction_id IS NOT NULL;

CREATE OR REPLACE FUNCTION public.process_payos_payment_webhook(
    p_order_code text,
    p_amount numeric,
    p_currency text,
    p_transaction_id text,
    p_paid_at timestamp with time zone,
    p_payload jsonb DEFAULT '{}'::jsonb
)
RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_payment public.payments%ROWTYPE;
    v_booking public.bookings%ROWTYPE;
    v_total_paid numeric(14,2);
BEGIN
    IF auth.role() <> 'service_role' THEN
        RAISE EXCEPTION 'Only the server may process payment webhooks'
            USING ERRCODE = '42501';
    END IF;

    SELECT *
    INTO v_payment
    FROM public.payments
    WHERE payment_method = 'payos'
      AND provider_order_code = p_order_code
    FOR UPDATE;

    -- A signed PayOS validation request can contain a test order code. It is
    -- acknowledged without creating or changing a payment.
    IF NOT FOUND THEN
        RETURN jsonb_build_object(
            'known_payment', false,
            'processed', false,
            'already_processed', false,
            'reason', 'unknown_order'
        );
    END IF;

    IF v_payment.status = 'succeeded' THEN
        SELECT *
        INTO v_booking
        FROM public.bookings
        WHERE id = v_payment.booking_id;

        RETURN jsonb_build_object(
            'known_payment', true,
            'processed', false,
            'already_processed', true,
            'reason', 'already_succeeded',
            'booking_id', v_payment.booking_id,
            'payer_id', v_payment.payer_id,
            'guide_profile_id', v_booking.guide_profile_id,
            'installment_type', v_payment.installment_type,
            'amount', v_payment.amount,
            'booking_status', v_booking.status
        );
    END IF;

    IF UPPER(COALESCE(p_currency, '')) <> UPPER(v_payment.currency) THEN
        UPDATE public.payments
        SET status = 'failed',
            failure_reason = 'Webhook currency does not match the payment attempt',
            processed_at = now(),
            metadata = COALESCE(metadata, '{}'::jsonb) || jsonb_build_object('lastWebhook', p_payload),
            updated_at = now()
        WHERE id = v_payment.id;

        RETURN jsonb_build_object(
            'known_payment', true,
            'processed', false,
            'already_processed', false,
            'reason', 'currency_mismatch',
            'booking_id', v_payment.booking_id
        );
    END IF;

    IF p_amount <> v_payment.amount THEN
        UPDATE public.payments
        SET status = 'failed',
            failure_reason = 'Webhook amount does not match the payment attempt',
            processed_at = now(),
            metadata = COALESCE(metadata, '{}'::jsonb) || jsonb_build_object('lastWebhook', p_payload),
            updated_at = now()
        WHERE id = v_payment.id;

        RETURN jsonb_build_object(
            'known_payment', true,
            'processed', false,
            'already_processed', false,
            'reason', 'amount_mismatch',
            'booking_id', v_payment.booking_id
        );
    END IF;

    IF EXISTS (
        SELECT 1
        FROM public.payments p
        WHERE p.payment_method = 'payos'
          AND p.provider_transaction_id = p_transaction_id
          AND p.id <> v_payment.id
    ) THEN
        RETURN jsonb_build_object(
            'known_payment', true,
            'processed', false,
            'already_processed', false,
            'reason', 'duplicate_provider_transaction',
            'booking_id', v_payment.booking_id
        );
    END IF;

    SELECT *
    INTO v_booking
    FROM public.bookings
    WHERE id = v_payment.booking_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Booking % for payment % was not found', v_payment.booking_id, v_payment.id;
    END IF;

    UPDATE public.payments
    SET status = 'succeeded',
        provider_transaction_id = p_transaction_id,
        paid_at = COALESCE(p_paid_at, now()),
        processed_at = now(),
        failure_reason = NULL,
        metadata = COALESCE(metadata, '{}'::jsonb) || jsonb_build_object('verifiedWebhook', p_payload),
        updated_at = now()
    WHERE id = v_payment.id;

    SELECT COALESCE(SUM(p.amount - p.refunded_amount), 0)
    INTO v_total_paid
    FROM public.payments p
    WHERE p.booking_id = v_booking.id
      AND p.status IN ('succeeded', 'partially_refunded');

    UPDATE public.bookings
    SET amount_paid = v_total_paid,
        payment_status = CASE
            WHEN v_total_paid >= total_amount THEN 'paid'
            WHEN v_total_paid > 0 THEN 'deposit_paid'
            ELSE 'unpaid'
        END,
        -- Only a verified deposit may expose the booking to the Guide. A
        -- balance payment deliberately preserves Confirmed (1).
        status = CASE
            WHEN v_payment.installment_type = 'deposit' AND status = -1 THEN 0
            ELSE status
        END,
        payment_reference = p_order_code,
        updated_at = now()
    WHERE id = v_booking.id
    RETURNING * INTO v_booking;

    RETURN jsonb_build_object(
        'known_payment', true,
        'processed', true,
        'already_processed', false,
        'booking_id', v_booking.id,
        'payer_id', v_payment.payer_id,
        'guide_profile_id', v_booking.guide_profile_id,
        'installment_type', v_payment.installment_type,
        'amount', v_payment.amount,
        'booking_status', v_booking.status,
        'payment_status', v_booking.payment_status,
        'overpayment_amount', GREATEST(v_total_paid - v_booking.total_amount, 0)
    );
END;
$$;

REVOKE ALL ON FUNCTION public.process_payos_payment_webhook(
    text, numeric, text, text, timestamp with time zone, jsonb
) FROM PUBLIC, anon, authenticated;

GRANT EXECUTE ON FUNCTION public.process_payos_payment_webhook(
    text, numeric, text, text, timestamp with time zone, jsonb
) TO service_role;

COMMENT ON FUNCTION public.process_payos_payment_webhook(
    text, numeric, text, text, timestamp with time zone, jsonb
) IS 'Atomically applies a signature-verified PayOS payment exactly once.';

COMMIT;
