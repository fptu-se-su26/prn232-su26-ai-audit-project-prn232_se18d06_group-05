# 📝 Hướng dẫn từng bước tạo tài khoản test

## ⚠️ QUAN TRỌNG: Đọc trước khi bắt đầu

Bạn PHẢI đăng ký tài khoản qua app trước, sau đó mới chạy SQL để update role.

---

## 🚀 Bước 1: Đăng ký 3 tài khoản qua app

Chạy app Flutter:
```bash
flutter run
```

Đăng ký 3 tài khoản:

### Tài khoản 1: Traveler
- Email: `traveler@test.com`
- Password: `Test123456`
- Full Name: `Nguyễn Văn A`

### Tài khoản 2: Guide
- Email: `guide@test.com`
- Password: `Test123456`
- Full Name: `Trần Thị B`

### Tài khoản 3: Admin
- Email: `admin@test.com`
- Password: `Test123456`
- Full Name: `Lê Văn C`

**Lưu ý:** Nếu gặp lỗi khi đăng ký, xem file `DEBUG_PROFILE_INSERT.md`

---

## 🔍 Bước 2: Kiểm tra users đã được tạo

Vào Supabase SQL Editor:
https://supabase.com/dashboard/project/nvbvvowyjzylllswhynv/editor

Chạy query này:

```sql
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
```

**Kết quả mong đợi:**
- 3 rows
- Mỗi row có `user_id`, `profile_id` (không null)
- Role mặc định là `traveler`

**Nếu không thấy 3 rows:**
- Quay lại Bước 1 và đăng ký lại
- Check email confirmation đã tắt chưa

**Nếu `profile_id` là null:**
- Chạy file `check_and_fix.sql` phần 2

---

## ⚙️ Bước 3: Update roles

Trong SQL Editor, chạy:

```sql
-- Update guide role
UPDATE profiles 
SET role = 'guide', updated_at = NOW()
WHERE email = 'guide@test.com';

-- Update admin role
UPDATE profiles 
SET role = 'admin', updated_at = NOW()
WHERE email = 'admin@test.com';

-- Verify
SELECT email, full_name, role
FROM profiles
WHERE email IN ('traveler@test.com', 'guide@test.com', 'admin@test.com')
ORDER BY role;
```

**Kết quả mong đợi:**
```
admin@test.com    | Lê Văn C    | admin
guide@test.com    | Trần Thị B  | guide
traveler@test.com | Nguyễn Văn A | traveler
```

---

## 🏖️ Bước 4: Tạo sample tours cho guide

Chạy script này:

```sql
DO $$
DECLARE
  v_guide_id UUID;
BEGIN
  -- Lấy guide ID
  SELECT id INTO v_guide_id 
  FROM profiles 
  WHERE email = 'guide@test.com';
  
  -- Check guide tồn tại
  IF v_guide_id IS NULL THEN
    RAISE EXCEPTION 'Guide not found!';
  END IF;
  
  RAISE NOTICE 'Creating tours for guide: %', v_guide_id;
  
  -- Tạo 5 tours
  INSERT INTO tours (guide_id, title, description, location, price, duration_hours, max_participants, images, status)
  VALUES 
    (v_guide_id, 'Khám phá Hà Nội Phố Cổ', 'Tour tham quan khu phố cổ Hà Nội', 'Hà Nội', 500000, 4, 15, ARRAY['https://images.unsplash.com/photo-1555400038-63f5ba517a47?w=800'], 'active'),
    (v_guide_id, 'Vịnh Hạ Long 1 ngày', 'Khám phá kỳ quan thiên nhiên', 'Quảng Ninh', 1500000, 8, 20, ARRAY['https://images.unsplash.com/photo-1528127269322-539801943592?w=800'], 'active'),
    (v_guide_id, 'Sài Gòn về đêm', 'Trải nghiệm Sài Gòn về đêm', 'Hồ Chí Minh', 300000, 3, 10, ARRAY['https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800'], 'active'),
    (v_guide_id, 'Hội An cổ kính', 'Dạo bước trong phố cổ', 'Quảng Nam', 400000, 5, 12, ARRAY['https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800'], 'active'),
    (v_guide_id, 'Đà Lạt lãng mạn', 'Khám phá thành phố ngàn hoa', 'Lâm Đồng', 600000, 6, 15, ARRAY['https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800'], 'active');
  
  RAISE NOTICE 'Created 5 tours successfully!';
END $$;
```

**Kết quả mong đợi:**
```
NOTICE: Creating tours for guide: [uuid]
NOTICE: Created 5 tours successfully!
```

---

## ✅ Bước 5: Verify

