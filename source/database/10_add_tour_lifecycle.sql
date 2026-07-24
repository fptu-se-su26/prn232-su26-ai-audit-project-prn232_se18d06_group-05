-- Adds an explicit lifecycle for Guide tour packages.
-- Run after 09_add_tour_included_guest_count.sql.

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'experience_packages'
          AND column_name = 'publication_status'
    ) THEN
        ALTER TABLE public.experience_packages
            ADD COLUMN publication_status text NOT NULL DEFAULT 'published';

        -- Preserve the meaning of inactive packages during the first migration.
        UPDATE public.experience_packages
        SET publication_status = CASE WHEN is_active THEN 'published' ELSE 'hidden' END;
    END IF;
END $$;

ALTER TABLE public.experience_packages
    ADD COLUMN IF NOT EXISTS updated_at timestamp with time zone NOT NULL DEFAULT now();

UPDATE public.experience_packages
SET publication_status = CASE WHEN is_active THEN 'published' ELSE 'hidden' END
WHERE publication_status IS NULL
   OR publication_status NOT IN ('draft', 'published', 'hidden');

ALTER TABLE public.experience_packages
    DROP CONSTRAINT IF EXISTS exp_pkg_publication_status_check;

ALTER TABLE public.experience_packages
    ADD CONSTRAINT exp_pkg_publication_status_check
    CHECK (publication_status IN ('draft', 'published', 'hidden'));

CREATE INDEX IF NOT EXISTS idx_experience_packages_guide_publication_status
    ON public.experience_packages (guide_profile_id, publication_status);

COMMENT ON COLUMN public.experience_packages.publication_status IS
    'Guide lifecycle: draft, published, or hidden. is_active remains synchronized for traveler compatibility.';
