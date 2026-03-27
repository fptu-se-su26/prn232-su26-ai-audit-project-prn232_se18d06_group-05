# 🧪 Hướng dẫn Test Authentication Feature

## 📋 Chuẩn bị

### 1. Setup Supabase Database

Trước khi test, cần tạo bảng `profiles` trong Supabase:

```sql
-- Create profiles table
CREATE TABLE profiles (
  id UUID REFERENCES auth.users ON DELETE CASCADE PRIMARY KEY,
  email TEXT UNIQUE NOT NULL,
  full_name TEXT,
  phone TEXT,
  avatar_url TEXT,
  role TEXT DEFAULT 'traveler' CHECK (role IN ('traveler', 'guide', 'admin')),
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Enable RLS
ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;

-- Policies
CREATE POLICY "Public profiles are viewable by everyone"
  ON profiles FOR SELECT
  USING (true);

CREATE POLICY "Users can update own profile"
  ON profiles FOR UPDATE
  USING (auth.uid() = id);

CREATE POLICY "Users can insert own profile"
  ON profiles FOR INSERT
  WITH CHECK (auth.uid() = id);

-- Auto-create profile trigger
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
  INSERT INTO public.profiles (id, email, full_name)
  VALUES (
    NEW.id,
    NEW.email,
    NEW.raw_user_meta_data->>'full_name'
  );
  RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW
  EXECUTE FUNCTION public.handle_new_user();
```

### 2. Enable Email Auth trong Supabase

1. Vào Supabase Dashboard
2. Authentication → Providers
3. Enable "Email" provider
4. (Optional) Disable email confirmation để test nhanh hơn:
   - Authentication → Settings
   - Tắt "Enable email confirmations"

### 3. Chạy App

```bash
flutter pub get
flutter run
```

## ✅ Test Cases

### Test 1: Sign Up (Đăng ký)

#### Scenario: Đăng ký thành công
1. Mở app → Màn hình Login
2. Click "Đăng ký"
3. Nhập thông tin:
   - Họ tên: `Nguyễn Văn A`
   - Email: `test@example.com`
   - Mật khẩu: `password123`
   - Xác nhận mật khẩu: `password123`
4. Click "Đăng ký"

**Expected Result:**
- ✅ Loading indicator hiển thị
- ✅ SnackBar hiện "Đăng ký thành công!"
- ✅ Chuyển đến Home screen
- ✅ Hiển thị thông tin user

#### Scenario: Email đã tồn tại
1. Đăng ký với email đã dùng trước đó
2. Click "Đăng ký"

**Expected Result:**
- ✅ SnackBar hiện "Email đã được đăng ký"
- ✅ Vẫn ở màn hình Sign Up

#### Scenario: Validation errors
Test các trường hợp:
- Họ tên trống → "Vui lòng nhập họ tên"
- Email trống → "Email không được để trống"
- Email không hợp lệ → "Email không hợp lệ"
- Mật khẩu trống → "Mật khẩu không được để trống"
- Mật khẩu < 8 ký tự → "Mật khẩu phải có ít nhất 8 ký tự"
- Mật khẩu không khớp → "Mật khẩu không khớp"

**Expected Result:**
- ✅ Error message hiển thị dưới field tương ứng
- ✅ Không submit form

### Test 2: Sign In (Đăng nhập)

#### Scenario: Đăng nhập thành công
1. Mở app → Màn hình Login
2. Nhập thông tin:
   - Email: `test@example.com`
   - Mật khẩu: `password123`
3. Click "Đăng nhập"

**Expected Result:**
- ✅ Loading indicator hiển thị
- ✅ Chuyển đến Home screen
- ✅ Hiển thị thông tin user đúng

#### Scenario: Sai mật khẩu
1. Nhập email đúng, password sai
2. Click "Đăng nhập"

**Expected Result:**
- ✅ SnackBar hiện "Email hoặc mật khẩu không đúng"
- ✅ Vẫn ở màn hình Login

#### Scenario: Email không tồn tại
1. Nhập email chưa đăng ký
2. Click "Đăng nhập"

**Expected Result:**
- ✅ SnackBar hiện "Email hoặc mật khẩu không đúng"
- ✅ Vẫn ở màn hình Login

#### Scenario: Validation errors
- Email trống → "Email không được để trống"
- Email không hợp lệ → "Email không hợp lệ"
- Mật khẩu trống → "Mật khẩu không được để trống"

**Expected Result:**
- ✅ Error message hiển thị
- ✅ Không submit form

### Test 3: Sign Out (Đăng xuất)

#### Scenario: Đăng xuất thành công
1. Đăng nhập vào app
2. Ở Home screen, click icon logout (top right)
3. Dialog xác nhận hiện ra
4. Click "Đăng xuất"

**Expected Result:**
- ✅ Dialog đóng
- ✅ Chuyển về Login screen
- ✅ Session bị xóa

