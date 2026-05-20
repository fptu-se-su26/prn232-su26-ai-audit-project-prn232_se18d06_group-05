import 'package:shared_preferences/shared_preferences.dart';

/// Lưu trữ JWT token từ ASP.NET Web API
class TokenStorage {
  static const _accessTokenKey = 'api_access_token';
  static const _refreshTokenKey = 'api_refresh_token';
  static const _expiresAtKey = 'api_expires_at';

  static Future<void> save({
    required String accessToken,
    required String refreshToken,
    required int expiresAt,
  }) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_accessTokenKey, accessToken);
    await prefs.setString(_refreshTokenKey, refreshToken);
    await prefs.setInt(_expiresAtKey, expiresAt);
  }

  static Future<String?> getAccessToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_accessTokenKey);
  }

  static Future<String?> getRefreshToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_refreshTokenKey);
  }

  static Future<bool> isTokenValid() async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString(_accessTokenKey);
    final expiresAt = prefs.getInt(_expiresAtKey);
    if (token == null || expiresAt == null) return false;
    // Coi là hết hạn nếu còn dưới 60 giây
    return DateTime.now().millisecondsSinceEpoch < (expiresAt * 1000) - 60000;
  }

  static Future<void> clear() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_accessTokenKey);
    await prefs.remove(_refreshTokenKey);
    await prefs.remove(_expiresAtKey);
  }
}
