# 🧪 Hướng dẫn tạo tài khoản mẫu để test

## 📋 Tổng quan

Có 2 cách để tạo tài khoản mẫu với các role khác nhau:
1. **Cách nhanh**: Đăng ký qua app, sau đó update role trong database
2. **Cách đầy đủ**: Tạo users trong Supabase Dashboard và chạy script

## 🚀 CÁCH 1: Nhanh nhất (Khuyên dùng)

### Bước 1: Đăng ký 3 tài khoản qua app

Chạy app và đăng ký 3 tài khoản:

1. **Traveler Account**
   - Email: `traveler@test.com`
   - Password: `Test123456`
   - Full Name: `Nguyễn Văn A`

2. **Guide Account**
   - Email: `guide@test.com`
   - Password: `Test123456`
   - Full Name: `Trần Thị B`

3. **Admin Account**
   - Email: `admin@test.com`
   - Password: `Test123456`
   - Full Name: `Lê Văn C`

### Bước 2: Update roles trong Supabase

Vào Supabase SQL Editor và chạy:

```sql
-- Update guide role
UPDATE profiles 
SET role = 'guide', updated_at = NOW()
WHERE email = 'guide@test.com';

-- Update admin role
UPDATE profiles 
SET role = 'admin', updated_at = NOW()
WHERE email = 'admin@test.com';

-- Traveler đã là role mặc định, không cần update
```

### Bước 3: Tạo sample tours cho guide

```sql
-- Lấy guide ID
SELECT id FROM profiles WHERE email = 'guide@test.com';

-- Thay GUIDE_ID bằng ID vừa lấy
INSERT INTO tours (guide_id, title, description, location, price, duration_hours, max_participants, images)
VALUES 
  (
    'GUIDE_ID',
    'Khám phá Hà Nội Phố Cổ',
    'Tour tham quan khu phố cổ Hà Nội với hướng dẫn viên địa phương',
    'Hà Nội',
    500000,
    4,
    15,
    ARRAY['https://images.unsplash.com/photo-1555400038-63f5ba517a47?w=800']
  ),
  (
    'GUIDE_ID',
    'Vịnh Hạ Long 1 ngày',
    'Khám phá kỳ quan thiên nhiên thế giới',
    'Quảng Ninh',
    1500000,
    8,
    20,
    ARRAY['https://images.unsplash.com/photo-1528127269322-539801943592?w=800']
  );
```

### Bước 4: Test

Đăng nhập lại với từng tài khoản và kiểm tra:

✅ **Traveler** (`traveler@test.com`):
- Xem được danh sách tours
- KHÔNG thấy nút "+" để tạo tour
- KHÔNG thấy nút edit/delete trên tour cards

✅ **Guide** (`guide@test.com`):
- Xem được danh sách tours
- Thấy nút "+" để tạo tour
- Thấy nút edit/delete trên tours của mình
- KHÔNG thấy nút edit/delete trên tours của người khác

✅ **Admin** (`admin@test.com`):
- Xem được danh sách tours
- Thấy nút "+" để tạo tour
- Thấy nút edit/delete trên TẤT CẢ tours

---

## 🔧 CÁCH 2: Tạo trong Supabase Dashboard

### Bước 1: Tạo users trong Dashboard

1. Vào: https://supabase.com/dashboard/project/nvbvvowyjzylllswhynv/auth/users
2. Click "Add user" → "Create new user"
3. Tạo 3 users:

**User 1 - Traveler:**
- Email: `traveler@test.com`
- Password: `Test123456`
- Auto Confirm User: ✅ (check)

**User 2 - Guide:**
- Email: `guide@test.com`
- Password: `Test123456`
- Auto Confirm User: ✅ (check)

**User 3 - Admin:**
- Email: `admin@test.com`
- Password: `Test123456`
- Auto Confirm User: ✅ (check)

### Bước 2: Lấy User IDs

Vào SQL Editor và chạy:

```sql
SELECT id, email 
FROM auth.users 
WHERE email IN ('traveler@test.com', 'guide@test.com', 'admin@test.com')
ORDER BY email;
```

Copy 3 IDs.

### Bước 3: Chạy script tạo profiles và tours

Mở file `create_sample_accounts.sql`, thay thế 3 IDs:

```sql
DO $$
DECLARE
  traveler_id UUID := 'PASTE_TRAVELER_ID_HERE';  -- Thay bằng ID thực
  guide_id UUID := 'PASTE_GUIDE_ID_HERE';        -- Thay bằng ID thực
  admin_id UUID := 'PASTE_ADMIN_ID_HERE';        -- Thay bằng ID thực
BEGIN
  -- Script sẽ tạo profiles và tours
  ...
END $$;
```

Chạy script trong SQL Editor.

### Bước 4: Verify

```sql
-- Xem profiles
SELECT email, full_name, role 
FROM profiles
WHERE email IN ('traveler@test.com', 'guide@test.com', 'admin@test.com');

-- Xem tours
SELECT t.title, p.email as guide_email
FROM tours t
JOIN profiles p ON t.guide_id = p.id
WHERE p.email = 'guide@test.com';
```

