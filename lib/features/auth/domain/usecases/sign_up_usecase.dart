import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/user_entity.dart';
import '../repositories/auth_repository.dart';

/// Sign up use case
class SignUpUseCase {
  final AuthRepository repository;

  SignUpUseCase(this.repository);

  Future<Either<Failure, UserEntity>> call({
    required String email,
    required String password,
    required String fullName,
  }) async {
    // Validate inputs
    if (email.isEmpty) {
      return const Left(
        ValidationFailure(message: 'Email không được để trống'),
      );
    }

    if (!_isValidEmail(email)) {
      return const Left(ValidationFailure(message: 'Email không hợp lệ'));
    }

    if (password.isEmpty) {
      return const Left(
        ValidationFailure(message: 'Mật khẩu không được để trống'),
      );
    }

    if (password.length < 8) {
      return const Left(
        ValidationFailure(message: 'Mật khẩu phải có ít nhất 8 ký tự'),
      );
    }

    if (fullName.isEmpty) {
      return const Left(
        ValidationFailure(message: 'Họ tên không được để trống'),
      );
    }

    // Call repository
    return await repository.signUp(
      email: email,
      password: password,
      fullName: fullName,
    );
  }

  bool _isValidEmail(String email) {
    final emailRegex = RegExp(r'^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$');
    return emailRegex.hasMatch(email);
  }
}
