-- ============================================
-- UPDATE USER ROLES - SIMPLE VERSION
-- ============================================
-- Script đơn giản để update role cho users đã tồn tại
-- ============================================

-- ============================================
-- CÁCH 1: Update role bằng email
-- ============================================

-- Xem tất cả users hiện tại
SELECT 
  p.id,
  p.email,
  p.full_name,
  p.role,
  u.email_confirmed_at
FROM profiles p
JOIN auth.users u ON p.id = u.id
ORDER BY p.created_at DESC;

-- Update role thành 'guide'
UPDATE profiles 
SET role = 'guide', updated_at = NOW()
WHERE email = 'YOUR_EMAIL@example.com';

-- Update role thành 'admin'
UPDATE profiles 
SET role = 'admin', updated_at = NOW()
WHERE email = 'YOUR_EMAIL@example.com';

-- Update role thành 'traveler'
UPDATE profiles 
SET role = 'traveler', updated_at = NOW()
WHERE email = 'YOUR_EMAIL@example.com';

-- ============================================
-- CÁCH 2: Update role bằng user ID
-- ============================================

-- Update role bằng ID
UPDATE profiles 
SET role = 'guide', updated_at = NOW()
WHERE id = 'YOUR_USER_ID';

-- ============================================
-- CÁCH 3: Tạo sample tours cho guide
-- ============================================

-- Lấy guide ID
SELECT id, email, full_name 
FROM profiles 
WHERE role = 'guide';

-- Tạo tour mẫu (thay GUIDE_ID)
INSERT INTO tours (
  guide_id, 
  title, 
  description, 
  location, 
  price, 
  duration_hours, 
  max_participants,
  images,
  status
)
VALUES (
  'GUIDE_ID',
  'Tour Hà Nội Phố Cổ',
  'Khám phá phố cổ Hà Nội với hướng dẫn viên địa phương',
  'Hà Nội',
  500000,
  4,
  15,
  ARRAY['https://images.unsplash.com/photo-1555400038-63f5ba517a47?w=800'],
  'active'
);

-- ============================================
-- QUICK COMMANDS
-- ============================================

-- Xem tất cả roles
SELECT role, COUNT(*) as count
FROM profiles
GROUP BY role;

-- Xem users theo role
SELECT email, full_name, role, created_at
FROM profiles
WHERE role = 'guide'
ORDER BY created_at DESC;

-- Xem tours của guide
SELECT 
  t.title,
  t.location,
  t.price,
  p.email as guide_email,
  t.created_at
FROM tours t
JOIN profiles p ON t.guide_id = p.id
ORDER BY t.created_at DESC;

-- ============================================
-- EXAMPLES
-- ============================================

-- Example 1: Chuyển user thành guide
UPDATE profiles 
SET role = 'guide', updated_at = NOW()
WHERE email = 'user@example.com';

-- Example 2: Chuyển guide thành admin
UPDATE profiles 
SET role = 'admin', updated_at = NOW()
WHERE email = 'guide@example.com';

-- Example 3: Reset về traveler
UPDATE profiles 
SET role = 'traveler', updated_at = NOW()
WHERE email = 'admin@example.com';

-- ============================================
-- VERIFY CHANGES
-- ============================================

-- Kiểm tra role đã update
SELECT 
  email,
  full_name,
  role,
  updated_at
FROM profiles
WHERE email = 'YOUR_EMAIL@example.com';
