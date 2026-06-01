# TripMate Web UI - Hướng dẫn sử dụng

## 📋 Tổng quan

Đây là giao diện web đơn giản (Raw HTML/CSS/JavaScript) để test các chức năng authentication của TripMate API.

## 🚀 Cách chạy

### 1. Khởi động API Server

```bash
cd source/web/TripMate_Webapi
dotnet run --project TripMate_Webapi
```

Server sẽ chạy tại: `http://localhost:5122`

### 2. Truy cập Web UI

Mở trình duyệt và truy cập các trang sau:

- **Trang chủ**: http://localhost:5122/
- **Đăng nhập**: http://localhost:5122/login.html
- **Đăng ký**: http://localhost:5122/register.html
- **Dashboard**: http://localhost:5122/dashboard.html (cần đăng nhập)

## 📁 Cấu trúc file

```
TripMate_Webapi/
├── wwwroot/
│   ├── index.html          # Trang chủ
│   ├── login.html          # Trang đăng nhập
│   ├── register.html       # Trang đăng ký
│   └── dashboard.html      # Trang dashboard (sau khi đăng nhập)
├── Controllers/
│   └── AuthController.cs   # API endpoints cho authentication
└── Services/
    └── SupabaseAuthService.cs  # Service xử lý authentication
```

## 🎨 Tính năng

### Trang chủ (index.html)
- Giới thiệu về TripMate
- Nút điều hướng đến đăng nhập/đăng ký
- Hiển thị các tính năng chính
- Kiểm tra trạng thái API

### Trang đăng ký (register.html)
- Form đăng ký với các trường:
  - Họ và tên
  - Email
  - Số điện thoại
  - Mật khẩu
  - Xác nhận mật khẩu
  - Vai trò (Du khách/Hướng dẫn viên)
- Validation form
- Hiển thị/ẩn mật khẩu
- Thông báo lỗi/thành công

### Trang đăng nhập (login.html)
- Form đăng nhập với email và mật khẩu
- Hiển thị/ẩn mật khẩu
- Lưu token vào localStorage
- Chuyển hướng đến dashboard sau khi đăng nhập thành công

### Trang Dashboard (dashboard.html)
- Hiển thị thông tin user
- Thống kê cơ bản (tours, bookings, ratings)
- Các hành động nhanh
- Nút đăng xuất

## 🔐 Authentication Flow

1. **Đăng ký**:
   - User điền form đăng ký
   - POST `/api/auth/register`
   - Chuyển đến trang đăng nhập

2. **Đăng nhập**:
   - User điền email/password
   - POST `/api/auth/login`
   - Nhận access token và refresh token
   - Lưu vào localStorage
   - Chuyển đến dashboard

3. **Truy cập trang bảo vệ**:
   - Kiểm tra token trong localStorage
   - Gửi token trong header: `Authorization: Bearer {token}`
   - Nếu không có token → chuyển đến login

4. **Đăng xuất**:
   - Xóa token khỏi localStorage
   - Chuyển về trang đăng nhập

## 🛠️ API Endpoints được sử dụng

### Authentication
- `POST /api/auth/register` - Đăng ký tài khoản mới
- `POST /api/auth/login` - Đăng nhập
- `POST /api/auth/refresh` - Làm mới token

### Tours
- `GET /api/tours` - Lấy danh sách tour

### Health Check
- `GET /api/health` - Kiểm tra trạng thái API

## 💾 LocalStorage Data

Sau khi đăng nhập thành công, các thông tin sau được lưu:

```javascript
localStorage.setItem('accessToken', data.accessToken);
localStorage.setItem('refreshToken', data.refreshToken);
localStorage.setItem('userId', data.userId);
localStorage.setItem('userEmail', data.email);
localStorage.setItem('userRole', data.role);
```

## 🎯 Test Cases

### Test đăng ký
1. Truy cập http://localhost:5122/register.html
2. Điền thông tin:
   - Họ tên: Nguyễn Văn A
   - Email: test@example.com
   - SĐT: 0123456789
   - Mật khẩu: 123456
   - Vai trò: Du khách
3. Click "Đăng ký"
4. Kiểm tra thông báo thành công
5. Tự động chuyển đến trang đăng nhập

### Test đăng nhập
1. Truy cập http://localhost:5122/login.html
2. Nhập email và mật khẩu vừa đăng ký
3. Click "Đăng nhập"
4. Kiểm tra thông báo thành công
5. Tự động chuyển đến dashboard

### Test dashboard
1. Sau khi đăng nhập, kiểm tra:
   - Hiển thị đúng email và vai trò
   - Thống kê được load
   - Các nút action hoạt động
2. Click "Đăng xuất"
3. Kiểm tra chuyển về trang đăng nhập

## 🐛 Troubleshooting

### Lỗi CORS
Nếu gặp lỗi CORS, kiểm tra:
- CORS đã được enable trong Program.cs
- Policy "AllowAll" đã được apply

### Lỗi 401 Unauthorized
- Kiểm tra token có được lưu trong localStorage không
- Kiểm tra token có hết hạn không
- Thử đăng nhập lại

### Lỗi kết nối API
- Kiểm tra API server đang chạy
- Kiểm tra URL trong JavaScript (API_BASE_URL)
- Mở Developer Console để xem chi tiết lỗi

## 📝 Notes

- Đây là UI đơn giản để test, không dùng cho production
- Không có validation phức tạp
- Không có xử lý refresh token tự động
- Không có error handling chi tiết
- Phù hợp cho development và testing

## 🔄 Cải tiến trong tương lai

- [ ] Thêm trang quản lý tours
- [ ] Thêm trang quản lý bookings
- [ ] Thêm trang profile
- [ ] Thêm chức năng upload avatar
- [ ] Thêm real-time notifications
- [ ] Thêm dark mode
- [ ] Responsive design tốt hơn
- [ ] Form validation chi tiết hơn
- [ ] Auto refresh token

## 📞 Liên hệ

Nếu có vấn đề, vui lòng tạo issue trên GitHub repository.
