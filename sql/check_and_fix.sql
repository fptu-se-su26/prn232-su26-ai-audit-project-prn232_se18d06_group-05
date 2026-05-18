-- ============================================
-- CHECK & FIX SCRIPT
-- ============================================
-- Chạy từng phần để kiểm tra và fix vấn đề
-- ============================================

-- ============================================
-- PHẦN 1: KIỂM TRA USERS
-- ============================================

-- 1.1: Xem users trong auth.users
SELECT 
  id,
  email,
  email_confirmed_at,
  created_at
FROM auth.users
ORDER BY created_at DESC
LIMIT 10;

-- 1.2: Xem profiles
SELECT 
  id,
  email,
  full_name,
  role,
  created_at
FROM profiles
ORDER BY created_at DESC
LIMIT 10;

-- 1.3: Check users test có tồn tại không
SELECT 
  u.id as user_id,
  u.email,
  u.email_confirmed_at,
  p.id as profile_id,
  p.role
FROM auth.users u
LEFT JOIN profiles p ON u.id = p.id
WHERE u.email IN ('traveler@test.com', 'guide@test.com', 'admin@test.com')
ORDER BY u.email;

-- ============================================
-- PHẦN 2: FIX MISSING PROFILES
-- ============================================
-- Nếu user có trong auth.users nhưng không có profile

-- 2.1: Tìm users không có profile
SELECT 
  u.id,
  u.email,
  u.created_at
FROM auth.users u
LEFT JOIN profiles p ON u.id = p.id
WHERE p.id IS NULL
  AND u.email IN ('traveler@test.com', 'guide@test.com', 'admin@test.com');

-- 2.2: Tạo profiles cho users thiếu (nếu có)
-- Chỉ chạy nếu query 2.1 trả về kết quả

INSERT INTO profiles (id, email, full_name, role)
SELECT 
  u.id,
  u.email,
  CASE 
    WHEN u.email = 'traveler@test.com' THEN 'Nguyễn Văn A (Du khách)'
    WHEN u.email = 'guide@test.com' THEN 'Trần Thị B (Hướng dẫn viên)'
    WHEN u.email = 'admin@test.com' THEN 'Lê Văn C (Quản trị viên)'
    ELSE 'Test User'
  END,
  CASE 
    WHEN u.email = 'traveler@test.com' THEN 'traveler'
    WHEN u.email = 'guide@test.com' THEN 'guide'
    WHEN u.email = 'admin@test.com' THEN 'admin'
    ELSE 'traveler'
  END
FROM auth.users u
LEFT JOIN profiles p ON u.id = p.id
WHERE p.id IS NULL
  AND u.email IN ('traveler@test.com', 'guide@test.com', 'admin@test.com')
ON CONFLICT (id) DO NOTHING;

-- ============================================
-- PHẦN 3: UPDATE ROLES
-- ============================================

-- 3.1: Update guide role
UPDATE profiles 
SET role = 'guide', updated_at = NOW()
WHERE email = 'guide@test.com'
RETURNING id, email, role;

-- 3.2: Update admin role
UPDATE profiles 
SET role = 'admin', updated_at = NOW()
WHERE email = 'admin@test.com'
RETURNING id, email, role;

-- 3.3: Verify roles
SELECT email, full_name, role
FROM profiles
WHERE email IN ('traveler@test.com', 'guide@test.com', 'admin@test.com')
ORDER BY role;

-- ============================================
-- PHẦN 4: TẠO SAMPLE TOURS
-- ============================================

-- 4.1: Lấy guide ID
SELECT id, email, role 
FROM profiles 
WHERE email = 'guide@test.com';

-- 4.2: Tạo tours (copy guide ID từ query 4.1 và thay vào đây)
DO $$
DECLARE
  v_guide_id UUID;
  v_tour_count INT;
