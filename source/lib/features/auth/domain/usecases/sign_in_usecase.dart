import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/user_entity.dart';
import '../repositories/auth_repository.dart';

/// Sign in use case
class SignInUseCase {
  final AuthRepository repository;

  SignInUseCase(this.repository);

  Future<Either<Failure, UserEntity>> call({
    required String email,
    required String password,
  }) async {
    // Validate inputs
    if (email.isEmpty) {
      return const Left(
        ValidationFailure(message: 'Email không được để trống'),
      );
    }

    if (password.isEmpty) {
      return const Left(
        ValidationFailure(message: 'Mật khẩu không được để trống'),
      );
    }

    // Call repository
    return await repository.signIn(email: email, password: password);
  }
}
