-- Adds the schedule snapshot and two-party completion lifecycle used by the
-- Guide completion flow. Run after 12_add_payment_lifecycle.sql.

BEGIN;

ALTER TABLE public.bookings
    ADD COLUMN IF NOT EXISTS scheduled_start_at timestamp with time zone,
    ADD COLUMN IF NOT EXISTS scheduled_end_at timestamp with time zone,
    ADD COLUMN IF NOT EXISTS completion_state text NOT NULL DEFAULT 'not_started',
    ADD COLUMN IF NOT EXISTS guide_completed_at timestamp with time zone,
    ADD COLUMN IF NOT EXISTS traveler_completed_at timestamp with time zone,
    ADD COLUMN IF NOT EXISTS traveler_confirmation_due_at timestamp with time zone,
    ADD COLUMN IF NOT EXISTS completion_disputed_at timestamp with time zone,
    ADD COLUMN IF NOT EXISTS completion_dispute_reason text;

-- Interpret legacy booking_date/start_time as local package time. The stored
-- timestamptz snapshot prevents later package schedule edits from changing an
-- existing booking's actual trip window.
UPDATE public.bookings AS b
SET scheduled_start_at = (
    (b.booking_date + b.start_time::time)
    AT TIME ZONE COALESCE(NULLIF(BTRIM(ep.time_zone), ''), 'Asia/Ho_Chi_Minh')
)
FROM public.experience_packages AS ep
WHERE ep.id = b.experience_package_id
  AND b.scheduled_start_at IS NULL;

UPDATE public.bookings AS b
SET scheduled_start_at = (
    (b.booking_date + b.start_time::time)
    AT TIME ZONE 'Asia/Ho_Chi_Minh'
)
WHERE b.scheduled_start_at IS NULL;

UPDATE public.bookings AS b
SET scheduled_end_at = CASE
    WHEN ep.duration_type = 'multi_day' AND ep.default_end_time IS NOT NULL THEN
        (
            (
                b.booking_date
                + (GREATEST(COALESCE(ep.duration_days, 1), 1) - 1)
                + ep.default_end_time
            )
            AT TIME ZONE COALESCE(NULLIF(BTRIM(ep.time_zone), ''), 'Asia/Ho_Chi_Minh')
        )
    ELSE
        b.scheduled_start_at + make_interval(
            mins => GREATEST(
                COALESCE(ep.duration_minutes, ROUND(ep.duration_hours * 60)::integer, 240),
                30
            )
        )
END
FROM public.experience_packages AS ep
WHERE ep.id = b.experience_package_id
  AND b.scheduled_end_at IS NULL
  AND b.scheduled_start_at IS NOT NULL;

-- Custom/legacy bookings without a package schedule keep a conservative
-- four-hour snapshot until their booking-specific schedule is edited.
UPDATE public.bookings
SET scheduled_end_at = scheduled_start_at + INTERVAL '4 hours'
WHERE scheduled_end_at IS NULL
  AND scheduled_start_at IS NOT NULL;

UPDATE public.bookings
SET completion_state = CASE
    WHEN status = 2 THEN 'confirmed'
    WHEN status = 3 THEN 'cancelled'
    ELSE 'not_started'
END
WHERE (completion_state = 'not_started' AND status IN (2, 3))
   OR completion_state IS NULL
   OR completion_state NOT IN (
       'not_started', 'awaiting_guide', 'awaiting_traveler',
       'confirmed', 'disputed', 'cancelled'
   );

ALTER TABLE public.bookings
    DROP CONSTRAINT IF EXISTS bookings_completion_state_check,
    DROP CONSTRAINT IF EXISTS bookings_scheduled_window_check;

ALTER TABLE public.bookings
    ADD CONSTRAINT bookings_completion_state_check
        CHECK (completion_state IN (
            'not_started', 'awaiting_guide', 'awaiting_traveler',
            'confirmed', 'disputed', 'cancelled'
        )),
    ADD CONSTRAINT bookings_scheduled_window_check
        CHECK (
            scheduled_start_at IS NULL
            OR scheduled_end_at IS NULL
            OR scheduled_end_at > scheduled_start_at
        );