BEGIN
  -- Lấy guide ID
  SELECT id INTO v_guide_id 
  FROM profiles 
  WHERE email = 'guide@test.com' AND role = 'guide';
  
  IF v_guide_id IS NULL THEN
    RAISE EXCEPTION 'Guide not found! Email: guide@test.com';
  END IF;
  
  RAISE NOTICE 'Guide ID: %', v_guide_id;
  
  -- Xóa tours cũ của guide này (nếu có)
  DELETE FROM tours WHERE guide_id = v_guide_id;
  
  -- Tạo tours mới
  INSERT INTO tours (guide_id, title, description, location, price, duration_hours, max_participants, images, status)
  VALUES 
    (v_guide_id, 'Khám phá Hà Nội Phố Cổ', 'Tour tham quan khu phố cổ Hà Nội với hướng dẫn viên địa phương. Khám phá lịch sử, văn hóa và ẩm thực đặc trưng.', 'Hà Nội', 500000, 4, 15, ARRAY['https://images.unsplash.com/photo-1555400038-63f5ba517a47?w=800'], 'active'),
    (v_guide_id, 'Vịnh Hạ Long 1 ngày', 'Khám phá kỳ quan thiên nhiên thế giới Vịnh Hạ Long. Tham quan hang động, bơi lội, và thưởng thức hải sản.', 'Quảng Ninh', 1500000, 8, 20, ARRAY['https://images.unsplash.com/photo-1528127269322-539801943592?w=800'], 'active'),
    (v_guide_id, 'Sài Gòn về đêm', 'Trải nghiệm Sài Gòn về đêm với xe máy. Thưởng thức ẩm thực đường phố và khám phá cuộc sống về đêm.', 'Hồ Chí Minh', 300000, 3, 10, ARRAY['https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800'], 'active'),
    (v_guide_id, 'Hội An cổ kính', 'Dạo bước trong phố cổ Hội An, khám phá kiến trúc độc đáo và văn hóa địa phương.', 'Quảng Nam', 400000, 5, 12, ARRAY['https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800'], 'active'),
    (v_guide_id, 'Đà Lạt lãng mạn', 'Khám phá thành phố ngàn hoa với khí hậu mát mẻ. Tham quan các điểm đến nổi tiếng.', 'Lâm Đồng', 600000, 6, 15, ARRAY['https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800'], 'active');
  
  SELECT COUNT(*) INTO v_tour_count FROM tours WHERE guide_id = v_guide_id;
  RAISE NOTICE 'Created % tours for guide', v_tour_count;
END $$;

-- ============================================
-- PHẦN 5: VERIFY EVERYTHING
-- ============================================

-- 5.1: Check profiles và roles
SELECT 
  email,
  full_name,
  role,
  phone,
  created_at
FROM profiles
WHERE email IN ('traveler@test.com', 'guide@test.com', 'admin@test.com')
ORDER BY 
  CASE role
    WHEN 'traveler' THEN 1
    WHEN 'guide' THEN 2
    WHEN 'admin' THEN 3
  END;

-- 5.2: Check tours
SELECT 
  t.id,
  t.title,
  t.location,
  t.price,
  t.duration_hours,
  p.email as guide_email,
  p.role as guide_role,
  t.status
FROM tours t
JOIN profiles p ON t.guide_id = p.id
WHERE p.email = 'guide@test.com'
ORDER BY t.created_at DESC;

-- 5.3: Summary
SELECT 
  p.email,
  p.role,
  COUNT(t.id) as total_tours
FROM profiles p
LEFT JOIN tours t ON p.id = t.guide_id
WHERE p.email IN ('traveler@test.com', 'guide@test.com', 'admin@test.com')
GROUP BY p.email, p.role
ORDER BY p.role;

-- ============================================
-- DONE!
-- ============================================
-- Nếu tất cả queries chạy thành công:
-- ✅ 3 profiles với đúng roles
-- ✅ 5 tours cho guide
-- ✅ Sẵn sàng để test!
-- ============================================
