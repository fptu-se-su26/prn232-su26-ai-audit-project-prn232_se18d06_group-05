# Authentication Feature

## 📋 Tổng quan

Feature Authentication được xây dựng theo Clean Architecture với 3 layers:
- **Domain Layer**: Business logic, entities, use cases
- **Data Layer**: Repository implementation, data sources, models
- **Presentation Layer**: UI screens, state management

## 🏗️ Cấu trúc

```
lib/features/auth/
├── domain/
│   ├── entities/
│   │   └── user_entity.dart           # User domain model
│   ├── repositories/
│   │   └── auth_repository.dart       # Repository interface
│   └── usecases/
│       ├── sign_up_usecase.dart       # Sign up business logic
│       ├── sign_in_usecase.dart       # Sign in business logic
│       ├── sign_out_usecase.dart      # Sign out business logic
│       └── get_current_user_usecase.dart
│
├── data/
│   ├── models/
│   │   └── user_model.dart            # User data model (DTO)
│   ├── datasources/
│   │   └── auth_remote_datasource.dart # Supabase API calls
│   └── repositories/
│       └── auth_repository_impl.dart   # Repository implementation
│
└── presentation/
    ├── providers/
    │   ├── auth_providers.dart         # Dependency injection
    │   └── auth_state_provider.dart    # State management
    └── screens/
        ├── login_screen.dart           # Login UI
        ├── signup_screen.dart          # Sign up UI
        └── home_screen.dart            # Home screen (temp)
```

## ✨ Tính năng

### 1. Đăng ký (Sign Up)
- ✅ Nhập họ tên, email, mật khẩu
- ✅ Validation form
- ✅ Tạo tài khoản Supabase
- ✅ Tự động tạo profile trong database
- ✅ Tự động đăng nhập sau khi đăng ký

### 2. Đăng nhập (Sign In)
- ✅ Nhập email và mật khẩu
- ✅ Validation form
- ✅ Xác thực với Supabase
- ✅ Lưu session
- ✅ Chuyển đến home screen

### 3. Đăng xuất (Sign Out)
- ✅ Xác nhận trước khi đăng xuất
- ✅ Xóa session
- ✅ Chuyển về login screen

### 4. Session Management
- ✅ Kiểm tra auth status khi app khởi động
- ✅ Tự động restore session
- ✅ Redirect dựa trên auth state

## 🔄 Data Flow

### Sign Up Flow
```
UI (SignUpScreen)
  ↓ User input
State Provider (AuthStateNotifier)
  ↓ Call use case
Use Case (SignUpUseCase)
  ↓ Validate & call repository
Repository (AuthRepositoryImpl)
  ↓ Call data source
Data Source (AuthRemoteDataSourceImpl)
  ↓ Supabase API
Supabase Auth + Database
  ↓ Response
Data Source → Repository → Use Case → State Provider → UI
```

### Sign In Flow
```
UI (LoginScreen)
  ↓ User input
State Provider (AuthStateNotifier)
  ↓ Call use case
Use Case (SignInUseCase)
  ↓ Validate & call repository
Repository (AuthRepositoryImpl)
  ↓ Call data source
Data Source (AuthRemoteDataSourceImpl)
  ↓ Supabase API
Supabase Auth
  ↓ Response
Data Source → Repository → Use Case → State Provider → UI
```

## 🎯 Use Cases

### SignUpUseCase
```dart
Future<Either<Failure, UserEntity>> call({
  required String email,
  required String password,
  required String fullName,
})
```
- Validates email format
- Validates password length (min 8 chars)
- Validates full name not empty
- Calls repository to create account

### SignInUseCase
```dart
Future<Either<Failure, UserEntity>> call({
  required String email,
  required String password,
})
```
- Validates email and password not empty
- Calls repository to authenticate

### SignOutUseCase
```dart
Future<Either<Failure, void>> call()
```
- Calls repository to sign out
- Clears session

