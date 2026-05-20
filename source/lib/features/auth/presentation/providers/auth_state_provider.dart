import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../domain/entities/user_entity.dart';
import 'auth_providers.dart';

/// Auth state
class AuthState {
  final UserEntity? user;
  final bool isLoading;
  final String? error;
  final bool isAuthenticated;

  const AuthState({
    this.user,
    this.isLoading = false,
    this.error,
    this.isAuthenticated = false,
  });

  AuthState copyWith({
    UserEntity? user,
    bool? isLoading,
    String? error,
    bool? isAuthenticated,
  }) {
    return AuthState(
      user: user ?? this.user,
      isLoading: isLoading ?? this.isLoading,
      error: error,
      isAuthenticated: isAuthenticated ?? this.isAuthenticated,
    );
  }
}

/// Auth state notifier
class AuthStateNotifier extends StateNotifier<AuthState> {
  final Ref ref;

  AuthStateNotifier(this.ref) : super(const AuthState()) {
    _checkAuthStatus();
  }

  /// Check authentication status on init
  Future<void> _checkAuthStatus() async {
    state = state.copyWith(isLoading: true);

    try {
      final getCurrentUserUseCase = ref.read(getCurrentUserUseCaseProvider);
      final result = await getCurrentUserUseCase();

      result.fold(
        (failure) {
          state = const AuthState(isLoading: false, isAuthenticated: false);
        },
        (user) {
          state = AuthState(
            user: user,
            isLoading: false,
            isAuthenticated: user != null,
          );
        },
      );
    } catch (e) {
      // Handle any unexpected errors during auth check
      state = const AuthState(isLoading: false, isAuthenticated: false);
    }
  }

  /// Force refresh auth status (useful for manual refresh)
  Future<void> refreshAuthStatus() async {
    await _checkAuthStatus();
  }

  /// Sign up
  Future<bool> signUp({
    required String email,
    required String password,
    required String fullName,
  }) async {
    state = state.copyWith(isLoading: true, error: null);

    final signUpUseCase = ref.read(signUpUseCaseProvider);
    final result = await signUpUseCase(
      email: email,
      password: password,
      fullName: fullName,
    );

    return result.fold(
      (failure) {
        state = state.copyWith(isLoading: false, error: failure.message);
        return false;
      },
      (user) {
        state = AuthState(user: user, isLoading: false, isAuthenticated: true);
        return true;
      },
    );
  }

  /// Sign in
  Future<bool> signIn({required String email, required String password}) async {
    state = state.copyWith(isLoading: true, error: null);

    final signInUseCase = ref.read(signInUseCaseProvider);
    final result = await signInUseCase(email: email, password: password);

    return result.fold(
      (failure) {
        state = state.copyWith(isLoading: false, error: failure.message);
        return false;
      },
      (user) {
        state = AuthState(user: user, isLoading: false, isAuthenticated: true);
        return true;
      },
    );
  }

  /// Sign out
  Future<bool> signOut() async {
    state = state.copyWith(isLoading: true);

    final signOutUseCase = ref.read(signOutUseCaseProvider);
    final result = await signOutUseCase();

    return result.fold(
      (failure) {
        state = state.copyWith(isLoading: false, error: failure.message);
        return false;
      },
      (_) {
        state = const AuthState(isLoading: false, isAuthenticated: false);
        return true;
      },
    );
  }

  /// Clear error
  void clearError() {
    state = state.copyWith(error: null);
  }
}

/// Auth state provider
final authStateProvider = StateNotifierProvider<AuthStateNotifier, AuthState>((
  ref,
) {
  return AuthStateNotifier(ref);
});
