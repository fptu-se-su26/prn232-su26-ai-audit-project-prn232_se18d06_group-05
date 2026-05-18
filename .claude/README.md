# 🏖️ TripMate - Travel Booking Platform

> Comprehensive documentation for Claude AI assistant

## 📋 Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture & Technology Stack](#architecture--technology-stack)
3. [Project Structure](#project-structure)
4. [Features & Requirements](#features--requirements)
5. [Development Guidelines](#development-guidelines)
6. [API Documentation](#api-documentation)
7. [Database Schema](#database-schema)
8. [Deployment & DevOps](#deployment--devops)

---

## 🎯 Project Overview

**TripMate** là một nền tảng đặt tour du lịch kết nối travelers (khách du lịch) với local guides (hướng dẫn viên địa phương). Ứng dụng được xây dựng với kiến trúc modern, scalable và user-friendly.

### 🎪 Core Concept
- **Travelers**: Tìm kiếm và đặt tour từ local guides
- **Guides**: Tạo tour, quản lý bookings, chat với travelers
- **Admins**: Quản lý toàn bộ hệ thống

### 🚀 Key Features
- **Multi-role Authentication**: JWT-based với role management
- **Tour Management**: CRUD operations với image support
- **Real-time Booking**: Instant booking với payment integration
- **Live Chat**: Real-time messaging giữa travelers và guides
- **Dashboard Analytics**: Comprehensive stats cho mỗi role
- **Mobile-first Design**: Responsive UI/UX

### 📊 Current Status
- **Overall Progress**: 85% Complete
- **Production Ready**: Yes (with minor enhancements needed)
- **Active Development**: Ongoing improvements và new features

---

## 🏗️ Architecture & Technology Stack

### 📱 Frontend (Mobile App)
```yaml
Framework: Flutter 3.11.3+
Language: Dart
Architecture: Clean Architecture + Feature-based
State Management: Riverpod 2.6.1
Navigation: Go Router 14.6.2
UI Framework: Material Design 3
```

### 🌐 Backend (API Server)
```yaml
Framework: ASP.NET Core 9.0
Language: C#
Architecture: Layered Architecture
Authentication: JWT Bearer Tokens
API Documentation: Swagger/OpenAPI
```

### 🗄️ Database & Storage
```yaml
Primary Database: Supabase (PostgreSQL)
Real-time: Supabase Realtime
Authentication: Supabase Auth
File Storage: Supabase Storage (planned)
Caching: SharedPreferences (local)
```

### 🔧 Development Tools
```yaml
IDE: VS Code, Android Studio
Version Control: Git
CI/CD: GitHub Actions
Package Manager: pub (Flutter), NuGet (.NET)
Code Generation: build_runner, freezed
```

### 📦 Key Dependencies

#### Flutter Dependencies
```yaml
Core:
  - flutter_riverpod: ^2.6.1 (State Management)
  - go_router: ^14.6.2 (Navigation)
  - dio: ^5.7.0 (HTTP Client)
  - supabase_flutter: ^2.9.1 (Backend Integration)

UI/UX:
  - image_picker: ^1.0.7 (Image Selection)
  - flutter_local_notifications: ^18.0.1 (Notifications)
  - geolocator: ^13.0.2 (Location Services)

Utilities:
  - dartz: ^0.10.1 (Functional Programming)
  - freezed: ^2.5.7 (Code Generation)
  - shared_preferences: ^2.3.3 (Local Storage)
```

#### .NET Dependencies
```yaml
Core:
  - Microsoft.AspNetCore.Authentication.JwtBearer: 9.0.0
  - supabase-csharp: 0.16.2
  - Swashbuckle.AspNetCore: 6.9.0 (Swagger)
```

---

## 📁 Project Structure

### 🎯 Clean Architecture Layers

```
lib/
├── 🎨 Presentation Layer
│   ├── screens/          # UI Screens
│   ├── widgets/          # Reusable UI Components
│   └── providers/        # State Management (Riverpod)
├── 🏢 Domain Layer
│   ├── entities/         # Business Models
│   ├── repositories/     # Abstract Interfaces
│   └── usecases/         # Business Logic
├── 📊 Data Layer
│   ├── models/           # Data Transfer Objects
│   ├── datasources/      # API & Local Data Sources
│   └── repositories/     # Repository Implementations
└── ⚙️ Core Layer
    ├── config/           # App Configuration
    ├── constants/        # App Constants
    ├── errors/           # Error Handling
    ├── services/         # Shared Services
    └── utils/            # Utility Functions
```

### 🗂️ Feature-based Organization

```
lib/features/
├── 🔐 auth/              # Authentication & Authorization
│   ├── data/
│   ├── domain/
│   └── presentation/
├── 🏠 dashboard/         # Role-based Dashboards
├── 🗺️ tour/             # Tour Management
├── 📅 booking/          # Booking System
├── 💬 chat/             # Real-time Messaging
└── 👤 profile/          # User Profile Management
```

### 🌐 Backend Structure

```
web/TripMate_Webapi/
├── 🎮 Controllers/       # API Endpoints
├── 🏢 Services/          # Business Logic
├── 📊 Models/            # Data Models
├── 🔧 Infrastructure/    # Configuration & Setup
├── 🗄️ Migrations/        # Database Migrations
└── 📋 Properties/        # App Settings
```

---

## ✨ Features & Requirements

### 🔐 Authentication System
```yaml
Status: ✅ Complete (100%)
Features:
  - Email/Password Registration
  - JWT-based Login
  - Role-based Access Control
  - Session Persistence
  - Secure Token Management
  - Auto-refresh Tokens
```

### 🏠 Dashboard System
```yaml
Status: ✅ Complete (100%)
Features:
  - Traveler Dashboard: Stats, Recommendations, Recent Bookings
  - Guide Dashboard: Earnings, Tour Management, Booking Analytics
  - Admin Dashboard: System Overview, User Management
  - Role-based Navigation
  - Beautiful Card-based UI
```

### 🗺️ Tour Management
```yaml
Status: ✅ Complete (95%)
Features:
  - Tour Catalog với Pagination
  - Advanced Search & Filters
  - Tour Detail View
  - Tour Creation/Editing (Guides only)
  - Image Upload Support
  - Price Management
  - Availability Calendar
```

### 📅 Booking System
```yaml
Status: 🔄 Near Complete (90%)
Features:
  - ✅ Booking Form với Date/Guest Selection
  - ✅ Price Calculation
  - ✅ Mock Payment Integration
  - ✅ Booking Confirmation
  - ✅ Booking History
  - ❌ Real Payment Gateway (Stripe/PayPal)
```

### 💬 Chat System
```yaml
Status: 🔄 Near Complete (85%)
Features:
  - ✅ Real-time Messaging
  - ✅ Conversation Management
  - ✅ Message History
  - ✅ Auto-scroll & Message Bubbles
  - ❌ Read Receipts
  - ❌ File/Image Sharing
```

### 👤 Profile Management
```yaml
Status: 🔄 In Progress (70%)
Features:
  - ✅ Profile Display
  - ✅ Basic Profile Editing
  - ✅ Role Management
  - ❌ Avatar Upload
  - ❌ Advanced Preferences
```

---

## 🛠️ Development Guidelines

### 📋 Code Standards

#### Flutter/Dart
```dart
// ✅ Good: Feature-based imports
import '../../domain/entities/user_entity.dart';
import '../providers/auth_providers.dart';

// ✅ Good: Descriptive naming
class AuthStateNotifier extends StateNotifier<AuthState> {
  Future<bool> signIn({required String email, required String password}) async {
    // Implementation
  }
}

// ✅ Good: Error handling
result.fold(
  (failure) => state = state.copyWith(error: failure.message),
  (user) => state = AuthState(user: user, isAuthenticated: true),
);
```

#### C# (.NET)
```csharp
// ✅ Good: Controller structure
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Implementation
    }
}

// ✅ Good: Service pattern
public class AuthService : IAuthService
{
    public async Task<AuthResult> AuthenticateAsync(string email, string password)
    {
        // Implementation
    }
}
```

### 🎯 Architecture Principles

1. **Separation of Concerns**: Mỗi layer có responsibility riêng biệt
2. **Dependency Inversion**: High-level modules không depend vào low-level modules
3. **Single Responsibility**: Mỗi class có một lý do duy nhất để thay đổi
4. **Open/Closed Principle**: Open for extension, closed for modification

### 🧪 Testing Strategy

```yaml
Unit Tests:
  - Domain Layer: Business Logic
  - Data Layer: Repository Implementations
  - Presentation Layer: State Management

Integration Tests:
  - API Endpoints
  - Database Operations
  - Authentication Flow

Widget Tests:
  - UI Components
  - Screen Navigation
  - User Interactions
```

---

## 🔌 API Documentation

### 🔐 Authentication Endpoints

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123",
  "fullName": "John Doe"
}

Response: 200 OK
{
  "accessToken": "jwt_token_here",
  "refreshToken": "refresh_token_here",
  "expiresAt": 1640995200,
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "role": "traveler"
  }
}
```

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123"
}
```

```http
GET /api/auth/me
Authorization: Bearer {jwt_token}

Response: 200 OK
{
  "id": "uuid",
  "email": "user@example.com",
  "role": "traveler"
}
```

### 🗺️ Tour Endpoints

```http
GET /api/tours?page=1&limit=10&search=hanoi
Authorization: Bearer {jwt_token}

Response: 200 OK
{
  "tours": [...],
  "totalCount": 50,
  "currentPage": 1,
  "totalPages": 5
}
```

```http
POST /api/tours
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "title": "Hanoi Street Food Tour",
  "description": "Explore the best street food in Hanoi",
  "location": "Hanoi, Vietnam",
  "price": 350000,
  "durationHours": 4,
  "maxParticipants": 8,
  "images": ["url1", "url2"]
}
```

### 📅 Booking Endpoints

```http
POST /api/bookings
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "tourId": "uuid",
  "bookingDate": "2024-01-15T09:00:00Z",
  "numberOfGuests": 2,
  "totalAmount": 700000,
  "specialRequests": "Vegetarian meals please"
}
```

---

## 🗄️ Database Schema

### 👥 Users & Authentication
```sql
-- Managed by Supabase Auth
auth.users (
  id uuid PRIMARY KEY,
  email text UNIQUE,
  encrypted_password text,
  created_at timestamptz,
  -- Supabase managed fields
)

-- Custom user profiles
public.profiles (
  id uuid PRIMARY KEY REFERENCES auth.users(id),
  full_name text,
  role text CHECK (role IN ('traveler', 'guide', 'admin')),
  phone text,
  avatar_url text,
  created_at timestamptz DEFAULT now()
)
```

### 🗺️ Tours
```sql
public.tours (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  guide_id uuid REFERENCES auth.users(id),
  title text NOT NULL,
  description text,
  location text NOT NULL,
  price decimal(10,2) NOT NULL,
  duration_hours integer NOT NULL,
  max_participants integer NOT NULL,
  images text[], -- Array of image URLs
  is_active boolean DEFAULT true,
  created_at timestamptz DEFAULT now(),
  updated_at timestamptz DEFAULT now()
)
```

### 📅 Bookings
```sql
public.bookings (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  tour_id uuid REFERENCES public.tours(id),
  traveler_id uuid REFERENCES auth.users(id),
  booking_date timestamptz NOT NULL,
  number_of_guests integer NOT NULL,
  total_amount decimal(10,2) NOT NULL,
  status text DEFAULT 'pending',
  special_requests text,
  created_at timestamptz DEFAULT now()
)
```

### 💬 Chat System
```sql
public.conversations (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  traveler_id uuid REFERENCES auth.users(id),
  guide_id uuid REFERENCES auth.users(id),
  booking_id uuid REFERENCES public.bookings(id),
  created_at timestamptz DEFAULT now(),
  UNIQUE(traveler_id, guide_id, booking_id)
)

public.messages (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  conversation_id uuid REFERENCES public.conversations(id),
  sender_id uuid REFERENCES auth.users(id),
  content text NOT NULL,
  is_read boolean DEFAULT false,
  created_at timestamptz DEFAULT now()
)
```

### 🔒 Row Level Security (RLS)
```sql
-- Tours: Anyone can read, only guides can create/update their own
CREATE POLICY "Anyone can view active tours" ON public.tours
  FOR SELECT USING (is_active = true);

CREATE POLICY "Guides can manage own tours" ON public.tours
  FOR ALL USING (auth.uid() = guide_id);

-- Bookings: Users can only see their own bookings
CREATE POLICY "Users can view own bookings" ON public.bookings
  FOR SELECT USING (
    auth.uid() = traveler_id OR 
    auth.uid() = (SELECT guide_id FROM tours WHERE id = tour_id)
  );
```

---

## 🚀 Deployment & DevOps

### 🏗️ Development Environment

```bash
# Flutter Setup
flutter doctor
flutter pub get
flutter run

# .NET API Setup
cd web/TripMate_Webapi
dotnet restore
dotnet run

# Environment Variables (.env)
SUPABASE_URL=your_supabase_url
SUPABASE_ANON_KEY=your_anon_key
API_BASE_URL=http://localhost:5122/api
```

### 📦 Build & Release

```yaml
# GitHub Actions Workflow
name: Build and Deploy
on:
  push:
    branches: [main]

jobs:
  build-flutter:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: subosito/flutter-action@v2
      - run: flutter pub get
      - run: flutter test
      - run: flutter build apk --release

  build-api:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
      - run: dotnet restore
      - run: dotnet test
      - run: dotnet publish -c Release
```

### 🌐 Production Deployment

```yaml
Frontend:
  - Platform: Android APK, iOS IPA
  - Distribution: Google Play Store, Apple App Store
  - Web: Firebase Hosting / Vercel

Backend:
  - Platform: Azure App Service / AWS ECS
  - Database: Supabase (Production tier)
  - CDN: Cloudflare
  - Monitoring: Application Insights
```

---

## 📞 Support & Contact

### 🛠️ Development Team
- **Lead Developer**: [Your Name]
- **Backend Developer**: [Team Member]
- **UI/UX Designer**: [Team Member]

### 📚 Resources
- **Documentation**: `/docs` folder
- **API Docs**: Swagger UI at `/swagger`
- **Database**: Supabase Dashboard
- **Monitoring**: Application logs

### 🐛 Issue Tracking
- **Bug Reports**: GitHub Issues
- **Feature Requests**: GitHub Discussions
- **Security Issues**: Private email

---

**Last Updated**: December 2024  
**Version**: 1.0.0  
**Status**: Production Ready (85% Complete)