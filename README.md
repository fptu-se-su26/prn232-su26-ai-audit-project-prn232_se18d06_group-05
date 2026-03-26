# TripMate - Travel Booking Platform

TripMate là nền tảng đặt dịch vụ du lịch đa nền tảng (Web & Mobile), kết nối khách du lịch với hướng dẫn viên địa phương một cách nhanh chóng và tiện lợi.

## 🎯 Tính năng chính

- ✅ **Xác thực người dùng**: Đăng ký, đăng nhập, quản lý phiên
- 🧭 **Quản lý Tour**: Xem danh sách, tìm kiếm, lọc tour
- 📅 **Hệ thống đặt tour**: Đặt lịch, xác nhận, lịch sử booking
- 👤 **Hồ sơ người dùng**: Quản lý thông tin cá nhân
- ⚡ **Cập nhật realtime**: Trạng thái booking, thông báo

## 🏗️ Kiến trúc

### Clean Architecture với 3 layers:
- **Presentation Layer**: UI, Widgets, State Management (Riverpod)
- **Domain Layer**: Business Logic, Entities, Use Cases
- **Data Layer**: Repositories, Data Sources, DTOs

### Tech Stack:
- **Frontend**: Flutter (Android, iOS, Web)
- **Backend**: Supabase (Auth + Database + Realtime)
- **State Management**: Riverpod
- **Navigation**: go_router
- **HTTP Client**: Dio + Supabase Client

## 📁 Cấu trúc thư mục

```
lib/
├── core/                    # Core utilities
│   ├── config/             # App configuration
│   │   ├── app_config.dart
│   │   └── supabase_config.dart
│   ├── constants/          # App constants
│   ├── errors/             # Error handling
│   │   ├── exceptions.dart
│   │   └── failures.dart
│   ├── theme/              # App theme
│   └── utils/              # Utilities
│
├── features/               # Feature modules
│   ├── auth/              # Authentication
│   │   ├── data/
│   │   ├── domain/
│   │   └── presentation/
│   ├── tour/              # Tour management
│   ├── booking/           # Booking system
│   └── profile/           # User profile
│
├── shared/                # Shared components
│   └── widgets/           # Reusable widgets
│
└── main.dart              # App entry point
```

## 🚀 Cài đặt và chạy

### Yêu cầu:
- Flutter SDK >= 3.11.3
- Dart SDK >= 3.11.3
- Supabase account

### Bước 1: Clone repository
```bash
git clone <repository-url>
cd flutter_tripmate_application
```

### Bước 2: Cài đặt dependencies
```bash
flutter pub get
```

### Bước 3: Cấu hình Supabase
File `.env` đã được tạo với thông tin Supabase:
```env
SUPABASE_URL=https://nvbvvowyjzylllswhynv.supabase.co
SUPABASE_ANON_KEY=sb_publishable_ZbSsVM4M0xZJa4PyobDMkw_cazDhnr2
```

### Bước 4: Chạy ứng dụng

**Android/iOS:**
```bash
flutter run
```

**Web:**
```bash
flutter run -d chrome
```

**Chọn device cụ thể:**
```bash
flutter devices                    # Xem danh sách devices
flutter run -d <device-id>         # Chạy trên device cụ thể
```

## 🔧 Development

### Generate code (khi cần):
```bash
flutter pub run build_runner build --delete-conflicting-outputs
```

### Chạy tests:
```bash
flutter test
```

### Build production:
```bash
# Android
flutter build apk --release

# iOS
flutter build ios --release

# Web
flutter build web --release
```

## 📝 Lưu ý quan trọng

1. **File .env**: Không commit file `.env` lên git (đã thêm vào `.gitignore`)
2. **Đường dẫn có khoảng trắng**: Nếu gặp lỗi với đường dẫn có khoảng trắng, hãy di chuyển project đến thư mục không có khoảng trắng
3. **Supabase**: Đảm bảo Supabase project đã được setup đúng với các bảng cần thiết

## 🎨 Màu sắc chủ đạo

- Primary: `#2196F3` (Blue)
- Secondary: `#03DAC6` (Teal)
- Background: `#F5F5F5` (Light Gray)
- Error: `#B00020` (Red)

## 📱 Platforms hỗ trợ

- ✅ Android
- ✅ iOS
- ✅ Web
- ⏳ Desktop (Future)

## 🔐 Bảo mật

- JWT-based authentication với Supabase
- PKCE flow cho OAuth
- Secure storage cho tokens
- HTTPS cho tất cả API calls

## 📄 License

Private project - All rights reserved

## 👥 Team

TripMate Development Team

---

**Version**: 1.0.0  
**Last Updated**: 2026-03-26
