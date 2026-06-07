-- FIX PROFILES POLICIES FOR REGISTRATION
-- Chạy script này trên Supabase SQL Editor

-- 1. Drop existing policies để tránh conflict
DROP POLICY IF EXISTS "Cho phép tất cả mọi người đọc profile của nhau" ON public.profiles;
DROP POLICY IF EXISTS "Chính chủ mới được cập nhật profile của mình" ON public.profiles;
DROP POLICY IF EXISTS "Cho phép tạo profile mới khi đăng ký" ON public.profiles;

-- 2. Tạo lại policies với quyền đầy đủ
-- Allow anyone to read profiles (public data)
CREATE POLICY "profiles_select_public"
  ON public.profiles FOR SELECT
  USING (true);

-- Allow users to insert their own profile (during registration)
CREATE POLICY "profiles_insert_own"
  ON public.profiles FOR INSERT
  WITH CHECK (auth.uid()::text = id);

-- Allow users to update their own profile
CREATE POLICY "profiles_update_own"
  ON public.profiles FOR UPDATE
  USING (auth.uid()::text = id);

-- Allow admins to update any profile (for approval workflow)
CREATE POLICY "profiles_admin_update"
  ON public.profiles FOR UPDATE
  USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid()::text AND role = 'admin'
    )
  );

-- 3. Ensure profiles table has correct grants
GRANT SELECT ON public.profiles TO anon, authenticated;
GRANT INSERT ON public.profiles TO authenticated;
GRANT UPDATE ON public.profiles TO authenticated;

-- 4. Fix admin_notifications policies
DROP POLICY IF EXISTS "Chỉ admin mới được xem thông báo admin" ON public.admin_notifications;
DROP POLICY IF EXISTS "Chỉ admin mới được cập nhật thông báo admin" ON public.admin_notifications;

CREATE POLICY "admin_notifications_select"
  ON public.admin_notifications FOR SELECT
  USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid()::text AND role = 'admin'
    )
  );

CREATE POLICY "admin_notifications_insert"
  ON public.admin_notifications FOR INSERT
  WITH CHECK (true); -- Allow system to insert notifications

CREATE POLICY "admin_notifications_update"
  ON public.admin_notifications FOR UPDATE
  USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid()::text AND role = 'admin'
    )
  );

-- 5. Grant permissions for admin_notifications
GRANT SELECT, INSERT, UPDATE ON public.admin_notifications TO authenticated;

-- 6. Kiểm tra trigger function
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS trigger AS $$
BEGIN
  INSERT INTO public.profiles (id, email, full_name, avatar_url, role, status)
  VALUES (
    new.id::text, 
    new.email, 
    new.raw_user_meta_data->>'full_name', 
    new.raw_user_meta_data->>'avatar_url', 
    coalesce(new.raw_user_meta_data->>'role', 'traveler'), 
    CASE 
      WHEN coalesce(new.raw_user_meta_data->>'role', 'traveler') = 'guide' THEN 'pending'
      ELSE 'active'
    END
  )
  ON CONFLICT (id) DO UPDATE SET
    full_name = EXCLUDED.full_name,
    role = EXCLUDED.role,
    status = CASE 
      WHEN EXCLUDED.role = 'guide' THEN 'pending'
      ELSE 'active'
    END,
    updated_at = now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- 7. Ensure trigger is active
DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

-- Success message
SELECT 'Profiles policies fixed! Registration should work now.' as message;