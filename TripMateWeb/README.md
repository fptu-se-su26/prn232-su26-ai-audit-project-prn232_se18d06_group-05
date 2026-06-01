# TripMate Web Application

Trang web quản lý tour du lịch được xây dựng bằng ASP.NET Core với Razor Pages, tương thích với cơ sở dữ liệu PostgreSQL của ứng dụng Flutter TripMate.

## 🏗️ Cấu trúc dự án

```
TripMateWeb/
├── Models/                 # Entity models
│   ├── Profile.cs         # User profiles
│   ├── TourTemplate.cs    # Tour templates
│   ├── GuideTour.cs       # Guide's tours
│   ├── Booking.cs         # Bookings
│   ├── Payment.cs         # Payments
│   ├── Review.cs          # Reviews
│   ├── Conversation.cs    # Conversations
│   ├── Message.cs         # Messages
│   ├── GuideCertificate.cs # Guide certificates
│   └── TourAvailability.cs # Tour availability
├── Data/
│   └── TripMateDbContext.cs # Entity Framework context
├── Pages/                 # Razor Pages
│   ├── Index.cshtml       # Trang chủ
│   ├── Tours/             # Quản lý tours
│   ├── Auth/              # Xác thực
│   └── Shared/            # Layout chung
└── wwwroot/               # Static files
```

## 🗄️ Cơ sở dữ liệu

Ứng dụng sử dụng cấu trúc cơ sở dữ liệu mới với các bảng chính:

### 1. **profiles** - Thông tin người dùng
- `id` (UUID, PK)
- `email` (TEXT, UNIQUE)
- `full_name`, `phone`, `avatar_url`
- `role` (traveler/guide/admin)

### 2. **tour_templates** - Mẫu tours
- `id` (UUID, PK)
- `title`, `description`, `location`
- `images` (TEXT[])

### 3. **guide_tours** - Tours của hướng dẫn viên
- `id` (UUID, PK)
- `tour_template_id` (FK)
- `guide_id` (FK)
- `price`, `duration_hours`, `max_participants`
- `rating`, `total_reviews`, `status`

### 4. **bookings** - Đặt chỗ
- `id` (UUID, PK)
- `guide_tour_id` (FK)
- `traveler_id` (FK)
- `tour_date`, `guests`, `total_price`
- `status` (pending/confirmed/completed/cancelled)

### 5. **payments** - Thanh toán
- `id` (UUID, PK)
- `booking_id` (FK, UNIQUE)
- `amount`, `payment_method`, `status`

### 6. **reviews** - Đánh giá
- `id` (UUID, PK)
- `guide_tour_id` (FK)
- `user_id` (FK)
- `booking_id` (FK, UNIQUE)
- `rating` (1-5), `comment`

### 7. **conversations** & **messages** - Tin nhắn
- Hệ thống chat giữa du khách và hướng dẫn viên

### 8. **guide_certificates** - Chứng chỉ hướng dẫn viên
- Quản lý chứng chỉ và xác minh

### 9. **tour_availability** - Lịch trống
- Quản lý số chỗ còn lại theo ngày

## 🚀 Cài đặt và chạy

### 1. Cài đặt dependencies
```bash
cd TripMateWeb
dotnet restore
```

### 2. Cấu hình database
Cập nhật connection string trong `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=tripmate;Username=postgres;Password=your_password"
  }
}
```

### 3. Chạy ứng dụng
```bash
dotnet run
```

Truy cập: `https://localhost:5001`

## 🎯 Tính năng chính

### 🏠 **Trang chủ**
- Hiển thị thống kê tổng quan
- Tours nổi bật với rating cao
- Tìm kiếm nhanh

### 🗺️ **Quản lý Tours**
- Danh sách tours với filter/search
- Chi tiết tour với hình ảnh
- Tạo/sửa/xóa tour (Guide only)
- Hệ thống đánh giá và review

### 👤 **Hệ thống người dùng**
- Đăng ký/đăng nhập
- 3 vai trò: Traveler, Guide, Admin
- Profile management

### 📅 **Đặt chỗ**
- Đặt tour với chọn ngày
- Quản lý booking theo status
- Hệ thống thanh toán

### 💬 **Tin nhắn**
- Chat realtime giữa du khách và guide
- Tự động tạo conversation khi booking confirmed

### 🏆 **Dashboard theo vai trò**
- **Traveler**: Quản lý bookings, wishlist
- **Guide**: Quản lý tours, earnings, bookings
- **Admin**: Quản lý users, analytics

## 🔧 Công nghệ sử dụng

- **Backend**: ASP.NET Core 8.0
- **Database**: PostgreSQL với Entity Framework Core
- **Frontend**: Razor Pages + Bootstrap 5
- **Icons**: Bootstrap Icons
- **Authentication**: Session-based (có thể nâng cấp JWT)

## 🔄 Tích hợp với Flutter App

Cấu trúc database được thiết kế để tương thích hoàn toàn với ứng dụng Flutter:

1. **Shared Database**: Cùng sử dụng PostgreSQL database
2. **Consistent Models**: Entity models tương ứng với Flutter entities
3. **API Ready**: Có thể mở rộng thành API cho Flutter app
4. **Role-based Access**: Hỗ trợ cùng hệ thống phân quyền

## 📱 Responsive Design

- Mobile-first approach
- Bootstrap 5 responsive grid
- Touch-friendly interface
- Progressive Web App ready

## 🔒 Bảo mật

- Input validation
- SQL injection protection (EF Core)
- XSS protection
- Role-based authorization
- Session management

## 🚀 Triển khai

Ứng dụng có thể triển khai trên:
- **Azure App Service**
- **AWS Elastic Beanstalk**
- **Google Cloud Run**
- **Docker containers**
- **IIS/Nginx**

## 📈 Mở rộng tương lai

1. **API Layer**: Tạo Web API cho Flutter app
2. **Real-time**: SignalR cho chat và notifications
3. **Payment Gateway**: Tích hợp VNPay, MoMo
4. **Analytics**: Dashboard chi tiết cho admin
5. **Mobile PWA**: Progressive Web App
6. **Microservices**: Tách thành các service nhỏ