Chạy query kiểm tra:

```sql
SELECT 
  p.email,
  p.role,
  COUNT(t.id) as total_tours
FROM profiles p
LEFT JOIN tours t ON p.id = t.guide_id
WHERE p.email IN ('traveler@test.com', 'guide@test.com', 'admin@test.com')
GROUP BY p.email, p.role
ORDER BY p.role;
```

**Kết quả mong đợi:**
```
admin@test.com    | admin    | 0
guide@test.com    | guide    | 5
traveler@test.com | traveler | 0
```

---

## 🧪 Bước 6: Test trong app

### Test 1: Traveler
1. Đăng nhập: `traveler@test.com` / `Test123456`
2. Vào Tour List Screen
3. ✅ Thấy 5 tours
4. ✅ KHÔNG thấy nút "+" ở AppBar
5. ✅ KHÔNG thấy nút edit/delete trên bất kỳ tour nào

### Test 2: Guide
1. Đăng xuất và đăng nhập: `guide@test.com` / `Test123456`
2. Vào Tour List Screen
3. ✅ Thấy 5 tours
4. ✅ Thấy nút "+" ở AppBar
5. ✅ Thấy nút edit/delete trên 5 tours của mình
6. Click nút edit → Thấy snackbar "Chỉnh sửa tour"
7. Click nút delete → Thấy confirmation dialog

### Test 3: Admin
1. Đăng xuất và đăng nhập: `admin@test.com` / `Test123456`
2. Vào Tour List Screen
3. ✅ Thấy 5 tours
4. ✅ Thấy nút "+" ở AppBar
5. ✅ Thấy nút edit/delete trên TẤT CẢ tours (kể cả không phải của mình)

---

## 🐛 Troubleshooting

### Lỗi: "Guide not found!"
**Nguyên nhân:** User `guide@test.com` chưa được đăng ký

**Giải pháp:**
1. Quay lại Bước 1
2. Đăng ký tài khoản `guide@test.com`
3. Chạy lại từ Bước 3

### Lỗi: "null value in column guide_id"
**Nguyên nhân:** Biến `v_guide_id` là null

**Giải pháp:**
```sql
-- Check guide có tồn tại không
SELECT id, email, role FROM profiles WHERE email = 'guide@test.com';

-- Nếu không có, đăng ký lại qua app
-- Nếu có nhưng role không phải 'guide', update:
UPDATE profiles SET role = 'guide' WHERE email = 'guide@test.com';
```

### Không thấy nút permission trong app
**Nguyên nhân:** Chưa đăng nhập lại sau khi update role

**Giải pháp:**
1. Đăng xuất khỏi app
2. Đăng nhập lại
3. Hoặc restart app: `flutter run`

### Tours không hiển thị
**Nguyên nhân:** Tours chưa được tạo hoặc status không phải 'active'

**Giải pháp:**
```sql
-- Check tours
SELECT * FROM tours WHERE guide_id IN (
  SELECT id FROM profiles WHERE email = 'guide@test.com'
);

-- Update status nếu cần
UPDATE tours SET status = 'active' WHERE status != 'active';
```

---

## 📋 Checklist

- [ ] Đăng ký 3 tài khoản qua app
- [ ] Verify 3 users trong database
- [ ] Update roles (guide, admin)
- [ ] Tạo 5 sample tours
- [ ] Verify tours được tạo
- [ ] Test đăng nhập với traveler
- [ ] Test đăng nhập với guide
- [ ] Test đăng nhập với admin
- [ ] Verify permissions hoạt động đúng

---

## 🎯 Quick Commands

### Xem tất cả users
```sql
SELECT email, role FROM profiles ORDER BY role;
```

### Xem tours của guide
```sql
SELECT t.title, t.location, t.price
FROM tours t
JOIN profiles p ON t.guide_id = p.id
WHERE p.email = 'guide@test.com';
```

### Reset role về traveler
```sql
UPDATE profiles SET role = 'traveler' WHERE email = 'YOUR_EMAIL';
```

### Xóa tất cả tours test
```sql
DELETE FROM tours WHERE guide_id IN (
  SELECT id FROM profiles WHERE email IN ('guide@test.com', 'admin@test.com')
);
```

---

## 📚 Files hỗ trợ

- `check_and_fix.sql` - Script kiểm tra và fix từng bước
- `quick_setup_roles.sql` - Script nhanh (sau khi đăng ký)
- `update_user_roles.sql` - Các query update role đơn giản
- `SAMPLE_ACCOUNTS_GUIDE.md` - Hướng dẫn chi tiết

---

Chúc bạn test thành công! 🎉
