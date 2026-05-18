-- ============================================
-- QUICK SETUP - ONE COMMAND
-- ============================================
-- Copy và paste vào SQL Editor sau khi đăng ký 3 tài khoản
-- ============================================

-- Bước 1: Kiểm tra users đã tồn tại chưa
SELECT email, id FROM profiles 
WHERE email IN ('traveler@test.com', 'guide@test.com', 'admin@test.com')
ORDER BY email;

-- Nếu không thấy users, BẠN CẦN ĐĂNG KÝ QUA APP TRƯỚC!
-- Nếu đã thấy users, tiếp tục:

-- Bước 2: Update roles
UPDATE profiles SET role = 'guide', updated_at = NOW() 
WHERE email = 'guide@test.com';

UPDATE profiles SET role = 'admin', updated_at = NOW() 
WHERE email = 'admin@test.com';

-- Bước 3: Tạo tours cho guide
DO $$
DECLARE
  v_guide_id UUID;
BEGIN
  -- Lấy guide ID
  SELECT id INTO v_guide_id FROM profiles WHERE email = 'guide@test.com';
  
  -- Kiểm tra guide có tồn tại không
  IF v_guide_id IS NULL THEN
    RAISE EXCEPTION 'Guide user not found! Please register guide@test.com first.';
  END IF;
  
  RAISE NOTICE 'Creating tours for guide: %', v_guide_id;
  
  -- Tạo 5 tours mẫu
  INSERT INTO tours (guide_id, title, description, location, price, duration_hours, max_participants, images, status)
  VALUES 
    (v_guide_id, 'Khám phá Hà Nội Phố Cổ', 'Tour tham quan khu phố cổ Hà Nội với hướng dẫn viên địa phương', 'Hà Nội', 500000, 4, 15, ARRAY['https://images.unsplash.com/photo-1555400038-63f5ba517a47?w=800'], 'active'),
    (v_guide_id, 'Vịnh Hạ Long 1 ngày', 'Khám phá kỳ quan thiên nhiên thế giới Vịnh Hạ Long', 'Quảng Ninh', 1500000, 8, 20, ARRAY['https://images.unsplash.com/photo-1528127269322-539801943592?w=800'], 'active'),
    (v_guide_id, 'Sài Gòn về đêm', 'Trải nghiệm Sài Gòn về đêm với xe máy', 'Hồ Chí Minh', 300000, 3, 10, ARRAY['https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800'], 'active'),
    (v_guide_id, 'Hội An cổ kính', 'Dạo bước trong phố cổ Hội An', 'Quảng Nam', 400000, 5, 12, ARRAY['https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800'], 'active'),
    (v_guide_id, 'Đà Lạt lãng mạn', 'Khám phá thành phố ngàn hoa', 'Lâm Đồng', 600000, 6, 15, ARRAY['https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800'], 'active')
  ON CONFLICT DO NOTHING;
  
  RAISE NOTICE 'Setup completed! Guide has % tours', (SELECT COUNT(*) FROM tours WHERE guide_id = v_guide_id);
END $$;

-- Bước 4: Verify
SELECT 
  p.email,
  p.full_name,
  p.role,
  COUNT(t.id) as tours_count
FROM profiles p
LEFT JOIN tours t ON p.id = t.guide_id
WHERE p.email IN ('traveler@test.com', 'guide@test.com', 'admin@test.com')
GROUP BY p.id, p.email, p.full_name, p.role
ORDER BY p.role;

-- ============================================
-- DONE! 
-- ============================================
-- Giờ bạn có thể đăng nhập với:
-- - traveler@test.com / Test123456 (Traveler)
-- - guide@test.com / Test123456 (Guide với 5 tours)
-- - admin@test.com / Test123456 (Admin)
-- ============================================
