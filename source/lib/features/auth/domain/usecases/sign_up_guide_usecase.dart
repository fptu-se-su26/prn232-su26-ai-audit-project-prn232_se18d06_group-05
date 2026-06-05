import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/file_picker_utils.dart';
import '../entities/user_entity.dart';
import '../repositories/auth_repository.dart';

/// Use case for guide sign up with certificate upload
class SignUpGuideUseCase {
  final AuthRepository repository;

  SignUpGuideUseCase(this.repository);

  Future<Either<Failure, UserEntity>> call({
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
    return await repository.signUpGuide(
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
  }
}
