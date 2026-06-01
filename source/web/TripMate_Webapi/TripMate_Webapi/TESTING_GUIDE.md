# 🧪 Testing Guide - MVC Views

## 🚀 Quick Start

### 1. Stop Current Application
Ứng dụng đang chạy cần được dừng lại:
- Nhấn `Ctrl+C` trong terminal đang chạy app
- Hoặc kill process 24968

### 2. Rebuild & Run
```bash
cd source/web/TripMate_Webapi/TripMate_Webapi
dotnet build
dotnet run
```

### 3. Open Browser
Mở trình duyệt và truy cập:
- **Home**: http://localhost:5000/
- **Login**: http://localhost:5000/Auth/Login
- **Register**: http://localhost:5000/Auth/Register

## 📋 Test Scenarios

### Scenario 1: View Home Page
1. Navigate to http://localhost:5000/
2. ✅ Verify logo AVATAR.png hiển thị
3. ✅ Verify hero section với search bar
4. ✅ Verify category filters
5. ✅ Verify featured tours (nếu có data trong database)
6. ✅ Verify curated stays section
7. ✅ Verify "Đăng nhập" button trong header (nếu chưa login)

### Scenario 2: Register New Account
1. Navigate to http://localhost:5000/Auth/Register
2. Fill form:
   - Họ và tên: `Nguyễn Văn A`
   - Email: `test@example.com`
   - Mật khẩu: `password123`
   - Xác nhận mật khẩu: `password123`
   - Vai trò: Select **Du khách**
   - Check "Tôi đồng ý với điều khoản"
3. Click "Đăng ký"
4. ✅ Verify spinner hiển thị
5. ✅ Verify redirect to `/personality-survey.html` (for traveler)
6. ✅ Verify localStorage có:
   - `accessToken`
   - `userEmail`
   - `userRole` = "traveler"

### Scenario 3: Login Existing Account
1. Navigate to http://localhost:5000/Auth/Login
2. Fill form:
   - Email: `test@example.com`
   - Mật khẩu: `password123`
3. Click "Đăng nhập"
4. ✅ Verify spinner hiển thị
5. ✅ Verify redirect based on role:
   - **Traveler** (chưa làm survey) → `/personality-survey.html`
   - **Traveler** (đã làm survey) → `/`
   - **Guide** → `/dashboard.html`
   - **Admin** → `/Admin/Dashboard`

### Scenario 4: Register as Guide
1. Navigate to http://localhost:5000/Auth/Register
2. Fill form với vai trò: **Hướng dẫn viên**
3. Click "Đăng ký"
4. ✅ Verify redirect to `/dashboard.html` (guide dashboard)
5. ✅ Verify localStorage có `userRole` = "guide"

### Scenario 5: Password Toggle
1. Navigate to http://localhost:5000/Auth/Login
2. Type password in password field
3. Click eye icon
4. ✅ Verify password becomes visible
5. Click eye icon again
6. ✅ Verify password becomes hidden

### Scenario 6: Form Validation
1. Navigate to http://localhost:5000/Auth/Register
2. Try to submit empty form
3. ✅ Verify browser validation messages
4. Fill password and confirm password with different values
5. Click "Đăng ký"
6. ✅ Verify error message: "Mật khẩu xác nhận không khớp!"

### Scenario 7: Error Handling
1. Navigate to http://localhost:5000/Auth/Login
2. Enter invalid credentials:
   - Email: `wrong@example.com`
   - Password: `wrongpassword`
3. Click "Đăng nhập"
4. ✅ Verify error message displays
5. ✅ Verify spinner hides after error

### Scenario 8: Responsive Design
1. Open any page
2. Resize browser window to mobile size (375px)
3. ✅ Verify layout adapts to mobile
4. ✅ Verify navigation menu works
5. ✅ Verify forms are usable on mobile

## 🔍 What to Check

### Visual Design
- ✅ Orange theme (#ff7a00) throughout
- ✅ Plus Jakarta Sans font loaded
- ✅ Material Icons display correctly
- ✅ Glass morphism effect on auth pages
- ✅ Smooth animations and transitions
- ✅ Consistent spacing and alignment

### Functionality
- ✅ Forms submit correctly
- ✅ API calls work (check Network tab)
- ✅ localStorage saves data
- ✅ Redirects work correctly
- ✅ Error messages display
- ✅ Spinner shows during loading

### Data Integration
- ✅ Home page loads tours from TourService
- ✅ Tour cards display correct data
- ✅ Images load (or fallback images work)
- ✅ Prices format correctly (with ₫ symbol)
- ✅ Ratings display

## 🐛 Common Issues

### Issue 1: Tours Not Displaying
**Symptom**: Home page shows "Chưa có tour nào"

**Possible Causes**:
- Database empty
- RLS policies blocking access
- TourService error

**Solution**:
1. Check browser console for errors
2. Check server logs
3. Verify database has tours
4. Check RLS policies

### Issue 2: Login Fails
**Symptom**: Error message after login attempt

**Possible Causes**:
- Wrong credentials
- API endpoint not working
- CORS issue

**Solution**:
1. Check Network tab for API response
2. Verify credentials in database
3. Check server logs
4. Verify CORS policy

### Issue 3: Redirect Not Working
**Symptom**: Stays on same page after login/register

**Possible Causes**:
- JavaScript error
- localStorage not saving
- API not returning token

**Solution**:
1. Check browser console for errors
2. Check localStorage in DevTools
3. Verify API response has token
4. Check redirect logic in JavaScript

### Issue 4: Images Not Loading
**Symptom**: Broken image icons

**Possible Causes**:
- AVATAR.png missing
- Wrong path
- Static files not served

**Solution**:
1. Verify `wwwroot/images/AVATAR.png` exists
2. Check browser Network tab
3. Verify `app.UseStaticFiles()` in Program.cs

## 📊 Browser DevTools

### Console Tab
Check for:
- JavaScript errors
- API response logs
- localStorage operations

### Network Tab
Check for:
- API calls to `/api/auth/login` and `/api/auth/register`
- Status codes (200 = success, 400/401 = error)
- Response data

### Application Tab
Check localStorage:
- `accessToken`
- `refreshToken`
- `userEmail`
- `userId`
- `userRole`
- `surveyCompleted`

## ✅ Success Criteria

All tests pass when:
- ✅ Home page displays with tours
- ✅ Login works and redirects correctly
- ✅ Register works and redirects correctly
- ✅ Role-based redirects work
- ✅ Personality survey redirect works for travelers
- ✅ Forms validate correctly
- ✅ Error messages display
- ✅ Responsive design works
- ✅ All images load
- ✅ No console errors

## 🎉 Next Steps After Testing

If all tests pass:
1. ✅ Mark Step 1 as complete
2. 🚀 Proceed to Step 2 (if needed):
   - Create Tour Detail page
   - Create Booking flow
   - Create Guide Dashboard
   - Migrate personality survey to .cshtml

If tests fail:
1. 🐛 Document the issue
2. 🔍 Debug using DevTools
3. 🔧 Fix the issue
4. 🔄 Retest

---

**Happy Testing! 🎉**
