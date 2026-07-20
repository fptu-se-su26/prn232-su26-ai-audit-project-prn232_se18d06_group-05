-- Create a function to update average_rating and total_reviews for a guide
CREATE OR REPLACE FUNCTION public.update_guide_rating()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
BEGIN
    IF TG_OP = 'INSERT' OR TG_OP = 'UPDATE' THEN
        UPDATE public.guide_profiles
        SET 
            average_rating = (
                SELECT COALESCE(AVG(rating), 0)
                FROM public.reviews
                WHERE guide_profile_id = NEW.guide_profile_id
            ),
            total_reviews = (
                SELECT COUNT(*)
                FROM public.reviews
                WHERE guide_profile_id = NEW.guide_profile_id
            )
        WHERE id = NEW.guide_profile_id;
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE public.guide_profiles
        SET 
            average_rating = (
                SELECT COALESCE(AVG(rating), 0)
                FROM public.reviews
                WHERE guide_profile_id = OLD.guide_profile_id
            ),
            total_reviews = (
                SELECT COUNT(*)
                FROM public.reviews
                WHERE guide_profile_id = OLD.guide_profile_id
            )
        WHERE id = OLD.guide_profile_id;
        RETURN OLD;
    END IF;
    RETURN NULL;
END;
$$;

-- Create the trigger on the reviews table
DROP TRIGGER IF EXISTS trigger_update_guide_rating ON public.reviews;
CREATE TRIGGER trigger_update_guide_rating
AFTER INSERT OR UPDATE OR DELETE
ON public.reviews
FOR EACH ROW
EXECUTE FUNCTION public.update_guide_rating();

-- Manually trigger the update for all existing guides to sync data
DO $$
DECLARE
    g_id uuid;
BEGIN
    FOR g_id IN SELECT id FROM public.guide_profiles LOOP
        UPDATE public.guide_profiles
        SET 
            average_rating = COALESCE((SELECT AVG(rating) FROM public.reviews WHERE guide_profile_id = g_id), 0),
            total_reviews = (SELECT COUNT(*) FROM public.reviews WHERE guide_profile_id = g_id)
        WHERE id = g_id;
    END LOOP;
END;
$$;
