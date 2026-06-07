import 'dart:convert';
import 'dart:typed_data';
import 'package:dio/dio.dart';
import '../../../../core/config/api_config.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/services/token_storage.dart';
import '../../../../core/utils/file_picker_utils.dart';
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
        // Không set Content-Type mặc định — để từng request tự quyết định
        // (JSON cho signIn, multipart/form-data tự động khi dùng FormData)
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
        // signIn gửi JSON body
        options: Options(headers: {'Content-Type': 'application/json'}),
      );

      final data = res.data as Map<String, dynamic>;
      final accessToken = data['accessToken'] as String;
      final refreshToken = data['refreshToken'] as String? ?? '';
      // Backend không trả expiresAt → mặc định token hết hạn sau 1 giờ
      final expiresAt = data['expiresAt'] as int? ??
          (DateTime.now().millisecondsSinceEpoch ~/ 1000) + 3600;

      await TokenStorage.save(
        accessToken: accessToken,
        refreshToken: refreshToken,
        expiresAt: expiresAt,
      );

      // Parse user info từ response
      final userJson = data['user'] as Map<String, dynamic>?;
      Logger.success('[API] Sign in success');

      if (userJson != null) {
        return UserModel.fromApiJson(userJson);
      }

      // Fallback: tạo minimal UserModel từ JWT payload (decode thủ công)
      return _parseUserFromToken(accessToken, email);
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
    String? phoneNumber,
  }) async {
    try {
      Logger.info('[API] Register traveler: $email');

      // Backend dùng [FromForm] nên phải gửi form-data, không phải JSON
      final formData = FormData.fromMap({
        'email': email,
        'password': password,
        'fullName': fullName,
        'role': 'traveler',
        if (phoneNumber != null && phoneNumber.isNotEmpty)
          'phoneNumber': phoneNumber,
      });

      final res = await _dio.post(
        '/auth/register',
        data: formData,
        // KHÔNG set contentType — Dio tự động set 'multipart/form-data; boundary=...'
        // khi nhận FormData. Nếu set thủ công sẽ mất boundary, server sẽ không parse được.
      );

      final data = res.data as Map<String, dynamic>;
      final accessToken = data['accessToken'] as String?;
      final refreshToken = data['refreshToken'] as String? ?? '';
      final expiresAt = data['expiresAt'] as int? ??
          (DateTime.now().millisecondsSinceEpoch ~/ 1000) + 3600;

      if (accessToken != null) {
        await TokenStorage.save(
          accessToken: accessToken,
          refreshToken: refreshToken,
          expiresAt: expiresAt,
        );
      }

      Logger.success('[API] Register success');
      final userJson = data['user'] as Map<String, dynamic>?;
      if (userJson != null) {
        return UserModel.fromApiJson(userJson);
      }
      // Fallback minimal
      return UserModel(
        id: '',
        email: email,
        fullName: fullName,
        role: 'traveler',
        createdAt: DateTime.now(),
      );
    } on DioException catch (e) {
      throw _mapDioError(e);
    }
  }

  // ── Sign Up Guide ──────────────────────────────────────────────────────────

  @override
  Future<UserModel> signUpGuide({
    required String email,
    required String password,
    required String fullName,
    required String phoneNumber,
    String? experience,
    String? specialization,
    String? languages,
    String? bio,
    PickedFile? certificatePickedFile,
  }) async {
    try {
      Logger.info('[API] Register guide: $email');

      // Build form fields (exclude null values)
      final fields = <String, dynamic>{
        'email': email,
        'password': password,
        'fullName': fullName,
        'phoneNumber': phoneNumber,
        'role': 'guide',
        if (experience != null) 'experience': experience,
        if (specialization != null) 'specialization': specialization,
        if (languages != null) 'languages': languages,
        if (bio != null) 'bio': bio,
      };

      final formData = FormData.fromMap(fields);

      // Add certificate file if provided
      if (certificatePickedFile != null) {
        final fileName = certificatePickedFile.name;
        MultipartFile multipartFile;

        if (certificatePickedFile.hasFile && certificatePickedFile.file != null) {
          // Mobile/Desktop: dùng file path
          multipartFile = await MultipartFile.fromFile(
            certificatePickedFile.file!.path,
            filename: fileName,
          );
        } else if (certificatePickedFile.hasBytes &&
            certificatePickedFile.bytes != null) {
          // Web: dùng bytes
          multipartFile = MultipartFile.fromBytes(
            certificatePickedFile.bytes!,
            filename: fileName,
          );
        } else {
          Logger.info('[API] Certificate PickedFile has no data, skipping');
          multipartFile = MultipartFile.fromBytes(
            Uint8List(0),
            filename: fileName,
          );
        }

        formData.files.add(MapEntry('Certificate', multipartFile));
        Logger.info('[API] Certificate attached: $fileName');
      }

      final res = await _dio.post(
        '/auth/register',
        data: formData,
        // KHÔNG set contentType — Dio tự set multipart/form-data; boundary=...
      );

      final data = res.data as Map<String, dynamic>;

      // Backend trả message thành công, không có accessToken ngay
      // Guide cần được admin duyệt trước khi đăng nhập
      Logger.success('[API] Guide register success — awaiting approval');

      // Trả về UserModel tạm thời với role guide (chưa có token)
      return UserModel(
        id: data['userId'] as String? ?? '',
        email: email,
        fullName: fullName,
        role: 'guide',
        createdAt: DateTime.now(),
      );
    } on DioException catch (e) {
      throw _mapDioError(e);
    } catch (e) {
      Logger.error('[API] Guide register error', e);
      throw ServerException(message: 'Lỗi khi đăng ký hướng dẫn viên: $e');
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
      // Kiểm tra token có hợp lệ không
      final token = await TokenStorage.getAccessToken();
      if (token == null) {
        Logger.info('[API] No access token found');
        return null;
      }

      final valid = await TokenStorage.isTokenValid();
      if (!valid) {
        Logger.info('[API] Token expired, attempting refresh...');
        final refreshed = await _tryRefresh();
        if (!refreshed) {
          Logger.info('[API] Token refresh failed');
          return null;
        }
      }

      // Decode JWT payload để lấy thông tin user (không cần gọi API)
      final freshToken = await TokenStorage.getAccessToken() ?? token;
      final userFromToken = _parseUserFromToken(freshToken, null);
      Logger.success('[API] Current user from token: ${userFromToken.email}');
      return userFromToken;
    } catch (e) {
      Logger.error('[API] getCurrentUser failed', e);
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
        refreshToken: data['refreshToken'] as String? ?? '',
        expiresAt: data['expiresAt'] as int? ??
            (DateTime.now().millisecondsSinceEpoch ~/ 1000) + 3600,
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

  /// Decode JWT payload (base64) để lấy thông tin user mà không gọi API
  UserModel _parseUserFromToken(String token, String? fallbackEmail) {
    try {
      final parts = token.split('.');
      if (parts.length < 2) throw Exception('Invalid JWT');

      // JWT payload là phần thứ 2 (index 1), encode base64url
      String payload = parts[1];
      // Thêm padding nếu thiếu
      switch (payload.length % 4) {
        case 2:
          payload += '==';
          break;
        case 3:
          payload += '=';
          break;
      }

      final decoded = utf8.decode(base64Url.decode(payload));
      final json = jsonDecode(decoded) as Map<String, dynamic>;

      // JWT claims: sub = userId, email, role
      final id = json['sub'] as String? ?? json['id'] as String? ?? '';
      final email = json['email'] as String? ?? fallbackEmail ?? '';
      final role = json['role'] as String? ??
          json['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
              as String? ??
          'traveler';

      Logger.info('[API] JWT decoded: id=$id, email=$email, role=$role');
      return UserModel(
        id: id,
        email: email,
        role: role,
        createdAt: DateTime.now(),
      );
    } catch (e) {
      Logger.error('[API] JWT decode failed, using fallback', e);
      // Trả về minimal model để không crash
      return UserModel(
        id: '',
        email: fallbackEmail ?? '',
        role: 'traveler',
        createdAt: DateTime.now(),
      );
    }
  }
}
