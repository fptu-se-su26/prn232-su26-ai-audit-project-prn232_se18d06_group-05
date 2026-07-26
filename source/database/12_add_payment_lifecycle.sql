-- Extends the existing public.payments table and adds a canonical payment
-- summary to bookings. This migration intentionally does not create a second
-- payment table.
-- Run after 11_add_tour_schedule.sql.

BEGIN;

ALTER TABLE public.bookings
    ADD COLUMN IF NOT EXISTS payment_status text NOT NULL DEFAULT 'unpaid';

UPDATE public.bookings
SET payment_status = CASE
    WHEN COALESCE(amount_paid, 0) <= 0 THEN 'unpaid'
    WHEN total_amount > 0 AND amount_paid >= total_amount THEN 'paid'
    ELSE 'deposit_paid'
END
WHERE payment_status IS NULL
   OR payment_status NOT IN ('partially_refunded', 'refunded');

ALTER TABLE public.bookings
    DROP CONSTRAINT IF EXISTS bookings_payment_status_check;

ALTER TABLE public.bookings
    ADD CONSTRAINT bookings_payment_status_check
    CHECK (payment_status IN ('unpaid', 'deposit_paid', 'paid', 'partially_refunded', 'refunded'));

CREATE OR REPLACE FUNCTION public.sync_booking_payment_status()
RETURNS trigger
LANGUAGE plpgsql
SET search_path = public
AS $$
BEGIN
    -- Respect an explicit lifecycle transition (for example a refund). When
    -- legacy code only changes amount_paid, derive the aggregate status here.
    IF TG_OP = 'UPDATE' AND NEW.payment_status IS DISTINCT FROM OLD.payment_status THEN
        RETURN NEW;
    END IF;

    NEW.payment_status := CASE
        WHEN COALESCE(NEW.amount_paid, 0) <= 0 THEN 'unpaid'
        WHEN NEW.total_amount > 0 AND NEW.amount_paid >= NEW.total_amount THEN 'paid'
        ELSE 'deposit_paid'
    END;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_sync_booking_payment_status ON public.bookings;
CREATE TRIGGER trg_sync_booking_payment_status
    BEFORE INSERT OR UPDATE OF amount_paid, total_amount, payment_status
    ON public.bookings
    FOR EACH ROW
    EXECUTE FUNCTION public.sync_booking_payment_status();

ALTER TABLE public.payments
    ADD COLUMN IF NOT EXISTS installment_type text NOT NULL DEFAULT 'legacy',
    ADD COLUMN IF NOT EXISTS provider_order_code text,
    ADD COLUMN IF NOT EXISTS checkout_url text,
    ADD COLUMN IF NOT EXISTS expires_at timestamp with time zone,
    ADD COLUMN IF NOT EXISTS processed_at timestamp with time zone,
    ADD COLUMN IF NOT EXISTS failure_reason text,
    ADD COLUMN IF NOT EXISTS refunded_amount numeric(14,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS idempotency_key text;

-- Preserve legacy records without guessing whether they represented the 30%
-- deposit or the remaining balance. New application writes must set the value.
UPDATE public.payments
SET installment_type = CASE
    WHEN LOWER(COALESCE(metadata ->> 'installmentType', metadata ->> 'installment_type', ''))
         IN ('deposit', 'balance')
        THEN LOWER(COALESCE(metadata ->> 'installmentType', metadata ->> 'installment_type'))
    ELSE 'legacy'
END
WHERE installment_type IS NULL
   OR installment_type = 'legacy'
   OR installment_type NOT IN ('deposit', 'balance', 'legacy');

UPDATE public.payments
SET provider_order_code = COALESCE(
    NULLIF(metadata ->> 'orderCode', ''),
    NULLIF(metadata ->> 'order_code', '')
)
WHERE provider_order_code IS NULL;

UPDATE public.payments
SET status = CASE LOWER(BTRIM(status))
    WHEN 'paid' THEN 'succeeded'
    WHEN 'success' THEN 'succeeded'
    WHEN 'pending' THEN 'pending'
    WHEN 'processing' THEN 'processing'
    WHEN 'succeeded' THEN 'succeeded'
    WHEN 'failed' THEN 'failed'
    WHEN 'refunded' THEN 'refunded'
    WHEN 'partially_refunded' THEN 'partially_refunded'
    WHEN 'cancelled' THEN 'cancelled'
    WHEN 'canceled' THEN 'cancelled'
    WHEN 'expired' THEN 'expired'
    ELSE 'unknown'
END;

ALTER TABLE public.payments
    DROP CONSTRAINT IF EXISTS payments_status_check,
    DROP CONSTRAINT IF EXISTS payments_installment_type_check,
    DROP CONSTRAINT IF EXISTS payments_amount_check,
    DROP CONSTRAINT IF EXISTS payments_refunded_amount_check;

ALTER TABLE public.payments
    ADD CONSTRAINT payments_status_check
        CHECK (status IN (
            'pending', 'processing', 'succeeded', 'failed',
            'partially_refunded', 'refunded', 'cancelled', 'expired', 'unknown'
        )),
    ADD CONSTRAINT payments_installment_type_check
        CHECK (installment_type IN ('deposit', 'balance', 'legacy')),
    ADD CONSTRAINT payments_amount_check
        CHECK (amount > 0),
    ADD CONSTRAINT payments_refunded_amount_check
        CHECK (refunded_amount >= 0 AND refunded_amount <= amount);

CREATE INDEX IF NOT EXISTS idx_payments_booking_created_at
    ON public.payments (booking_id, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_payments_status
    ON public.payments (status);

CREATE UNIQUE INDEX IF NOT EXISTS ux_payments_provider_order_code
    ON public.payments (payment_method, provider_order_code)
    WHERE provider_order_code IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_payments_idempotency_key
    ON public.payments (idempotency_key)
    WHERE idempotency_key IS NOT NULL;

ALTER TABLE public.payments ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS payments_select_participants ON public.payments;
CREATE POLICY payments_select_participants
    ON public.payments
    FOR SELECT
    USING (
        payer_id = auth.uid()
        OR EXISTS (
            SELECT 1
            FROM public.bookings b
            JOIN public.guide_profiles gp ON gp.id = b.guide_profile_id
            WHERE b.id = payments.booking_id
              AND gp.user_id = auth.uid()
        )
        OR EXISTS (
            SELECT 1
            FROM public.profiles p
            WHERE p.id = auth.uid() AND p.role = 'admin'
        )
    );

-- Payment attempts and provider results are server-owned. Browser clients get
-- read access to their own transaction history but no direct write privilege.
REVOKE INSERT, UPDATE, DELETE ON public.payments FROM anon, authenticated;
GRANT SELECT ON public.payments TO authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON public.payments TO service_role;

COMMENT ON COLUMN public.bookings.payment_status IS
    'Payment summary: unpaid, deposit_paid, paid, partially_refunded, or refunded.';
COMMENT ON COLUMN public.payments.installment_type IS
    'Payment installment represented by this row: deposit, balance, or legacy.';
COMMENT ON COLUMN public.payments.provider_order_code IS
    'Stable provider order identifier, such as a PayOS order code.';
COMMENT ON COLUMN public.payments.idempotency_key IS
    'Application-generated key preventing duplicate payment attempts/events.';
COMMENT ON FUNCTION public.sync_booking_payment_status() IS
    'Keeps the booking payment summary compatible with legacy amount_paid writes.';

COMMIT;
