-- Keeps the two-party completion transition and Admin payout queue in sync.
-- Run after 15_add_payos_webhook_processing.sql.

BEGIN;

CREATE OR REPLACE FUNCTION public.sync_completed_booking_payout_eligibility()
RETURNS trigger
LANGUAGE plpgsql
SET search_path = public
AS $$
BEGIN
    IF NEW.status = 2
       AND NEW.completion_state = 'confirmed'
       AND COALESCE(NEW.amount_paid, 0) >= NEW.total_amount
       AND NOT COALESCE(NEW.escrow_released, false) THEN
        NEW.traveler_completed_at := COALESCE(NEW.traveler_completed_at, now());

        IF NEW.payout_status = 'held' THEN
            NEW.payout_status := 'eligible';
            NEW.payout_eligible_at := COALESCE(NEW.payout_eligible_at, now());
            NEW.payout_failure_reason := NULL;
        END IF;
    END IF;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_sync_completed_booking_payout_eligibility
    ON public.bookings;

CREATE TRIGGER trg_sync_completed_booking_payout_eligibility
    BEFORE INSERT OR UPDATE OF
        status,
        completion_state,
        amount_paid,
        total_amount,
        escrow_released
    ON public.bookings
    FOR EACH ROW
    EXECUTE FUNCTION public.sync_completed_booking_payout_eligibility();

-- Repair bookings completed before the application wrote the payout lifecycle
-- fields. updated_at is the closest durable timestamp to the Traveler action.
UPDATE public.bookings
SET traveler_completed_at = COALESCE(traveler_completed_at, updated_at, now()),
    payout_status = 'eligible',
    payout_eligible_at = COALESCE(payout_eligible_at, updated_at, now()),
    payout_failure_reason = NULL
WHERE status = 2
  AND completion_state = 'confirmed'
  AND COALESCE(amount_paid, 0) >= total_amount
  AND NOT COALESCE(escrow_released, false)
  AND payout_status = 'held';

COMMENT ON FUNCTION public.sync_completed_booking_payout_eligibility() IS
    'Marks fully paid, mutually confirmed bookings eligible for idempotent Admin payout release.';

COMMIT;
