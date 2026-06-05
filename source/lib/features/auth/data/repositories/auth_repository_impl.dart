import 'package:dartz/dartz.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/file_picker_utils.dart';
import '../../../../core/utils/logger.dart';
import '../../domain/entities/user_entity.dart';
import '../../domain/repositories/auth_repository.dart';
import '../datasources/auth_remote_datasource.dart';

/// Authentication repository implementation
class AuthRepositoryImpl implements AuthRepository {
  final AuthRemoteDataSource remoteDataSource;

  AuthRepositoryImpl({required this.remoteDataSource});

  @override
  Future<Either<Failure, UserEntity>> signUp({
    required String email,
    required String password,
    required String fullName,
    String? phoneNumber,
  }) async {
    try {
      final user = await remoteDataSource.signUp(
        email: email,
        password: password,
        fullName: fullName,
        phoneNumber: phoneNumber,
      );
      return Right(user.toEntity());
    } on AppAuthException catch (e) {
      Logger.error('Auth exception in repository', e);
      return Left(AuthFailure(message: e.message, code: e.code));
    } on NetworkException catch (e) {
      Logger.error('Network exception in repository', e);
      return Left(NetworkFailure(message: e.message));
    } on ServerException catch (e) {
      Logger.error('Server exception in repository', e);
      return Left(ServerFailure(message: e.message));
    } catch (e) {
      Logger.error('Unexpected error in repository', e);
      return const Left(
        ServerFailure(message: 'Đã xảy ra lỗi không mong muốn'),
      );
    }
  }

  @override
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
  }) async {
    try {
      final user = await remoteDataSource.signUpGuide(
        email: email,
        password: password,
        fullName: fullName,
        phoneNumber: phoneNumber,
        experience: experience,
        specialization: specialization,
        languages: languages,
        bio: bio,
        certificatePickedFile: certificatePickedFile,
      );
      return Right(user.toEntity());
    } on AppAuthException catch (e) {
      Logger.error('Auth exception in repository', e);
      return Left(AuthFailure(message: e.message, code: e.code));
    } on NetworkException catch (e) {
      Logger.error('Network exception in repository', e);
      return Left(NetworkFailure(message: e.message));
    } on ServerException catch (e) {
      Logger.error('Server exception in repository', e);
      return Left(ServerFailure(message: e.message));
    } catch (e) {
      Logger.error('Unexpected error in guide sign up', e);
      return const Left(
        ServerFailure(message: 'Đã xảy ra lỗi khi đăng ký hướng dẫn viên'),
      );
    }
  }

  @override
  Future<Either<Failure, UserEntity>> signIn({
    required String email,
    required String password,
  }) async {
    try {
      final user = await remoteDataSource.signIn(
        email: email,
        password: password,
      );
      return Right(user.toEntity());
    } on AppAuthException catch (e) {
      Logger.error('Auth exception in repository', e);
      return Left(AuthFailure(message: e.message, code: e.code));
    } on NetworkException catch (e) {
      Logger.error('Network exception in repository', e);
      return Left(NetworkFailure(message: e.message));
    } on ServerException catch (e) {
      Logger.error('Server exception in repository', e);
      return Left(ServerFailure(message: e.message));
    } catch (e) {
      Logger.error('Unexpected error in repository', e);
      return const Left(
        ServerFailure(message: 'Đã xảy ra lỗi không mong muốn'),
      );
    }
  }

  @override
  Future<Either<Failure, void>> signOut() async {
    try {
      await remoteDataSource.signOut();
      return const Right(null);
    } on ServerException catch (e) {
      Logger.error('Server exception in repository', e);
      return Left(ServerFailure(message: e.message));
    } catch (e) {
      Logger.error('Unexpected error in repository', e);
      return const Left(ServerFailure(message: 'Đã xảy ra lỗi khi đăng xuất'));
    }
  }

  @override
  Future<Either<Failure, UserEntity?>> getCurrentUser() async {
    try {
      final user = await remoteDataSource.getCurrentUser();
      return Right(user?.toEntity());
    } catch (e) {
      Logger.error('Error getting current user', e);
      return const Left(
        ServerFailure(message: 'Không thể lấy thông tin người dùng'),
      );
    }
  }

  @override
  Future<bool> isAuthenticated() async {
    try {
      return await remoteDataSource.isAuthenticated();
    } catch (e) {
      Logger.error('Error checking authentication', e);
      return false;
    }
  }

  @override
  Future<Either<Failure, void>> resetPassword({required String email}) async {
    try {
      await remoteDataSource.resetPassword(email: email);
      return const Right(null);
    } on AppAuthException catch (e) {
      Logger.error('Auth exception in repository', e);
      return Left(AuthFailure(message: e.message, code: e.code));
    } on ServerException catch (e) {
      Logger.error('Server exception in repository', e);
      return Left(ServerFailure(message: e.message));
    } catch (e) {
      Logger.error('Unexpected error in repository', e);
      return const Left(
        ServerFailure(message: 'Đã xảy ra lỗi khi đặt lại mật khẩu'),
      );
    }
  }

  @override
  Future<Either<Failure, UserEntity>> updateProfile({
    required String userId,
    String? fullName,
    String? phone,
    String? avatarUrl,
  }) async {
    try {
      final user = await remoteDataSource.updateProfile(
        userId: userId,
        fullName: fullName,
        phone: phone,
        avatarUrl: avatarUrl,
      );
      return Right(user.toEntity());
    } on ServerException catch (e) {
      Logger.error('Server exception in repository', e);
      return Left(ServerFailure(message: e.message));
    } catch (e) {
      Logger.error('Unexpected error in repository', e);
      return const Left(
        ServerFailure(message: 'Đã xảy ra lỗi khi cập nhật hồ sơ'),
      );
    }
  }
}
