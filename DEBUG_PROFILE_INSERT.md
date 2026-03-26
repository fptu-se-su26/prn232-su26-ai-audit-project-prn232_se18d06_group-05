# Debug Profile Insert Issue

## 🔍 Vấn đề
User đăng ký thành công nhưng profile không được insert vào bảng `profiles`.

## 🛠️ Các bước debug

### Bước 1: Kiểm tra logs trong app

Chạy app và xem console logs khi đăng ký:

```
✅ Signing up user: test@example.com
✅ User signed up: [uuid]
✅ User email confirmed: true
✅ Creating profile for user: [uuid]
✅ Email: test@example.com, Full name: Test User
✅ Current auth user: [uuid]
✅ Auth session exists: true
❌ Error creating user profile: [error message]
```

**Nếu thấy lỗi**, note lại error message và code.

### Bước 2: Kiểm tra Supabase Dashboard

#### 2.1 Kiểm tra auth.users
1. Vào: https://supabase.com/dashboard/project/nvbvvowyjzylllswhynv/auth/users
2. Xem user vừa đăng ký có trong danh sách không
3. Check `email_confirmed_at` có giá trị không (không null)

#### 2.2 Kiểm tra profiles table
1. Vào: https://supabase.com/dashboard/project/nvbvvowyjzylllswhynv/editor
2. Click vào table `profiles`
3. Xem có row với `id` = user id không

### Bước 3: Kiểm tra RLS Policies

Chạy SQL này trong SQL Editor:

```sql
-- Xem policies hiện tại
SELECT 
  policyname,
  cmd,
  qual,
  with_check
FROM pg_policies 
WHERE tablename = 'profiles';
```

**Kết quả mong đợi:**
- `Public profiles are viewable by everyone` - SELECT
- `Users can insert own profile` - INSERT
- `Users can update own profile` - UPDATE

**Nếu thiếu policy INSERT**, chạy:

```sql
CREATE POLICY "Users can insert own profile"
  ON profiles FOR INSERT
  WITH CHECK (auth.uid() = id);
```

### Bước 4: Test insert trực tiếp

Trong SQL Editor, test insert với user ID thực:

```sql
-- Lấy user ID mới nhất
SELECT id, email FROM auth.users ORDER BY created_at DESC LIMIT 1;

-- Test insert (thay YOUR_USER_ID)
INSERT INTO profiles (id, email, full_name, role)
VALUES (
  'YOUR_USER_ID',
  'test@example.com',
  'Test User',
  'traveler'
);
```

**Nếu lỗi:**
- `new row violates row-level security policy` → RLS policy sai
- `duplicate key value` → Profile đã tồn tại
- `violates foreign key constraint` → User không tồn tại trong auth.users

### Bước 5: Kiểm tra trigger

Xem trigger có hoạt động không:

```sql
-- Xem trigger
SELECT * FROM pg_trigger WHERE tgname = 'on_auth_user_created';

-- Xem function
SELECT proname, prosrc 
FROM pg_proc 
WHERE proname = 'handle_new_user';
```

### Bước 6: Test với RLS tắt (Temporary)

**CHỈ CHO DEV - KHÔNG DÙNG PRODUCTION!**

```sql
-- Tắt RLS tạm thời
ALTER TABLE profiles DISABLE ROW LEVEL SECURITY;

-- Test đăng ký lại
-- ...

-- Xem có insert được không
SELECT * FROM profiles ORDER BY created_at DESC;

-- BẬT LẠI RLS
ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;
```

## 🔧 Các giải pháp phổ biến

### Giải pháp 1: Fix RLS Policy

Chạy file `fix_profiles_rls.sql`:

```sql
-- Xóa policies cũ
DROP POLICY IF EXISTS "Users can insert own profile" ON profiles;

-- Tạo lại policy đúng
CREATE POLICY "Users can insert own profile"
  ON profiles FOR INSERT
  WITH CHECK (auth.uid() = id);
```

### Giải pháp 2: Sử dụng Service Role Key (Bypass RLS)

**Trong code** (không khuyến khích cho production):

