import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/file_picker_utils.dart';
import '../entities/user_entity.dart';

/// Authentication repository interface
/// Defines contract for authentication operations
abstract class AuthRepository {
  /// Sign up with email and password
  Future<Either<Failure, UserEntity>> signUp({
    required String email,
    required String password,
    required String fullName,
    String? phoneNumber,
  });

  /// Sign up as guide with additional information and certificate
  Future<Either<Failure, UserEntity>> signUpGuide({
    required String email,
    required String password,
    required String fullName,
    required String phoneNumber,
    String? experience,
    String? specialization,
    String? languages,
    String? bio,
    PickedFile? certificatePickedFile,
  });

  /// Sign in with email and password
  Future<Either<Failure, UserEntity>> signIn({
    required String email,
    required String password,
  });

  /// Sign out current user
  Future<Either<Failure, void>> signOut();

  /// Get current user
  Future<Either<Failure, UserEntity?>> getCurrentUser();

  /// Check if user is authenticated
  Future<bool> isAuthenticated();

  /// Reset password
  Future<Either<Failure, void>> resetPassword({required String email});

  /// Update user profile
  Future<Either<Failure, UserEntity>> updateProfile({
    required String userId,
    String? fullName,
    String? phone,
    String? avatarUrl,
  });
}
