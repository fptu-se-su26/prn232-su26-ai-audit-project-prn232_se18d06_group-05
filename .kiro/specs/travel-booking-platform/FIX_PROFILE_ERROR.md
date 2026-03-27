# Fix Profile Error - Hướng dẫn

## ❌ Lỗi gặp phải
```
PostgrestException: Cannot coerce the result to a single JSON object
The result contains 0 rows
```

## 🔍 Nguyên nhân
Sau khi user đăng ký, trigger tự động tạo profile có thể chưa kịp chạy, dẫn đến code query profile trả về 0 rows.

## ✅ Giải pháp đã áp dụng

### 1. Tạo profile thủ công trong code
Thay vì chỉ dựa vào trigger, code giờ sẽ tự tạo profile ngay sau khi user đăng ký thành công.

**File đã sửa:** `lib/features/auth/data/datasources/auth_remote_datasource.dart`

**Thay đổi:**
- Thêm method `_createUserProfile()` để tạo profile thủ công
- Gọi `_createUserProfile()` trong `signUp()` trước khi query profile
- Xử lý trường hợp profile đã tồn tại (từ trigger) để tránh lỗi duplicate

### 2. Code mới

```dart
// Trong signUp()
await _createUserProfile(
  userId: response.user!.id,
  email: email,
  fullName: fullName,
);

// Method mới
Future<void> _createUserProfile({
  required String userId,
  required String email,
  required String fullName,
}) async {
  try {
    await client.from('profiles').insert({
      'id': userId,
      'email': email,
      'full_name': fullName,
      'role': 'traveler',
    });
  } catch (e) {
    // Nếu profile đã tồn tại (từ trigger), bỏ qua lỗi
    if (e.toString().contains('duplicate key')) {
      return;
    }
    throw ServerException(message: 'Không thể tạo hồ sơ người dùng');
  }
}
```

## 🧪 Test lại

1. Xóa user cũ trong Supabase Dashboard (nếu có):
   - Vào Authentication > Users
   - Xóa user test

2. Chạy lại app:
   ```bash
   flutter run
   ```

3. Thử đăng ký tài khoản mới

4. Kiểm tra logs để xác nhận:
   - ✅ "User signed up: [user_id]"
   - ✅ "Creating profile for user: [user_id]"
   - ✅ "Profile created successfully"
   - ✅ "User signed in: [user_id]"

## 📊 Kiểm tra Database

Vào Supabase Dashboard > Table Editor > profiles để xem profile đã được tạo.

## 🔄 Trigger vẫn hoạt động

Trigger `on_auth_user_created` vẫn được giữ lại như một backup. Nếu code tạo profile thất bại, trigger sẽ tạo.

## 💡 Lưu ý

- Code giờ đã xử lý cả 2 trường hợp: tạo profile thủ công và từ trigger
- Không có lỗi duplicate key vì code đã catch và bỏ qua
- Đáng tin cậy hơn vì không phụ thuộc vào timing của trigger
