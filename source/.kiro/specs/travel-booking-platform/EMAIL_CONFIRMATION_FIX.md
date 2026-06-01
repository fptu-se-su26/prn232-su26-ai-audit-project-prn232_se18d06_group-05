# Fix Email Confirmation Issue

## ❌ Vấn đề
```
PostgrestException: Cannot coerce the result to a single JSON object
The result contains 0 rows
```

## 🔍 Nguyên nhân chính
Supabase mặc định yêu cầu **xác nhận email** trước khi user có thể đăng nhập. Khi user đăng ký:
1. User được tạo trong `auth.users` nhưng chưa được kích hoạt
2. Profile không được tạo vì user chưa xác nhận email
3. Code query profile → trả về 0 rows → lỗi

## ✅ Giải pháp

### Bước 1: Tắt xác nhận email trong Supabase (Cho môi trường Dev)

1. Truy cập Supabase Dashboard:
   ```
   https://supabase.com/dashboard/project/nvbvvowyjzylllswhynv/auth/providers
   ```

2. Vào **Authentication** → **Providers** → **Email**

3. Tìm phần **"Confirm email"** và **TẮT** (disable)

4. Click **Save**

### Bước 2: Code đã được cải thiện

**File:** `lib/features/auth/data/datasources/auth_remote_datasource.dart`

#### Thay đổi 1: Thêm delay và log
```dart
Logger.info('User email confirmed: ${response.user!.emailConfirmedAt != null}');

// Wait a bit for database to sync
await Future.delayed(const Duration(milliseconds: 500));
```

#### Thay đổi 2: Dùng upsert thay vì insert
```dart
// Trước (có thể lỗi duplicate key)
await client.from('profiles').insert({...});

// Sau (xử lý cả insert và update)
await client.from('profiles').upsert({
  'id': userId,
  'email': email,
  'full_name': fullName,
  'role': 'traveler',
  'created_at': DateTime.now().toIso8601String(),
  'updated_at': DateTime.now().toIso8601String(),
}, onConflict: 'id');
```

#### Thay đổi 3: Xử lý lỗi tốt hơn
```dart
catch (e) {
  Logger.error('Auth error during sign up', e);
  if (e is AppAuthException) rethrow;
  if (e is ServerException) rethrow;  // Thêm dòng này
  
  final errorMessage = e.toString();
  throw AppAuthException(message: _getAuthErrorMessage(errorMessage));
}
```

## 🧪 Test lại

### 1. Xóa user cũ (nếu có)
Vào Supabase Dashboard:
```
Authentication > Users > [Chọn user] > Delete user
```

### 2. Xóa profile cũ (nếu có)
Vào Table Editor:
```
profiles > [Chọn row] > Delete
```

### 3. Chạy lại app
```bash
flutter clean
flutter pub get
flutter run
```

### 4. Đăng ký tài khoản mới
- Email: test@example.com
- Password: Test123456
- Full Name: Test User

### 5. Kiểm tra logs
Logs thành công sẽ như sau:
```
✅ Signing up user: test@example.com
✅ User signed up: [uuid]
✅ User email confirmed: true
✅ Creating profile for user: [uuid]
✅ Profile created/updated successfully
✅ User signed in: [uuid]
```

## 📊 Kiểm tra Database

### 1. Kiểm tra auth.users
```sql
SELECT id, email, email_confirmed_at, created_at 
FROM auth.users 
ORDER BY created_at DESC 
LIMIT 1;
```

Kết quả mong đợi:
- `email_confirmed_at` phải có giá trị (không null)

### 2. Kiểm tra profiles
```sql
SELECT * FROM profiles ORDER BY created_at DESC LIMIT 1;
```

Kết quả mong đợi:
- Profile với cùng `id` như user
- `email`, `full_name`, `role` đều có giá trị

## 🔄 Luồng hoạt động mới

```
1. User nhập thông tin đăng ký
   ↓
2. Gọi client.auth.signUp()
   ↓
3. Supabase tạo user (email_confirmed_at = NOW vì đã tắt confirm)
   ↓
4. Trigger tự động tạo profile (backup)
   ↓
5. Code tạo profile thủ công (upsert)
   ↓
6. Delay 500ms để database sync
   ↓
7. Query profile → Thành công!
   ↓
8. User được đăng nhập tự động
```

## 🚨 Lưu ý quan trọng

### Môi trường Production
Trong production, BẬT LẠI email confirmation:
1. Enable "Confirm email" trong Supabase
2. Cấu hình email template
3. Xử lý flow xác nhận email trong app

### Xử lý email chưa xác nhận
Nếu giữ email confirmation, thêm logic:
```dart
if (response.user!.emailConfirmedAt == null) {
  throw AppAuthException(
    message: 'Vui lòng kiểm tra email để xác nhận tài khoản',
  );
}
```

## 🎯 Kết quả

- ✅ User có thể đăng ký ngay lập tức
- ✅ Profile được tạo tự động
- ✅ Không còn lỗi "0 rows"
- ✅ Code xử lý cả trigger và manual creation
- ✅ Sẵn sàng cho development

## 📚 Tài liệu tham khảo

- [Supabase Auth Configuration](https://supabase.com/docs/guides/auth/auth-email)
- [Row Level Security](https://supabase.com/docs/guides/auth/row-level-security)
- [Database Triggers](https://supabase.com/docs/guides/database/postgres/triggers)
