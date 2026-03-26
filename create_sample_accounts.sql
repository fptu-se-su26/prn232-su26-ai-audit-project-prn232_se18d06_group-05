-- ============================================
-- CREATE SAMPLE ACCOUNTS FOR TESTING
-- ============================================
-- Script này tạo 3 tài khoản mẫu với các role khác nhau
-- ============================================

-- ============================================
-- QUAN TRỌNG: Đọc trước khi chạy!
-- ============================================
-- 1. Tài khoản trong auth.users phải được tạo qua Supabase Auth API
-- 2. Script này CHỈ tạo profiles và sample data
-- 3. Bạn cần đăng ký tài khoản qua app trước, sau đó update role

-- ============================================
-- CÁCH SỬ DỤNG
-- ============================================
-- Option 1: Tạo tài khoản qua app, sau đó update role bằng script này
-- Option 2: Tạo tài khoản trực tiếp trong Supabase Dashboard

-- ============================================
-- BƯỚC 1: TẠO TÀI KHOẢN TRONG SUPABASE DASHBOARD
-- ============================================
-- Vào: Authentication > Users > Add user
-- Tạo 3 users:
-- 1. traveler@test.com / Test123456
-- 2. guide@test.com / Test123456  
-- 3. admin@test.com / Test123456

-- ============================================
-- BƯỚC 2: LẤY USER IDs
-- ============================================
-- Chạy query này để lấy user IDs vừa tạo:

SELECT id, email, created_at 
FROM auth.users 
WHERE email IN ('traveler@test.com', 'guide@test.com', 'admin@test.com')
ORDER BY email;

-- Copy các IDs và thay thế vào biến dưới đây

-- ============================================
-- BƯỚC 3: THAY THẾ USER IDs VÀ CHẠY SCRIPT
-- ============================================

-- Thay thế các giá trị này bằng user IDs thực tế
DO $$
DECLARE
  traveler_id UUID := 'PASTE_TRAVELER_ID_HERE';  -- Thay bằng ID thực
  guide_id UUID := 'PASTE_GUIDE_ID_HERE';        -- Thay bằng ID thực
  admin_id UUID := 'PASTE_ADMIN_ID_HERE';        -- Thay bằng ID thực