### GetCurrentUserUseCase
```dart
Future<Either<Failure, UserEntity?>> call()
```
- Gets current authenticated user
- Returns null if not authenticated

## 📦 Models

### UserEntity (Domain)
```dart
class UserEntity {
  final String id;
  final String email;
  final String? fullName;
  final String? phone;
  final String? avatarUrl;
  final String role;
  final DateTime createdAt;
}
```

### UserModel (Data)
```dart
class UserModel extends UserEntity {
  // Includes fromJson, toJson methods
  // Converts between Entity and JSON
}
```

## 🔐 Security

- ✅ Passwords never stored in plain text
- ✅ JWT-based authentication
- ✅ PKCE flow for OAuth
- ✅ Secure session storage
- ✅ HTTPS for all API calls
- ✅ Input validation on client side
- ✅ Row Level Security on Supabase

## 🎨 UI Components

### LoginScreen
- Email input field
- Password input field (with show/hide)
- Login button with loading state
- Sign up link
- Error messages via SnackBar

### SignUpScreen
- Full name input field
- Email input field
- Password input field (with show/hide)
- Confirm password field
- Sign up button with loading state
- Login link
- Error messages via SnackBar

### HomeScreen (Temporary)
- User avatar
- Welcome message
- User info card
- Logout button
- Coming soon message

## 🧪 Testing

### Unit Tests (TODO)
- [ ] Use case tests
- [ ] Repository tests
- [ ] Data source tests
- [ ] Model tests

### Widget Tests (TODO)
- [ ] Login screen tests
- [ ] Sign up screen tests
- [ ] Home screen tests

### Integration Tests (TODO)
- [ ] Full auth flow test
- [ ] Session persistence test

## 🚀 Usage

### 1. Sign Up
```dart
final success = await ref.read(authStateProvider.notifier).signUp(
  email: 'user@example.com',
  password: 'password123',
  fullName: 'John Doe',
);
```

### 2. Sign In
```dart
final success = await ref.read(authStateProvider.notifier).signIn(
  email: 'user@example.com',
  password: 'password123',
);
```

### 3. Sign Out
```dart
final success = await ref.read(authStateProvider.notifier).signOut();
```

### 4. Get Current User
```dart
final authState = ref.watch(authStateProvider);
final user = authState.user;
```

### 5. Check Auth Status
```dart
final authState = ref.watch(authStateProvider);
if (authState.isAuthenticated) {
  // User is logged in
}
```

## 🔧 Configuration

### Supabase Setup Required
1. Enable Email authentication in Supabase dashboard
2. Create `profiles` table (see SUPABASE_SETUP.md)
3. Setup trigger to auto-create profile on signup
4. Configure Row Level Security policies

### Environment Variables
```env
SUPABASE_URL=your_supabase_url
SUPABASE_ANON_KEY=your_anon_key
```

## 📝 Error Handling

### Validation Errors
- Empty email/password
- Invalid email format
- Password too short
- Passwords don't match

### Auth Errors
- Invalid credentials
- Email already registered
- Email not confirmed
- Network errors
- Server errors

### Error Display
- Errors shown via SnackBar
- User-friendly Vietnamese messages
- Auto-clear after display

## 🎯 Next Steps

- [ ] Forgot password functionality
- [ ] Email verification
- [ ] Social login (Google, Facebook)
- [ ] Profile update screen
- [ ] Change password
- [ ] Delete account
- [ ] Remember me option
- [ ] Biometric authentication

## 📚 Dependencies

- `flutter_riverpod` - State management
- `supabase_flutter` - Backend client
- `dartz` - Functional programming (Either type)

## 🐛 Known Issues

None currently

## 📞 Support

Nếu gặp vấn đề với authentication:
1. Check Supabase connection
2. Verify database schema
3. Check console logs
4. Verify .env configuration

---

**Status**: ✅ Complete  
**Last Updated**: 2026-03-26
