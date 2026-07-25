-- Adds the Guide-managed schedule contract for same-day and multi-day tours.
-- Run after 10_add_tour_lifecycle.sql.
--
-- duration_hours remains in place while the existing Traveler/API flows migrate.
-- New Guide writes should keep it synchronized with duration_minutes.

BEGIN;

ALTER TABLE public.experience_packages
    ADD COLUMN IF NOT EXISTS duration_type text,
    ADD COLUMN IF NOT EXISTS duration_minutes integer,
    ADD COLUMN IF NOT EXISTS duration_days integer,
    ADD COLUMN IF NOT EXISTS default_start_time time without time zone,
    ADD COLUMN IF NOT EXISTS default_end_time time without time zone,
    ADD COLUMN IF NOT EXISTS time_zone text,
    ADD COLUMN IF NOT EXISTS timeline_json jsonb NOT NULL DEFAULT '[]'::jsonb;

-- Backfill only missing values so rerunning this migration never overwrites a
-- schedule that a Guide has already configured.
UPDATE public.experience_packages
SET duration_type = CASE
        WHEN duration_hours > 24 THEN 'multi_day'
        ELSE 'same_day'
    END
WHERE duration_type IS NULL;

UPDATE public.experience_packages
SET duration_minutes = GREATEST(30, ROUND(duration_hours * 60)::integer)
WHERE duration_minutes IS NULL;

UPDATE public.experience_packages
SET duration_days = CASE
        WHEN duration_type = 'multi_day'
            THEN GREATEST(2, CEIL(duration_hours / 24.0)::integer)
        ELSE 1
    END
WHERE duration_days IS NULL;

UPDATE public.experience_packages
SET time_zone = 'Asia/Ho_Chi_Minh'
WHERE time_zone IS NULL OR BTRIM(time_zone) = '';

ALTER TABLE public.experience_packages
    ALTER COLUMN duration_type SET DEFAULT 'same_day',
    ALTER COLUMN duration_type SET NOT NULL,
    ALTER COLUMN duration_days SET DEFAULT 1,
    ALTER COLUMN duration_days SET NOT NULL,
    ALTER COLUMN time_zone SET DEFAULT 'Asia/Ho_Chi_Minh',
    ALTER COLUMN time_zone SET NOT NULL;

ALTER TABLE public.experience_packages
    DROP CONSTRAINT IF EXISTS exp_pkg_duration_type_check,
    DROP CONSTRAINT IF EXISTS exp_pkg_duration_minutes_check,
    DROP CONSTRAINT IF EXISTS exp_pkg_duration_days_check,
    DROP CONSTRAINT IF EXISTS exp_pkg_duration_shape_check,
    DROP CONSTRAINT IF EXISTS exp_pkg_time_zone_check;

ALTER TABLE public.experience_packages
    ADD CONSTRAINT exp_pkg_duration_type_check
        CHECK (duration_type IN ('same_day', 'multi_day')),
    ADD CONSTRAINT exp_pkg_duration_minutes_check
        CHECK (duration_minutes IS NULL OR duration_minutes >= 30),
    ADD CONSTRAINT exp_pkg_duration_days_check
        CHECK (duration_days >= 1),
    ADD CONSTRAINT exp_pkg_duration_shape_check
        CHECK (
            (duration_type = 'same_day' AND duration_days = 1)
            OR (duration_type = 'multi_day' AND duration_days >= 2)
        ),
    ADD CONSTRAINT exp_pkg_time_zone_check
        CHECK (BTRIM(time_zone) <> '');

COMMENT ON COLUMN public.experience_packages.duration_type IS
    'Guide schedule type: same_day or multi_day.';
COMMENT ON COLUMN public.experience_packages.duration_minutes IS
    'Exact elapsed duration used by the Guide schedule. Nullable only during legacy compatibility.';
COMMENT ON COLUMN public.experience_packages.duration_days IS
    'Number of calendar days represented by the Guide itinerary.';
COMMENT ON COLUMN public.experience_packages.default_start_time IS
    'Default local start time for new bookings of this package.';
COMMENT ON COLUMN public.experience_packages.default_end_time IS
    'Default local end time for new bookings of this package.';
COMMENT ON COLUMN public.experience_packages.time_zone IS
    'IANA time-zone identifier used to interpret the package schedule.';
COMMENT ON COLUMN public.experience_packages.timeline_json IS
    'Guide itinerary JSON. New records use ordered entries with day number, day title, start/end time, activity, and location.';

COMMIT;