BEGIN
  -- ============================================
  -- TẠO/CẬP NHẬT PROFILES
  -- ============================================
  
  -- 1. Traveler Profile
  INSERT INTO profiles (id, email, full_name, phone, role, created_at, updated_at)
  VALUES (
    traveler_id,
    'traveler@test.com',
    'Nguyễn Văn A (Du khách)',
    '0901234567',
    'traveler',
    NOW(),
    NOW()
  )
  ON CONFLICT (id) DO UPDATE
  SET 
    role = 'traveler',
    full_name = 'Nguyễn Văn A (Du khách)',
    phone = '0901234567',
    updated_at = NOW();

  -- 2. Guide Profile
  INSERT INTO profiles (id, email, full_name, phone, role, created_at, updated_at)
  VALUES (
    guide_id,
    'guide@test.com',
    'Trần Thị B (Hướng dẫn viên)',
    '0912345678',
    'guide',
    NOW(),
    NOW()
  )
  ON CONFLICT (id) DO UPDATE
  SET 
    role = 'guide',
    full_name = 'Trần Thị B (Hướng dẫn viên)',
    phone = '0912345678',
    updated_at = NOW();

  -- 3. Admin Profile
  INSERT INTO profiles (id, email, full_name, phone, role, created_at, updated_at)
  VALUES (
    admin_id,
    'admin@test.com',
    'Lê Văn C (Quản trị viên)',
    '0923456789',
    'admin',
    NOW(),
    NOW()
  )
  ON CONFLICT (id) DO UPDATE
  SET 
    role = 'admin',
    full_name = 'Lê Văn C (Quản trị viên)',
    phone = '0923456789',
    updated_at = NOW();

  RAISE NOTICE 'Profiles created/updated successfully!';

  -- ============================================
  -- TẠO SAMPLE TOURS (CHO GUIDE)
  -- ============================================
  
  -- Tour 1: Hà Nội Phố Cổ
  INSERT INTO tours (
    guide_id, title, description, location, price, 
    duration_hours, max_participants, images, status
  )
  VALUES (
    guide_id,
    'Khám phá Hà Nội Phố Cổ',
    'Tour tham quan khu phố cổ Hà Nội với hướng dẫn viên địa phương. Khám phá lịch sử, văn hóa và ẩm thực đặc trưng của thủ đô ngàn năm văn hiến.',
    'Hà Nội',
    500000,
    4,
    15,
    ARRAY[
      'https://images.unsplash.com/photo-1555400038-63f5ba517a47?w=800',
      'https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800'
    ],
    'active'
  )
  ON CONFLICT DO NOTHING;

  -- Tour 2: Vịnh Hạ Long
  INSERT INTO tours (
    guide_id, title, description, location, price, 
    duration_hours, max_participants, images, status
  )
  VALUES (
    guide_id,
    'Vịnh Hạ Long 1 ngày',
    'Khám phá kỳ quan thiên nhiên thế giới Vịnh Hạ Long. Tham quan hang động, bơi lội, và thưởng thức hải sản tươi ngon trên du thuyền.',
    'Quảng Ninh',
    1500000,
    8,
    20,
    ARRAY[
      'https://images.unsplash.com/photo-1528127269322-539801943592?w=800',
      'https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?w=800'
    ],
    'active'
  )
  ON CONFLICT DO NOTHING;

  -- Tour 3: Sài Gòn về đêm
  INSERT INTO tours (
    guide_id, title, description, location, price, 
    duration_hours, max_participants, images, status
  )
  VALUES (
    guide_id,
    'Sài Gòn về đêm',
    'Trải nghiệm Sài Gòn về đêm với xe máy. Thưởng thức ẩm thực đường phố và khám phá cuộc sống về đêm sôi động của thành phố Hồ Chí Minh.',
    'Hồ Chí Minh',
    300000,
    3,
    10,
    ARRAY[
      'https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800'
    ],
    'active'
  )
  ON CONFLICT DO NOTHING;

  -- Tour 4: Hội An cổ kính
  INSERT INTO tours (
    guide_id, title, description, location, price, 
    duration_hours, max_participants, images, status
  )
  VALUES (
    guide_id,
    'Hội An cổ kính',
    'Dạo bước trong phố cổ Hội An, khám phá kiến trúc độc đáo, thưởng thức ẩm thực địa phương và tìm hiểu về lịch sử văn hóa.',
    'Quảng Nam',
    400000,
    5,
    12,
    ARRAY[
      'https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800'
    ],
    'active'
  )
  ON CONFLICT DO NOTHING;

  -- Tour 5: Đà Lạt lãng mạn
  INSERT INTO tours (
    guide_id, title, description, location, price, 
    duration_hours, max_participants, images, status
  )
  VALUES (
    guide_id,
    'Đà Lạt lãng mạn',
    'Khám phá thành phố ngàn hoa với khí hậu mát mẻ. Tham quan các điểm đến nổi tiếng và thưởng thức đặc sản Đà Lạt.',
    'Lâm Đồng',
    600000,
    6,
    15,
    ARRAY[
      'https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800'
    ],
    'active'
  )
  ON CONFLICT DO NOTHING;

  RAISE NOTICE 'Sample tours created successfully!';

END $$;

-- ============================================
-- BƯỚC 4: KIỂM TRA KẾT QUẢ
-- ============================================

-- Xem profiles đã tạo
SELECT 
  id,
  email,
  full_name,
  role,
  phone,
  created_at
FROM profiles
WHERE email IN ('traveler@test.com', 'guide@test.com', 'admin@test.com')
ORDER BY role;

-- Xem tours đã tạo
SELECT 
  t.id,
  t.title,
  t.location,
  t.price,
  t.duration_hours,
  p.full_name as guide_name,
  t.status
FROM tours t
JOIN profiles p ON t.guide_id = p.id
WHERE p.email = 'guide@test.com'
ORDER BY t.created_at DESC;

-- ============================================
-- THÔNG TIN TÀI KHOẢN MẪU
-- ============================================

/*
1. TRAVELER (Du khách)
   Email: traveler@test.com
   Password: Test123456
   Role: traveler
   Permissions:
   - Xem tours
   - Tạo booking
   - Tạo review
   - Sửa profile của mình

2. GUIDE (Hướng dẫn viên)
   Email: guide@test.com
   Password: Test123456
   Role: guide
   Permissions:
   - Tất cả quyền của traveler
   - Tạo/sửa/xóa tours
   - Xem bookings của tours mình tạo
   - Xác nhận bookings

3. ADMIN (Quản trị viên)
   Email: admin@test.com
   Password: Test123456
   Role: admin
   Permissions:
   - Tất cả quyền trong hệ thống
   - Quản lý users
   - Xem analytics
   - Sửa/xóa bất kỳ resource nào
*/

-- ============================================
-- NOTES
-- ============================================
-- 1. Đảm bảo email confirmation đã tắt trong Supabase Auth settings
-- 2. Nếu không thể đăng nhập, check RLS policies
-- 3. Images sử dụng Unsplash placeholder URLs
-- 4. Có thể thay đổi password trong Supabase Dashboard
