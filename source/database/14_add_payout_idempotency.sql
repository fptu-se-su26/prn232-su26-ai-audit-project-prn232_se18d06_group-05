-- Adds an explicit payout lifecycle and an atomic, idempotent payout release
-- function. Run after 13_add_booking_completion_lifecycle.sql.

BEGIN;

ALTER TABLE public.bookings
    ADD COLUMN IF NOT EXISTS payout_status text NOT NULL DEFAULT 'held',
    ADD COLUMN IF NOT EXISTS payout_eligible_at timestamp with time zone,
    ADD COLUMN IF NOT EXISTS payout_released_at timestamp with time zone,
    ADD COLUMN IF NOT EXISTS payout_failure_reason text;

UPDATE public.bookings
SET payout_status = CASE
    WHEN escrow_released THEN 'released'
    WHEN status = 3 THEN 'not_applicable'
    WHEN status = 2
         AND completion_state = 'confirmed'
         AND amount_paid >= total_amount THEN 'eligible'
    ELSE 'held'
END
WHERE payout_status = 'held'
   OR payout_status IS NULL
   OR payout_status NOT IN ('held', 'eligible', 'processing', 'released', 'failed', 'not_applicable');

UPDATE public.bookings
SET payout_released_at = COALESCE(payout_released_at, updated_at, created_at, now())
WHERE escrow_released = true
  AND payout_status = 'released'
  AND payout_released_at IS NULL;

UPDATE public.bookings
SET payout_eligible_at = COALESCE(payout_eligible_at, updated_at, created_at, now())
WHERE payout_status = 'eligible'
  AND payout_eligible_at IS NULL;

ALTER TABLE public.bookings
    DROP CONSTRAINT IF EXISTS bookings_payout_status_check,
    DROP CONSTRAINT IF EXISTS bookings_released_payout_check;

ALTER TABLE public.bookings
    ADD CONSTRAINT bookings_payout_status_check
        CHECK (payout_status IN ('held', 'eligible', 'processing', 'released', 'failed', 'not_applicable')),
    ADD CONSTRAINT bookings_released_payout_check
        CHECK (NOT escrow_released OR payout_status = 'released');

ALTER TABLE public.ledger_entries
    ADD COLUMN IF NOT EXISTS idempotency_key text;

CREATE UNIQUE INDEX IF NOT EXISTS ux_ledger_entries_idempotency_key
    ON public.ledger_entries (idempotency_key);

CREATE INDEX IF NOT EXISTS idx_bookings_payout_queue
    ON public.bookings (payout_status, payout_eligible_at)
    WHERE payout_status IN ('eligible', 'failed');

CREATE OR REPLACE FUNCTION public.release_booking_payout(p_booking_id uuid)
RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_booking public.bookings%ROWTYPE;
    v_guide_user_id uuid;
BEGIN
    IF auth.role() <> 'service_role'
       AND NOT EXISTS (
           SELECT 1
           FROM public.profiles p
           WHERE p.id = auth.uid() AND p.role = 'admin'
       ) THEN
        RAISE EXCEPTION 'Only an administrator can release a payout'
            USING ERRCODE = '42501';
    END IF;

    SELECT *
    INTO v_booking
    FROM public.bookings
    WHERE id = p_booking_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Booking % was not found', p_booking_id
            USING ERRCODE = 'P0002';
    END IF;

    IF v_booking.escrow_released OR v_booking.payout_status = 'released' THEN
        RETURN jsonb_build_object(
            'booking_id', v_booking.id,
            'payout_status', 'released',
            'already_released', true
        );
    END IF;

    IF v_booking.status <> 2 OR v_booking.completion_state <> 'confirmed' THEN
        RAISE EXCEPTION 'Booking % has not been confirmed complete', p_booking_id;
    END IF;

    IF v_booking.amount_paid < v_booking.total_amount THEN
        RAISE EXCEPTION 'Booking % is not fully paid', p_booking_id;
    END IF;

    IF v_booking.payout_status NOT IN ('eligible', 'failed') THEN
        RAISE EXCEPTION 'Booking % is not eligible for payout (state: %)',
            p_booking_id, v_booking.payout_status;
    END IF;

    SELECT gp.user_id
    INTO v_guide_user_id
    FROM public.guide_profiles gp
    WHERE gp.id = v_booking.guide_profile_id;

    IF v_guide_user_id IS NULL THEN
        RAISE EXCEPTION 'Booking % has no valid Guide account', p_booking_id;
    END IF;

    INSERT INTO public.ledger_entries (
        booking_id, user_id, type, amount, idempotency_key
    )
    VALUES (
        v_booking.id,
        v_booking.traveler_id,
        'FEE',
        v_booking.platform_fee,
        'payout:' || v_booking.id::text || ':fee'
    )
    ON CONFLICT (idempotency_key) DO NOTHING;

    INSERT INTO public.ledger_entries (
        booking_id, user_id, type, amount, idempotency_key
    )
    VALUES (
        v_booking.id,
        v_guide_user_id,
        'EARNING',
        v_booking.guide_earnings,
        'payout:' || v_booking.id::text || ':earning'
    )
    ON CONFLICT (idempotency_key) DO NOTHING;

    UPDATE public.bookings
    SET escrow_released = true,
        payout_status = 'released',
        payout_released_at = COALESCE(payout_released_at, now()),
        payout_failure_reason = NULL,
        updated_at = now()
    WHERE id = v_booking.id;

    RETURN jsonb_build_object(
        'booking_id', v_booking.id,
        'payout_status', 'released',
        'already_released', false,
        'guide_user_id', v_guide_user_id,
        'guide_earnings', v_booking.guide_earnings,
        'platform_fee', v_booking.platform_fee
    );
END;
$$;

REVOKE ALL ON FUNCTION public.release_booking_payout(uuid) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION public.release_booking_payout(uuid) TO authenticated, service_role;

COMMENT ON COLUMN public.bookings.payout_status IS
    'Payout state: held, eligible, processing, released, failed, or not_applicable.';
COMMENT ON COLUMN public.ledger_entries.idempotency_key IS
    'Unique business-operation key preventing duplicate ledger entries.';
COMMENT ON FUNCTION public.release_booking_payout(uuid) IS
    'Atomically validates completion/payment, writes ledger entries, and releases Guide earnings.';

COMMIT;
