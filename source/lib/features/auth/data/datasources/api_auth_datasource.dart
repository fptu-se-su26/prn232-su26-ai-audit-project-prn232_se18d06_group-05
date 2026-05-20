import 'package:dio/dio.dart';
import '../../../../core/config/api_config.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/services/token_storage.dart';
import '../../../../core/utils/logger.dart';
import '../models/user_model.dart';
import 'auth_remote_datasource.dart';

/// Auth datasource gọi ASP.NET Web API
class ApiAuthDataSource implements AuthRemoteDataSource {
  late final Dio _dio;

  ApiAuthDataSource() {
    _dio = Dio(
      BaseOptions(
        baseUrl: ApiConfig.baseUrl,
        connectTimeout: ApiConfig.timeout,
        receiveTimeout: ApiConfig.timeout,
        headers: {'Content-Type': 'application/json'},
      ),
    );

    // Log requests trong debug
    _dio.interceptors.add(
      LogInterceptor(
        requestBody: true,
        responseBody: true,
        logPrint: (o) => Logger.info(o.toString()),
      ),
    );
  }

  // ── Sign In ────────────────────────────────────────────────────────────────

  @override
  Future<UserModel> signIn({
    required String email,
    required String password,
  }) async {
    try {
      Logger.info('[API] Sign in: $email');
      final res = await _dio.post(
        '/auth/login',
        data: {'email': email, 'password': password},
      );

      final data = res.data as Map<String, dynamic>;
      await TokenStorage.save(
        accessToken: data['accessToken'] as String,
        refreshToken: data['refreshToken'] as String,
        expiresAt: data['expiresAt'] as int,
      );

      Logger.success('[API] Sign in success');
      return UserModel.fromApiJson(data['user'] as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _mapDioError(e);
    }
  }

  // ── Sign Up ────────────────────────────────────────────────────────────────

  @override
  Future<UserModel> signUp({
    required String email,
    required String password,
    required String fullName,
  }) async {
    try {
      Logger.info('[API] Register: $email');
      final res = await _dio.post(
        '/auth/register',
        data: {'email': email, 'password': password, 'fullName': fullName},
      );

      final data = res.data as Map<String, dynamic>;
      await TokenStorage.save(
        accessToken: data['accessToken'] as String,
        refreshToken: data['refreshToken'] as String,
        expiresAt: data['expiresAt'] as int,
      );

      Logger.success('[API] Register success');
      return UserModel.fromApiJson(data['user'] as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _mapDioError(e);
    }
  }

  // ── Sign Out ───────────────────────────────────────────────────────────────

  @override
  Future<void> signOut() async {
    await TokenStorage.clear();
    Logger.info('[API] Signed out — token cleared');
  }

  // ── Get Current User ───────────────────────────────────────────────────────

  @override
  Future<UserModel?> getCurrentUser() async {
    try {
      // First check if we have a valid token
      final valid = await TokenStorage.isTokenValid();
      if (!valid) {
        Logger.info('[API] Token expired, attempting refresh...');
        // Try to refresh the token
        final refreshed = await _tryRefresh();
        if (!refreshed) {
          Logger.info('[API] Token refresh failed, user not authenticated');
          return null;
        }
      }

      final token = await TokenStorage.getAccessToken();
      if (token == null) {
        Logger.info('[API] No access token found');
        return null;
      }

      Logger.info('[API] Getting current user with valid token');
      final res = await _dio.get(
        '/auth/me',
        options: Options(headers: {'Authorization': 'Bearer $token'}),
      );

      final data = res.data as Map<String, dynamic>;
      Logger.success('[API] Current user retrieved successfully');

      // /me chỉ trả id + email + role, tạo UserModel minimal
      return UserModel(
        id: data['id'] as String,
        email: data['email'] as String? ?? '',
        role: data['role'] as String? ?? 'traveler',
        createdAt: DateTime.now(),
      );
    } catch (e) {
      Logger.error('[API] getCurrentUser failed', e);
      // Clear invalid tokens
      await TokenStorage.clear();
      return null;
    }
  }

  // ── isAuthenticated ────────────────────────────────────────────────────────

  @override
  Future<bool> isAuthenticated() async {
    if (await TokenStorage.isTokenValid()) return true;
    return _tryRefresh();
  }

  // ── Reset Password ─────────────────────────────────────────────────────────

  @override
  Future<void> resetPassword({required String email}) async {
    // Chưa implement trên API — fallback thông báo
    throw AppAuthException(
      message: 'Tính năng đặt lại mật khẩu chưa được hỗ trợ qua API',
    );
  }

  // ── Update Profile ─────────────────────────────────────────────────────────

  @override
  Future<UserModel> updateProfile({
    required String userId,
    String? fullName,
    String? phone,
    String? avatarUrl,
  }) async {
    throw ServerException(message: 'updateProfile chưa implement trên API');
  }

  // ── Helpers ────────────────────────────────────────────────────────────────

  Future<bool> _tryRefresh() async {
    try {
      final refreshToken = await TokenStorage.getRefreshToken();
      if (refreshToken == null) return false;

      final res = await _dio.post(
        '/auth/refresh',
        data: {'refreshToken': refreshToken},
      );

      final data = res.data as Map<String, dynamic>;
      await TokenStorage.save(
        accessToken: data['accessToken'] as String,
        refreshToken: data['refreshToken'] as String,
        expiresAt: data['expiresAt'] as int,
      );
      Logger.success('[API] Token refreshed');
      return true;
    } catch (e) {
      Logger.error('[API] Token refresh failed', e);
      await TokenStorage.clear();
      return false;
    }
  }

  AppException _mapDioError(DioException e) {
    final statusCode = e.response?.statusCode;
    final body = e.response?.data;

    String message = 'Lỗi kết nối';
    if (body is Map && body['message'] != null) {
      message = body['message'] as String;
    } else if (e.type == DioExceptionType.connectionTimeout ||
        e.type == DioExceptionType.receiveTimeout) {
      message = 'Kết nối quá thời gian. Kiểm tra server đang chạy chưa.';
    } else if (e.type == DioExceptionType.connectionError) {
      message = 'Không thể kết nối tới server. Kiểm tra API đang chạy.';
    }

    Logger.error('[API] HTTP $statusCode — $message');

    if (statusCode == 401) return AppAuthException(message: message);
    if (statusCode == 400) return AppAuthException(message: message);
    return ServerException(message: message);
  }
}
