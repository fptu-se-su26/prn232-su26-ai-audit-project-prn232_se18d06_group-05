-- ============================================
-- FIX PROFILES RLS POLICIES
-- ============================================
-- Chạy script này nếu không thể insert vào profiles
-- ============================================

-- 1. Kiểm tra RLS có đang bật không
SELECT tablename, rowsecurity 
FROM pg_tables 
WHERE schemaname = 'public' AND tablename = 'profiles';

-- 2. Xem tất cả policies hiện tại
SELECT * FROM pg_policies WHERE tablename = 'profiles';

-- 3. XÓA tất cả policies cũ (nếu có vấn đề)
DROP POLICY IF EXISTS "Public profiles are viewable by everyone" ON profiles;
DROP POLICY IF EXISTS "Users can update own profile" ON profiles;
DROP POLICY IF EXISTS "Users can insert own profile" ON profiles;

-- 4. TẠO LẠI policies đúng

-- Policy 1: Cho phép mọi người xem profiles
CREATE POLICY "Public profiles are viewable by everyone"
  ON profiles FOR SELECT
  USING (true);

-- Policy 2: Cho phép user insert profile của chính mình
CREATE POLICY "Users can insert own profile"
  ON profiles FOR INSERT
  WITH CHECK (auth.uid() = id);

-- Policy 3: Cho phép user update profile của chính mình
CREATE POLICY "Users can update own profile"
  ON profiles FOR UPDATE
  USING (auth.uid() = id);

-- 5. KIỂM TRA lại policies
SELECT 
  schemaname,
  tablename,
  policyname,
  permissive,
  roles,
  cmd,
  qual,
  with_check
FROM pg_policies 
WHERE tablename = 'profiles';

-- ============================================
-- ALTERNATIVE: TẠM THỜI TẮT RLS (CHỈ CHO DEV)
-- ============================================
-- Nếu vẫn không work, tạm thời tắt RLS để test
-- CẢNH BÁO: Chỉ dùng cho development, KHÔNG dùng cho production!

-- ALTER TABLE profiles DISABLE ROW LEVEL SECURITY;

-- Sau khi test xong, BẬT LẠI:
-- ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;

-- ============================================
-- TEST INSERT
-- ============================================
-- Test insert trực tiếp (thay YOUR_USER_ID)
/*
INSERT INTO profiles (id, email, full_name, role)
VALUES (
  'YOUR_USER_ID',
  'test@example.com',
  'Test User',
  'traveler'
);
*/

-- ============================================
-- KIỂM TRA DỮ LIỆU
-- ============================================
-- Xem tất cả profiles
SELECT * FROM profiles ORDER BY created_at DESC;

-- Xem users trong auth.users
SELECT id, email, email_confirmed_at, created_at 
FROM auth.users 
ORDER BY created_at DESC;

-- Kiểm tra user có profile chưa
/*
SELECT 
  u.id,
  u.email,
  u.created_at as user_created,
  p.id as profile_id,
  p.full_name,
  p.created_at as profile_created
FROM auth.users u
LEFT JOIN profiles p ON u.id = p.id
ORDER BY u.created_at DESC;
*/
