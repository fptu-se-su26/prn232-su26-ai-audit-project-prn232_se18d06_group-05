/// ASP.NET Web API base URL
/// Đổi sang IP thực nếu test trên device thật (không dùng localhost)
class ApiConfig {
  // Flutter Web: dùng 127.0.0.1 thay localhost (tránh DNS resolve issue)
  // Android emulator: 10.0.2.2
  // iOS simulator: localhost hoặc 127.0.0.1
  // Device thật: IP máy tính (vd: 192.168.1.x)
  static const String baseUrl = 'http://localhost:5122/api';

  static const Duration timeout = Duration(seconds: 15);
}
