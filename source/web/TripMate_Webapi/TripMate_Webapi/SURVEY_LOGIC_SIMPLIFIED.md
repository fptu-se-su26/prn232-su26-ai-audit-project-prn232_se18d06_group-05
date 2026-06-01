# Personality Survey Logic - Simplified

## Logic Đơn Giản (Không Còn "Lần Đầu")

### Khi đăng nhập (login.html):
```javascript
if (role === 'traveler' && surveyCompleted !== 'true') {
    // Redirect đến survey
    window.location.href = '/personality-survey.html';
} else {
    // Redirect đến home
    window.location.href = '/index.html';
}
```

### Khi vào trang survey (personality-survey.html):
```javascript
if (!token) {
    // Chưa đăng nhập → redirect về login
    window.location.href = '/login.html';
} else if (role === 'traveler' && surveyCompleted !== 'true') {
    // Traveler chưa làm survey → ở lại trang này
    // Show form
} else {
    // Tất cả các trường hợp khác → redirect về home
    window.location.href = '/index.html';
}
```

## Các Trường Hợp

### ✅ Case 1: Traveler chưa làm survey
- **Khi đăng nhập**: Redirect → `/personality-survey.html`
- **Khi vào survey trực tiếp**: Hiển thị form
- **Sau khi hoàn thành survey**: Set `surveyCompleted='true'` → Redirect → `/index.html`

### ✅ Case 2: Traveler đã làm survey
- **Khi đăng nhập**: Redirect → `/index.html`
- **Khi vào survey trực tiếp**: Redirect → `/index.html`

### ✅ Case 3: Guide hoặc Admin
- **Khi đăng nhập**: Redirect → `/index.html`
- **Khi vào survey trực tiếp**: Redirect → `/index.html`

### ✅ Case 4: Chưa đăng nhập
- **Khi vào survey trực tiếp**: Redirect → `/login.html`

## Không Còn Khái Niệm "Lần Đầu"

Logic mới **KHÔNG** quan tâm đến việc đây có phải lần đăng nhập đầu tiên hay không. Chỉ cần:
- Là traveler
- Chưa có `surveyCompleted='true'` trong localStorage

→ Thì sẽ redirect đến survey

## Test Scenarios

### Scenario 1: Traveler mới đăng ký
1. Đăng ký tài khoản với role="traveler"
2. Đăng nhập
3. ✅ Redirect đến `/personality-survey.html`
4. Làm survey
5. ✅ Redirect đến `/index.html`

### Scenario 2: Traveler đã làm survey, đăng nhập lại
1. Đăng nhập với tài khoản đã làm survey
2. localStorage có `surveyCompleted='true'`
3. ✅ Redirect đến `/index.html` (bỏ qua survey)

### Scenario 3: Traveler clear localStorage
1. Traveler đã làm survey trước đó
2. Clear localStorage (hoặc đổi trình duyệt)
3. Đăng nhập lại
4. ✅ Redirect đến `/personality-survey.html` (phải làm lại survey)

### Scenario 4: Traveler chưa làm survey, vào home trước
1. Traveler đăng nhập → redirect đến survey
2. Traveler bỏ qua, gõ URL `/index.html` trực tiếp
3. ✅ Vào được home (không bắt buộc phải làm survey)
4. Nhưng lần đăng nhập sau vẫn sẽ redirect đến survey

## Lưu Ý Quan Trọng

### Survey KHÔNG bắt buộc
- Nếu traveler muốn bỏ qua survey, có thể gõ URL `/index.html` trực tiếp
- Nhưng mỗi lần đăng nhập sẽ vẫn redirect đến survey cho đến khi hoàn thành

### Survey chỉ cần làm 1 lần
- Sau khi hoàn thành, `surveyCompleted='true'` được lưu vào localStorage
- Các lần đăng nhập sau sẽ không phải làm lại

### Clear localStorage = Phải làm lại
- Nếu user clear localStorage hoặc đổi trình duyệt
- Sẽ phải làm lại survey vì không còn flag `surveyCompleted`

## So Sánh Logic Cũ vs Mới

### ❌ Logic Cũ (Phức Tạp):
```javascript
// Kiểm tra nhiều điều kiện
if (role !== 'traveler') {
    redirect to home
} else if (surveyCompleted === 'true') {
    redirect to home
} else {
    stay on survey
}
```

### ✅ Logic Mới (Đơn Giản):
```javascript
// Chỉ 1 điều kiện duy nhất
if (role === 'traveler' && surveyCompleted !== 'true') {
    stay on survey
} else {
    redirect to home
}
```

## Files Changed
- ✅ `wwwroot/login.html` - Simplified redirect logic
- ✅ `wwwroot/personality-survey.html` - Simplified check logic
- ✅ `SURVEY_LOGIC_SIMPLIFIED.md` - New documentation
