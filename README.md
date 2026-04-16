# 🌟 TripMate - Travel Booking Platform

> Nền tảng đặt tour du lịch kết nối khách hàng với hướng dẫn viên địa phương

[![Flutter](https://img.shields.io/badge/Flutter-3.11.3+-blue.svg)](https://flutter.dev/)
[![Supabase](https://img.shields.io/badge/Supabase-Backend-green.svg)](https://supabase.com/)
[![ASP.NET](https://img.shields.io/badge/ASP.NET_Core-API-purple.svg)](https://dotnet.microsoft.com/)

## 🎯 Tổng quan

TripMate là nền tảng đặt tour du lịch đa nền tảng (Mobile & Web) với kiến trúc Clean Architecture, sử dụng Flutter cho frontend và Supabase + ASP.NET Core cho backend.

### ✨ Tính năng chính

- 🔐 **Xác thực đa vai trò**: Traveler, Guide, Admin
- 🗺️ **Quản lý Tour**: Tạo, chỉnh sửa, tìm kiếm tour
- 📅 **Hệ thống Booking**: Đặt tour, thanh toán, xác nhận
- 💬 **Chat realtime**: Tin nhắn giữa khách và hướng dẫn viên
- 📊 **Dashboard**: Thống kê cho từng vai trò
- 🔔 **Thông báo**: Cập nhật trạng thái booking

## 🏗️ Kiến trúc

### Frontend (Flutter)
```
lib/
├── core/                    # Core utilities & config
├── features/               # Feature modules
│   ├── auth/              # Authentication
│   ├── dashboard/         # Role-based dashboards  
│   ├── tour/              # Tour management
│   ├── booking/           # Booking system
│   └── chat/              # Real-time messaging
└── shared/                # Shared components
```

### Backend
- **Supabase**: Database, Auth, Realtime
- **ASP.NET Core**: API Gateway, Business Logic
- **PostgreSQL**: Primary database

### Tech Stack
- **Frontend**: Flutter, Riverpod, go_router
- **Backend**: Supabase, ASP.NET Core
- **Database**: PostgreSQL
- **Real-time**: Supabase Realtime
- **State Management**: Riverpod

## 🚀 Quick Start

### 1. Prerequisites
```bash
# Flutter SDK
flutter --version  # >= 3.11.3

# .NET SDK (for API)
dotnet --version   # >= 7.0
```

### 2. Clone & Setup
```bash
git clone <repository-url>
cd flutter_tripmate_application

# Install Flutter dependencies
flutter pub get

# Setup environment
cp .env.example .env
# Edit .env with your Supabase credentials
```

### 3. Database Setup
```bash
# Run in Supabase SQL Editor
# Copy content from: web/TripMate_Webapi/migrations/003_chat_tables.sql
```

### 4. Run Application
```bash
# Flutter app
flutter run

# ASP.NET API (optional)
cd web/TripMate_Webapi
dotnet run
```

## 📱 Platforms

- ✅ **Android** - Native mobile experience
- ✅ **iOS** - Native mobile experience  
- ✅ **Web** - Responsive web application
- ⏳ **Desktop** - Future support

## 🎨 Design System

### Colors
- **Primary**: `#E91E8C` (Pink)
- **Secondary**: `#2196F3` (Blue)
- **Success**: `#4CAF50` (Green)
- **Warning**: `#FF9800` (Orange)
- **Error**: `#F44336` (Red)

### Typography
- **Font**: System default with Vietnamese support
- **Scale**: Material 3 typography scale

## 👥 User Roles

### 🧳 Traveler
- Browse and search tours
- Book tours with payment
- Chat with guides
- View booking history
- Rate and review tours

### 🗺️ Guide
- Create and manage tours
- Accept/decline bookings
- Chat with travelers
- View earnings and analytics
- Manage availability

### 👨‍💼 Admin
- Manage all users and tours
- View platform analytics
- Handle disputes
- System configuration

## 🔧 Development

### Code Generation
```bash
flutter pub run build_runner build --delete-conflicting-outputs
```

### Testing
```bash
flutter test
```

### Build
```bash
# Android
flutter build apk --release

# iOS  
flutter build ios --release

# Web
flutter build web --release
```

## 📚 Documentation

- 📖 [Setup Guide](docs/SETUP_GUIDE.md) - Detailed setup instructions
- 🗄️ [Database Setup](docs/DATABASE_SETUP.md) - Database schema and migration
- 🔧 [API Documentation](docs/API_GUIDE.md) - ASP.NET Core API guide
- 🎨 [Design System](docs/DESIGN_SYSTEM.md) - UI/UX guidelines
- 🧪 [Testing Guide](docs/TESTING_GUIDE.md) - Testing strategies

## 🔐 Security

- JWT-based authentication
- Row Level Security (RLS) in Supabase
- HTTPS for all communications
- Input validation and sanitization
- Secure token storage

## 🌍 Internationalization

- Vietnamese (primary)
- English (secondary)
- Extensible for more languages

## 📄 License

Private project - All rights reserved

## 👨‍💻 Development Team

TripMate Development Team

---

**Version**: 2.0.0  
**Last Updated**: December 2024  
**Status**: ✅ Production Ready

## 🆘 Support

For technical support or questions:
- Check [Documentation](docs/)
- Review [Common Issues](docs/TROUBLESHOOTING.md)
- Contact development team

---

Made with ❤️ in Vietnam 🇻🇳