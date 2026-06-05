import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../data/datasources/api_auth_datasource.dart';
import '../../data/datasources/auth_remote_datasource.dart';
import '../../data/repositories/auth_repository_impl.dart';
import '../../domain/repositories/auth_repository.dart';
import '../../domain/usecases/get_current_user_usecase.dart';
import '../../domain/usecases/sign_in_usecase.dart';
import '../../domain/usecases/sign_out_usecase.dart';
import '../../domain/usecases/sign_up_usecase.dart';
import '../../domain/usecases/sign_up_guide_usecase.dart';

/// ── Datasource switch ────────────────────────────────────────────────────────
/// true  → gọi ASP.NET Web API
/// false → gọi Supabase trực tiếp (legacy)
const bool _useWebApi = true;

final authRemoteDataSourceProvider = Provider<AuthRemoteDataSource>((ref) {
  if (_useWebApi) return ApiAuthDataSource();
  return AuthRemoteDataSourceImpl();
});

/// ── Repository & Use Cases ───────────────────────────────────────────────────

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepositoryImpl(
    remoteDataSource: ref.watch(authRemoteDataSourceProvider),
  );
});

final signUpUseCaseProvider = Provider<SignUpUseCase>((ref) {
  return SignUpUseCase(ref.watch(authRepositoryProvider));
});

final signUpGuideUseCaseProvider = Provider<SignUpGuideUseCase>((ref) {
  return SignUpGuideUseCase(ref.watch(authRepositoryProvider));
});

final signInUseCaseProvider = Provider<SignInUseCase>((ref) {
  return SignInUseCase(ref.watch(authRepositoryProvider));
});

final signOutUseCaseProvider = Provider<SignOutUseCase>((ref) {
  return SignOutUseCase(ref.watch(authRepositoryProvider));
});

final getCurrentUserUseCaseProvider = Provider<GetCurrentUserUseCase>((ref) {
  return GetCurrentUserUseCase(ref.watch(authRepositoryProvider));
});
