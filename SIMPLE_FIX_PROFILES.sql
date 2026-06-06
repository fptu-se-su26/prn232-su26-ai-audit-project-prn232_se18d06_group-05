-- SIMPLE FIX FOR PROFILES PERMISSIONS
-- Chạy trên Supabase SQL Editor

-- 1. Disable RLS temporarily để test
ALTER TABLE public.profiles DISABLE ROW LEVEL SECURITY;

-- 2. Grant full permissions for authenticated users
GRANT ALL ON public.profiles TO authenticated;
GRANT ALL ON public.admin_notifications TO authenticated;

-- 3. Ensure trigger function works
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS trigger 
SECURITY DEFINER
AS $$
BEGIN
  INSERT INTO public.profiles (id, email, full_name, avatar_url, role, status)
  VALUES (
    new.id, 
    new.email, 
    COALESCE(new.raw_user_meta_data->>'full_name', 'User'), 
    new.raw_user_meta_data->>'avatar_url', 
    COALESCE(new.raw_user_meta_data->>'role', 'traveler'), 
    CASE 
      WHEN COALESCE(new.raw_user_meta_data->>'role', 'traveler') = 'guide' THEN 'pending'
      ELSE 'active'
    END
  )
  ON CONFLICT (id) DO UPDATE SET
    full_name = EXCLUDED.full_name,
    role = EXCLUDED.role,
    status = CASE 
      WHEN EXCLUDED.role = 'guide' THEN 'pending'
      ELSE EXCLUDED.status
    END,
    updated_at = now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 4. Recreate trigger
DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

-- 5. Test message
SELECT 'RLS disabled for profiles table - registration should work now!' as status;