---

## 🎯 Test Scenarios

### Scenario 1: Traveler không thể tạo tour

1. Đăng nhập: `traveler@test.com`
2. Vào Tour List Screen
3. ✅ KHÔNG thấy nút "+" ở AppBar
4. ✅ KHÔNG thấy nút edit/delete trên bất kỳ tour nào

### Scenario 2: Guide có thể quản lý tours của mình

1. Đăng nhập: `guide@test.com`
2. Vào Tour List Screen
3. ✅ Thấy nút "+" ở AppBar
4. ✅ Thấy nút edit/delete trên tours của mình
5. ✅ KHÔNG thấy nút edit/delete trên tours của người khác

### Scenario 3: Admin có thể quản lý tất cả

1. Đăng nhập: `admin@test.com`
2. Vào Tour List Screen
3. ✅ Thấy nút "+" ở AppBar
4. ✅ Thấy nút edit/delete trên TẤT CẢ tours

### Scenario 4: Chuyển đổi role

```sql
-- Chuyển traveler thành guide
UPDATE profiles 
SET role = 'guide' 
WHERE email = 'traveler@test.com';
```

Đăng nhập lại → Thấy nút "+" xuất hiện

---

## 📊 Quick Reference

### Xem tất cả users và roles

```sql
SELECT 
  p.email,
  p.full_name,
  p.role,
  COUNT(t.id) as total_tours
FROM profiles p
LEFT JOIN tours t ON p.id = t.guide_id
GROUP BY p.id, p.email, p.full_name, p.role
ORDER BY p.role;
```

### Update role nhanh

```sql
-- Thành guide
UPDATE profiles SET role = 'guide' WHERE email = 'YOUR_EMAIL';

-- Thành admin
UPDATE profiles SET role = 'admin' WHERE email = 'YOUR_EMAIL';

-- Về traveler
UPDATE profiles SET role = 'traveler' WHERE email = 'YOUR_EMAIL';
```

### Xóa tất cả sample data

```sql
-- Xóa tours của guide test
DELETE FROM tours 
WHERE guide_id IN (
  SELECT id FROM profiles 
  WHERE email IN ('guide@test.com', 'admin@test.com')
);

-- Xóa profiles test
DELETE FROM profiles 
WHERE email IN ('traveler@test.com', 'guide@test.com', 'admin@test.com');

-- Xóa auth users (trong Dashboard hoặc SQL)
-- Vào Authentication > Users > Delete
```

---

## 🐛 Troubleshooting

### Lỗi: "Cannot insert into profiles"

**Nguyên nhân:** RLS policy chặn insert

**Giải pháp:**
```sql
-- Tạm tắt RLS (CHỈ CHO DEV)
ALTER TABLE profiles DISABLE ROW LEVEL SECURITY;

-- Chạy lại script

-- Bật lại RLS
ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;
```

### Lỗi: "User already exists"

**Nguyên nhân:** Email đã được đăng ký

**Giải pháp:**
- Xóa user cũ trong Dashboard
- Hoặc dùng email khác

### Không thấy nút permission

**Nguyên nhân:** Role chưa được update hoặc chưa đăng nhập lại

**Giải pháp:**
1. Check role trong database:
```sql
SELECT email, role FROM profiles WHERE email = 'YOUR_EMAIL';
```

2. Đăng xuất và đăng nhập lại
3. Clear app cache: `flutter clean && flutter run`

### Tours không hiển thị

**Nguyên nhân:** Tours chưa được tạo hoặc status không phải 'active'

**Giải pháp:**
```sql
-- Check tours
SELECT * FROM tours WHERE guide_id = 'GUIDE_ID';

-- Update status
UPDATE tours SET status = 'active' WHERE status != 'active';
```

---

## 📝 Thông tin tài khoản mẫu

| Role | Email | Password | Permissions |
|------|-------|----------|-------------|
| Traveler | traveler@test.com | Test123456 | Xem tours, tạo booking, review |
| Guide | guide@test.com | Test123456 | + Tạo/sửa/xóa tours, quản lý bookings |
| Admin | admin@test.com | Test123456 | Tất cả quyền |

---

## ✅ Checklist

- [ ] Tắt email confirmation trong Supabase
- [ ] Đăng ký 3 tài khoản qua app
- [ ] Update roles trong database
- [ ] Tạo sample tours cho guide
- [ ] Test đăng nhập với từng role
- [ ] Verify permissions hoạt động đúng
- [ ] Test chuyển đổi roles

---

## 🎓 Tips

1. **Dùng email test riêng**: Tránh dùng email thật để test
2. **Bookmark SQL queries**: Lưu các query thường dùng
3. **Test trên nhiều devices**: Kiểm tra trên web và mobile
4. **Clear cache khi đổi role**: Đảm bảo UI update đúng
5. **Check logs**: Xem console logs để debug permissions

Chúc bạn test vui vẻ! 🚀