#### Scenario: Hủy đăng xuất
1. Click icon logout
2. Dialog hiện ra
3. Click "Hủy"

**Expected Result:**
- ✅ Dialog đóng
- ✅ Vẫn ở Home screen
- ✅ User vẫn đăng nhập

### Test 4: Session Persistence

#### Scenario: Session được lưu
1. Đăng nhập thành công
2. Đóng app (kill app)
3. Mở lại app

**Expected Result:**
- ✅ Không cần đăng nhập lại
- ✅ Tự động vào Home screen
- ✅ Thông tin user vẫn hiển thị

#### Scenario: Session sau khi logout
1. Đăng nhập
2. Đăng xuất
3. Đóng app
4. Mở lại app

**Expected Result:**
- ✅ Hiển thị Login screen
- ✅ Không tự động đăng nhập

### Test 5: UI/UX

#### Password visibility toggle
1. Ở màn hình Login/Sign Up
2. Click icon eye ở password field

**Expected Result:**
- ✅ Password hiển thị/ẩn
- ✅ Icon thay đổi (eye/eye-off)

#### Loading states
1. Submit form đăng nhập/đăng ký
2. Quan sát button

**Expected Result:**
- ✅ Button disabled khi loading
- ✅ Hiển thị CircularProgressIndicator
- ✅ Không thể click lại

#### Navigation
1. Từ Login → Click "Đăng ký"
2. Từ Sign Up → Click "Đăng nhập"

**Expected Result:**
- ✅ Chuyển màn hình đúng
- ✅ Form được reset

### Test 6: Error Handling

#### Network error
1. Tắt internet
2. Thử đăng nhập/đăng ký

**Expected Result:**
- ✅ Error message hiển thị
- ✅ App không crash

#### Supabase error
1. Sai Supabase URL/Key trong .env
2. Restart app

**Expected Result:**
- ✅ Error screen hiển thị
- ✅ Message rõ ràng

## 🔍 Debug Tips

### Check Logs
Xem console logs để debug:
```
ℹ️ INFO: Signing up user: test@example.com
✅ SUCCESS: User signed up: uuid-here
ℹ️ INFO: Getting current user: uuid-here
```

### Check Supabase Dashboard
1. Authentication → Users: Xem users đã tạo
2. Table Editor → profiles: Xem profile data
3. Logs: Xem API calls

### Common Issues

#### Issue: "Email đã được đăng ký" nhưng không thấy user
**Solution:** Check Supabase Dashboard → Authentication → Users

#### Issue: Profile không được tạo
**Solution:** 
- Check trigger `on_auth_user_created` đã tạo chưa
- Check RLS policies
- Xem logs trong Supabase

#### Issue: Session không persist
**Solution:**
- Check Supabase client initialization
- Verify auth flow type (PKCE)

#### Issue: "Lỗi máy chủ"
**Solution:**
- Check internet connection
- Verify Supabase URL/Key
- Check Supabase project status

## 📊 Test Checklist

### Functional Tests
- [ ] Sign up với thông tin hợp lệ
- [ ] Sign up với email đã tồn tại
- [ ] Sign up với validation errors
- [ ] Sign in với credentials đúng
- [ ] Sign in với credentials sai
- [ ] Sign in với validation errors
- [ ] Sign out thành công
- [ ] Cancel sign out
- [ ] Session persistence sau restart
- [ ] Session cleared sau logout

### UI/UX Tests
- [ ] Password visibility toggle
- [ ] Loading states
- [ ] Error messages display
- [ ] Navigation giữa screens
- [ ] Form validation real-time
- [ ] Button states (enabled/disabled)

### Edge Cases
- [ ] Network offline
- [ ] Supabase down
- [ ] Invalid credentials
- [ ] Empty fields
- [ ] Special characters trong password
- [ ] Very long inputs

## 🎯 Performance Tests

### Load Time
- [ ] App startup < 3 seconds
- [ ] Login response < 2 seconds
- [ ] Sign up response < 3 seconds

### Memory
- [ ] No memory leaks
- [ ] Controllers disposed properly

## 📱 Platform Tests

### Android
- [ ] All tests pass
- [ ] Back button behavior
- [ ] Keyboard handling

### iOS
- [ ] All tests pass
- [ ] Swipe back gesture
- [ ] Keyboard handling

### Web
- [ ] All tests pass
- [ ] Browser back button
- [ ] Responsive design

## ✅ Sign-off

Sau khi test xong tất cả:
- [ ] All functional tests passed
- [ ] All UI/UX tests passed
- [ ] All edge cases handled
- [ ] Performance acceptable
- [ ] All platforms tested
- [ ] Documentation updated

---

**Tester**: _______________  
**Date**: _______________  
**Status**: ⬜ Pass / ⬜ Fail  
**Notes**: _______________
