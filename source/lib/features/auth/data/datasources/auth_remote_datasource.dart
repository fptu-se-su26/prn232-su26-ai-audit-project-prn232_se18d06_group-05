import 'package:supabase_flutter/supabase_flutter.dart' hide AuthException;
import '../../../../core/config/supabase_config.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/utils/logger.dart';
import '../models/user_model.dart';

/// Authentication remote data source
/// Handles all Supabase authentication operations
abstract class AuthRemoteDataSource {
  Future<UserModel> signUp({
    required String email,
    required String password,
    required String fullName,
  });

  Future<UserModel> signIn({required String email, required String password});

  Future<void> signOut();

  Future<UserModel?> getCurrentUser();

  Future<bool> isAuthenticated();

  Future<void> resetPassword({required String email});

  Future<UserModel> updateProfile({
    required String userId,
    String? fullName,
    String? phone,
    String? avatarUrl,
  });
}

class AuthRemoteDataSourceImpl implements AuthRemoteDataSource {
  final SupabaseClient client;

  AuthRemoteDataSourceImpl({SupabaseClient? client})
    : client = client ?? SupabaseConfig.client;

  @override
  Future<UserModel> signUp({
    required String email,
    required String password,
    required String fullName,
  }) async {
    try {
      Logger.info('Signing up user: $email');

      final response = await client.auth.signUp(
        email: email,
        password: password,
        data: {'full_name': fullName},
      );

      if (response.user == null) {
        throw AppAuthException(message: 'Đăng ký thất bại');
      }

      Logger.success('User signed up: ${response.user!.id}');
      Logger.info(
        'User email confirmed: ${response.user!.emailConfirmedAt != null}',
      );

      // Create profile manually (more reliable than trigger)
      await _createUserProfile(
        userId: response.user!.id,
        email: email,
        fullName: fullName,
      );

      // Wait a bit for database to sync
      await Future.delayed(const Duration(milliseconds: 500));

      // Get user profile from profiles table
      final profile = await _getUserProfile(response.user!.id);
      return profile;
    } catch (e) {
      Logger.error('Auth error during sign up', e);
      if (e is AppAuthException) rethrow;
      if (e is ServerException) rethrow;

      // Handle Supabase auth errors
      final errorMessage = e.toString();
      throw AppAuthException(message: _getAuthErrorMessage(errorMessage));
    }
  }

  @override
  Future<UserModel> signIn({
    required String email,
    required String password,
  }) async {
    try {
      Logger.info('Signing in user: $email');

      final response = await client.auth.signInWithPassword(
        email: email,
        password: password,
      );

      if (response.user == null) {
        throw AppAuthException(message: 'Đăng nhập thất bại');
      }

      Logger.success('User signed in: ${response.user!.id}');

      // Get user profile
      final profile = await _getUserProfile(response.user!.id);
      return profile;
    } catch (e) {
      Logger.error('Auth error during sign in', e);
      if (e is AppAuthException) rethrow;

      // Handle Supabase auth errors
      final errorMessage = e.toString();
      throw AppAuthException(message: _getAuthErrorMessage(errorMessage));
    }
  }

  @override
  Future<void> signOut() async {
    try {
      Logger.info('Signing out user');
      await client.auth.signOut();
      Logger.success('User signed out');
    } catch (e) {
      Logger.error('Error during sign out', e);
      throw ServerException(message: 'Lỗi khi đăng xuất');
    }
  }

  @override
  Future<UserModel?> getCurrentUser() async {
    try {
      final user = client.auth.currentUser;
      if (user == null) {
        Logger.info('No current user');
        return null;
      }

      Logger.info('Getting current user: ${user.id}');
      final profile = await _getUserProfile(user.id);
      return profile;
    } catch (e) {
      Logger.error('Error getting current user', e);
      return null;
    }
  }

  @override
  Future<bool> isAuthenticated() async {
    final session = client.auth.currentSession;
    return session != null;
  }

  @override
  Future<void> resetPassword({required String email}) async {
    try {
      Logger.info('Resetting password for: $email');
      await client.auth.resetPasswordForEmail(email);
      Logger.success('Password reset email sent');
    } catch (e) {
      Logger.error('Auth error during password reset', e);
      final errorMessage = e.toString();
      throw AppAuthException(message: _getAuthErrorMessage(errorMessage));
    }
  }

  @override
  Future<UserModel> updateProfile({
    required String userId,
    String? fullName,
    String? phone,
    String? avatarUrl,
  }) async {
    try {
      Logger.info('Updating profile for user: $userId');

      final updates = <String, dynamic>{};
      if (fullName != null) updates['full_name'] = fullName;
      if (phone != null) updates['phone'] = phone;
      if (avatarUrl != null) updates['avatar_url'] = avatarUrl;
      updates['updated_at'] = DateTime.now().toIso8601String();

      await client.from('profiles').update(updates).eq('id', userId);

      Logger.success('Profile updated');

      // Get updated profile
      final profile = await _getUserProfile(userId);
      return profile;
    } catch (e) {
      Logger.error('Error updating profile', e);
      throw ServerException(message: 'Lỗi khi cập nhật hồ sơ');
    }
  }

  /// Create user profile in profiles table
  Future<void> _createUserProfile({
    required String userId,
    required String email,
    required String fullName,
  }) async {
    try {
      Logger.info('Creating profile for user: $userId');
      Logger.info('Email: $email, Full name: $fullName');

      // Check current auth state
      final currentUser = client.auth.currentUser;
      Logger.info('Current auth user: ${currentUser?.id}');
      Logger.info('Auth session exists: ${client.auth.currentSession != null}');

      // Use upsert to handle both insert and update cases
      final response = await client.from('profiles').upsert({
        'id': userId,
        'email': email,
        'full_name': fullName,
        'role': 'traveler',
        'created_at': DateTime.now().toIso8601String(),
        'updated_at': DateTime.now().toIso8601String(),
      }, onConflict: 'id').select();

      Logger.success('Profile created/updated successfully');
      Logger.info('Profile response: $response');
    } catch (e, stackTrace) {
      Logger.error('Error creating user profile', e);
      Logger.error('Stack trace', stackTrace);

      // Log detailed error info
      if (e is PostgrestException) {
        Logger.error('Postgrest error code: ${e.code}');
        Logger.error('Postgrest error message: ${e.message}');
        Logger.error('Postgrest error details: ${e.details}');
        Logger.error('Postgrest error hint: ${e.hint}');
      }

      throw ServerException(
        message: 'Không thể tạo hồ sơ người dùng: ${e.toString()}',
      );
    }
  }

  /// Get user profile from profiles table
  Future<UserModel> _getUserProfile(String userId) async {
    try {
      final response = await client
          .from('profiles')
          .select()
          .eq('id', userId)
          .single();

      return UserModel.fromJson(response);
    } catch (e) {
      Logger.error('Error getting user profile', e);
      throw ServerException(message: 'Không thể lấy thông tin người dùng');
    }
  }

  /// Convert Supabase auth error to user-friendly message
  String _getAuthErrorMessage(String message) {
    if (message.contains('Invalid login credentials')) {
      return 'Email hoặc mật khẩu không đúng';
    }
    if (message.contains('User already registered')) {
      return 'Email đã được đăng ký';
    }
    if (message.contains('Email not confirmed')) {
      return 'Vui lòng xác nhận email';
    }
    if (message.contains('Invalid email')) {
      return 'Email không hợp lệ';
    }
    return 'Lỗi xác thực. Vui lòng thử lại.';
  }
}
