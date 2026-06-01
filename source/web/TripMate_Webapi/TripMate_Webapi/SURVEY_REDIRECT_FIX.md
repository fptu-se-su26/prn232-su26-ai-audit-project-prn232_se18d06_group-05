# Survey Redirect Bug Fix

## Problem
Sau khi đăng nhập với role "traveler", người dùng không được redirect đến trang personality survey. Thậm chí khi ép URL trực tiếp đến `/personality-survey.html`, trang vẫn redirect về home.

## Root Cause
Có 2 vấn đề chính:

### 1. Race Condition trong localStorage
Khi login.html redirect đến personality-survey.html, có một khoảng thời gian rất ngắn mà localStorage chưa được cập nhật đầy đủ. Trang personality-survey.html kiểm tra localStorage ngay lập tức trong `window.addEventListener('load')`, dẫn đến việc đọc giá trị cũ hoặc null.

### 2. Logic kiểm tra quá nghiêm ngặt
Code kiểm tra `if (role !== 'traveler')` sẽ redirect ngay cả khi role là null hoặc undefined do localStorage chưa sẵn sàng.

## Solution

### 1. Thêm delay 100ms trong personality-survey.html
```javascript
setTimeout(() => {
    const role = localStorage.getItem('userRole');
    const surveyCompleted = localStorage.getItem('surveyCompleted');
    const token = localStorage.getItem('accessToken');
    
    // Check logic here...
}, 100); // 100ms delay để đảm bảo localStorage đã sẵn sàng
```

### 2. Cải thiện logging để debug
```javascript
console.log('=== SURVEY PAGE CHECK ===');
console.log('Role:', role);
console.log('Survey completed:', surveyCompleted);
console.log('Has token:', !!token);
console.log('========================');
```

### 3. Kiểm tra token trước
Nếu không có token, redirect về login thay vì home:
```javascript
if (!token) {
    console.log('No token found, redirecting to login...');
    window.location.href = '/login.html';
    return;
}
```

### 4. Kiểm tra role rõ ràng hơn
```javascript
if (role !== 'traveler') {
    console.log('Not a traveler (role=' + role + '), redirecting to home...');
    window.location.href = '/index.html';
    return;
}
```

## Testing Flow

### Scenario 1: Traveler đăng nhập lần đầu (chưa làm survey)
1. Đăng nhập với role="traveler"
2. localStorage được set: `userRole=traveler`, `surveyCompleted` không tồn tại
3. Redirect đến `/personality-survey.html`
4. Sau 100ms, kiểm tra: role="traveler", surveyCompleted=null
5. ✅ Hiển thị form survey

### Scenario 2: Traveler đã làm survey
1. Đăng nhập với role="traveler"
2. localStorage: `userRole=traveler`, `surveyCompleted=true`
3. Redirect đến `/personality-survey.html`
4. Sau 100ms, kiểm tra: role="traveler", surveyCompleted="true"
5. ✅ Redirect về `/index.html`

### Scenario 3: Guide hoặc Admin đăng nhập
1. Đăng nhập với role="guide" hoặc "admin"
2. localStorage: `userRole=guide`
3. Redirect đến `/index.html` (không qua survey)
4. ✅ Hiển thị trang home

### Scenario 4: Ép URL trực tiếp
1. User đã đăng nhập với role="traveler", chưa làm survey
2. Truy cập trực tiếp `/personality-survey.html`
3. Sau 100ms, kiểm tra: role="traveler", surveyCompleted=null
4. ✅ Hiển thị form survey

## Debug với debug.html

Sử dụng `/debug.html` để test các scenario:

### Test Traveler chưa làm survey:
```javascript
localStorage.setItem('userRole', 'traveler');
localStorage.removeItem('surveyCompleted');
// Sau đó vào /personality-survey.html
```

### Test Traveler đã làm survey:
```javascript
localStorage.setItem('userRole', 'traveler');
localStorage.setItem('surveyCompleted', 'true');
// Sau đó vào /personality-survey.html
```

### Test Guide:
```javascript
localStorage.setItem('userRole', 'guide');
// Sau đó vào /personality-survey.html
```

## Files Changed
- `wwwroot/personality-survey.html` - Fixed redirect logic with delay and better checks
- `wwwroot/login.html` - Already correct, no changes needed
- `wwwroot/debug.html` - Already has test scenarios

## Logo Update
Đã tạo logo mới tại `/images/logo.svg` và cập nhật tất cả các trang:
- ✅ index.html
- ✅ login.html
- ✅ register.html
- ✅ personality-survey.html
- ✅ dashboard.html

Logo là SVG với:
- Màu cam (#FF6B35) làm nền
- Chữ "tm" màu trắng
- Mũi tên và target icon
- Responsive và sắc nét ở mọi kích thước