CREATE OR REPLACE FUNCTION public.set_booking_schedule_snapshot()
RETURNS trigger
LANGUAGE plpgsql
SET search_path = public
AS $$
DECLARE
    v_duration_type text;
    v_duration_minutes integer;
    v_duration_days integer;
    v_duration_hours numeric;
    v_default_end_time time without time zone;
    v_time_zone text;
BEGIN
    IF NEW.scheduled_start_at IS NOT NULL AND NEW.scheduled_end_at IS NOT NULL THEN
        RETURN NEW;
    END IF;

    SELECT
        ep.duration_type,
        ep.duration_minutes,
        ep.duration_days,
        ep.duration_hours,
        ep.default_end_time,
        COALESCE(NULLIF(BTRIM(ep.time_zone), ''), 'Asia/Ho_Chi_Minh')
    INTO
        v_duration_type,
        v_duration_minutes,
        v_duration_days,
        v_duration_hours,
        v_default_end_time,
        v_time_zone
    FROM public.experience_packages ep
    WHERE ep.id = NEW.experience_package_id;

    v_time_zone := COALESCE(v_time_zone, 'Asia/Ho_Chi_Minh');

    IF NEW.scheduled_start_at IS NULL THEN
        NEW.scheduled_start_at := (
            (NEW.booking_date + NEW.start_time::time) AT TIME ZONE v_time_zone
        );
    END IF;

    IF NEW.scheduled_end_at IS NULL THEN
        IF v_duration_type = 'multi_day' AND v_default_end_time IS NOT NULL THEN
            NEW.scheduled_end_at := (
                (
                    NEW.booking_date
                    + (GREATEST(COALESCE(v_duration_days, 1), 1) - 1)
                    + v_default_end_time
                )
                AT TIME ZONE v_time_zone
            );
        ELSE
            NEW.scheduled_end_at := NEW.scheduled_start_at + make_interval(
                mins => GREATEST(
                    COALESCE(v_duration_minutes, ROUND(v_duration_hours * 60)::integer, 240),
                    30
                )
            );
        END IF;
    END IF;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_set_booking_schedule_snapshot ON public.bookings;
CREATE TRIGGER trg_set_booking_schedule_snapshot
    BEFORE INSERT
    ON public.bookings
    FOR EACH ROW
    EXECUTE FUNCTION public.set_booking_schedule_snapshot();

CREATE INDEX IF NOT EXISTS idx_bookings_guide_completion_state
    ON public.bookings (guide_profile_id, completion_state, scheduled_end_at);

CREATE INDEX IF NOT EXISTS idx_bookings_traveler_completion_state
    ON public.bookings (traveler_id, completion_state, scheduled_end_at);

CREATE INDEX IF NOT EXISTS idx_bookings_traveler_confirmation_due
    ON public.bookings (traveler_confirmation_due_at)
    WHERE completion_state = 'awaiting_traveler';

COMMENT ON COLUMN public.bookings.scheduled_start_at IS
    'Immutable UTC trip-start snapshot used by booking lifecycle rules.';
COMMENT ON COLUMN public.bookings.scheduled_end_at IS
    'Immutable UTC trip-end snapshot used to unlock Guide completion.';
COMMENT ON COLUMN public.bookings.completion_state IS
    'Two-party completion state: not_started, awaiting_guide, awaiting_traveler, confirmed, disputed, or cancelled.';
COMMENT ON COLUMN public.bookings.traveler_confirmation_due_at IS
    'Deadline for the traveler to confirm or dispute a Guide completion claim.';
COMMENT ON FUNCTION public.set_booking_schedule_snapshot() IS
    'Captures the package schedule on booking creation so later package edits do not alter the booking.';

COMMIT;
