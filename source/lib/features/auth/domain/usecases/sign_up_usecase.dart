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
    String? phoneNumber,
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

    if (password.length < 6) { // Note: Backend requires at least 6 characters, let's change 8 to 6 to align!
      return const Left(
        ValidationFailure(message: 'Mật khẩu phải có ít nhất 6 ký tự'),
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
      phoneNumber: phoneNumber,
    );
  }

  bool _isValidEmail(String email) {
    final emailRegex = RegExp(r'^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$');
    return emailRegex.hasMatch(email);
  }
}
