-- Fixed-tour pricing model:
-- price_per_session = base price for the tour
-- included_guest_count = guests included in the base price
-- price_per_person = additional fee for each guest above included_guest_count
-- max_group_size = hard booking capacity

ALTER TABLE public.experience_packages
ADD COLUMN IF NOT EXISTS included_guest_count integer NOT NULL DEFAULT 1;

COMMENT ON COLUMN public.experience_packages.price_per_person IS
'Additional fee charged for each guest above included_guest_count; this is not a standalone per-person tour price.';

COMMENT ON COLUMN public.experience_packages.included_guest_count IS
'Number of guests covered by price_per_session.';

UPDATE public.experience_packages
SET included_guest_count = 1
WHERE included_guest_count IS NULL OR included_guest_count < 1;

-- Preserve the old per-person custom package behavior under the new model:
-- one guest is included in the base price and each extra guest pays the same fee.
UPDATE public.experience_packages
SET price_per_session = price_per_person
WHERE id = '00000000-0000-0000-0000-000000000000'
  AND price_per_session = 0
  AND COALESCE(price_per_person, 0) > 0;

UPDATE public.experience_packages
SET max_group_size = included_guest_count
WHERE max_group_size < included_guest_count;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'exp_pkg_included_guest_count_check'
  ) THEN
    ALTER TABLE public.experience_packages
      ADD CONSTRAINT exp_pkg_included_guest_count_check CHECK (included_guest_count >= 1);
  END IF;

  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'exp_pkg_max_group_size_check'
  ) THEN
    ALTER TABLE public.experience_packages
      ADD CONSTRAINT exp_pkg_max_group_size_check CHECK (max_group_size >= included_guest_count);
  END IF;

  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'exp_pkg_price_check'
  ) THEN
    ALTER TABLE public.experience_packages
      ADD CONSTRAINT exp_pkg_price_check
      CHECK (price_per_session >= 0 AND COALESCE(price_per_person, 0) >= 0);
  END IF;
END $$;
