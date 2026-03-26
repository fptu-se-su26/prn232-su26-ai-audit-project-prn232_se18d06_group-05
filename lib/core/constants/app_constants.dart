/// Application-wide constants
class AppConstants {
  // App Info
  static const String appName = 'TripMate';
  static const String appVersion = '1.0.0';

  // Routes
  static const String splashRoute = '/';
  static const String loginRoute = '/login';
  static const String signupRoute = '/signup';
  static const String homeRoute = '/home';
  static const String tourListRoute = '/tours';
  static const String tourDetailRoute = '/tours/:id';
  static const String bookingRoute = '/booking';
  static const String bookingHistoryRoute = '/bookings';
  static const String profileRoute = '/profile';

  // Storage Keys
  static const String authTokenKey = 'auth_token';
  static const String userIdKey = 'user_id';
  static const String userPrefsKey = 'user_preferences';

  // Error Messages
  static const String networkError =
      'Không thể kết nối. Vui lòng kiểm tra mạng.';
  static const String serverError = 'Lỗi máy chủ. Vui lòng thử lại sau.';
  static const String unknownError = 'Đã xảy ra lỗi. Vui lòng thử lại.';
  static const String authError =
      'Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.';

  // Validation Messages
  static const String emailRequired = 'Vui lòng nhập email';
  static const String emailInvalid = 'Email không hợp lệ';
  static const String passwordRequired = 'Vui lòng nhập mật khẩu';
  static const String passwordTooShort = 'Mật khẩu phải có ít nhất 8 ký tự';
}