```dart
// Tạo client với service role key (bypass RLS)
final serviceClient = SupabaseClient(
  'YOUR_SUPABASE_URL',
  'YOUR_SERVICE_ROLE_KEY', // Service role key, không phải anon key
);

// Dùng serviceClient để insert profile
await serviceClient.from('profiles').insert({...});
```

### Giải pháp 3: Dùng Database Function

Tạo function với SECURITY DEFINER (bypass RLS):

```sql
CREATE OR REPLACE FUNCTION create_profile(
  user_id UUID,
  user_email TEXT,
  user_full_name TEXT
)
RETURNS void AS $$
BEGIN
  INSERT INTO profiles (id, email, full_name, role)
  VALUES (user_id, user_email, user_full_name, 'traveler')
  ON CONFLICT (id) DO UPDATE
  SET 
    email = EXCLUDED.email,
    full_name = EXCLUDED.full_name,
    updated_at = NOW();
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Grant execute permission
GRANT EXECUTE ON FUNCTION create_profile TO authenticated;
```

Trong code:

```dart
await client.rpc('create_profile', params: {
  'user_id': userId,
  'user_email': email,
  'user_full_name': fullName,
});
```

### Giải pháp 4: Đơn giản hóa Policy

Thay vì check `auth.uid() = id`, cho phép tất cả authenticated users:

```sql
DROP POLICY IF EXISTS "Users can insert own profile" ON profiles;

CREATE POLICY "Authenticated users can insert profiles"
  ON profiles FOR INSERT
  TO authenticated
  WITH CHECK (true);
```

## 📊 Checklist Debug

- [ ] User được tạo trong `auth.users`
- [ ] `email_confirmed_at` không null
- [ ] RLS đang bật cho table `profiles`
- [ ] Policy INSERT tồn tại
- [ ] Policy INSERT có điều kiện đúng
- [ ] Auth session tồn tại khi insert
- [ ] `auth.uid()` trả về đúng user ID
- [ ] Không có lỗi trong logs
- [ ] Trigger `on_auth_user_created` tồn tại
- [ ] Function `handle_new_user` tồn tại

## 🚨 Lỗi phổ biến

### Lỗi 1: "new row violates row-level security policy"
**Nguyên nhân:** RLS policy không cho phép insert

**Giải pháp:**
```sql
-- Check policy
SELECT * FROM pg_policies WHERE tablename = 'profiles' AND cmd = 'INSERT';

-- Fix policy
CREATE POLICY "Users can insert own profile"
  ON profiles FOR INSERT
  WITH CHECK (auth.uid() = id);
```

### Lỗi 2: "auth.uid() is null"
**Nguyên nhân:** User chưa được authenticate khi insert

**Giải pháp:**
- Đảm bảo `signUp()` trả về session
- Check `client.auth.currentUser` không null
- Thêm delay sau signUp

### Lỗi 3: "duplicate key value violates unique constraint"
**Nguyên nhân:** Profile đã tồn tại (từ trigger hoặc insert trước đó)

**Giải pháp:**
- Dùng `upsert` thay vì `insert`
- Hoặc check tồn tại trước khi insert

### Lỗi 4: "violates foreign key constraint"
**Nguyên nhân:** User ID không tồn tại trong `auth.users`

**Giải pháp:**
- Đảm bảo user được tạo thành công
- Check `response.user` không null

## 📝 Logs mẫu thành công

```
[INFO] Signing up user: test@example.com
[SUCCESS] User signed up: 550e8400-e29b-41d4-a716-446655440000
[INFO] User email confirmed: true
[INFO] Creating profile for user: 550e8400-e29b-41d4-a716-446655440000
[INFO] Email: test@example.com, Full name: Test User
[INFO] Current auth user: 550e8400-e29b-41d4-a716-446655440000
[INFO] Auth session exists: true
[SUCCESS] Profile created/updated successfully
[INFO] Profile response: [{id: 550e8400-e29b-41d4-a716-446655440000, email: test@example.com, ...}]
```

## 🎯 Next Steps

1. Chạy app với logs mới
2. Đăng ký user mới
3. Copy error message (nếu có)
4. Chạy `fix_profiles_rls.sql`
5. Test lại

Nếu vẫn không work, gửi cho tôi:
- Error logs đầy đủ
- Screenshot Supabase policies
- User ID đang test
