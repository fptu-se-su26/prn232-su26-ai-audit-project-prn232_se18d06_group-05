/// Base class for all failures in the application
abstract class Failure {
  final String message;
  final String? code;
  final dynamic details;

  const Failure({required this.message, this.code, this.details});

  @override
  String toString() => 'Failure(message: $message, code: $code)';
}

/// Network-related failures
class NetworkFailure extends Failure {
  const NetworkFailure({required super.message, super.code, super.details});
}

/// Server-related failures
class ServerFailure extends Failure {
  const ServerFailure({required super.message, super.code, super.details});
}

/// Authentication failures
class AuthFailure extends Failure {
  const AuthFailure({required super.message, super.code, super.details});
}

/// Validation failures
class ValidationFailure extends Failure {
  const ValidationFailure({required super.message, super.code, super.details});
}

/// Cache failures
class CacheFailure extends Failure {
  const CacheFailure({required super.message, super.code, super.details});
}

/// Not found failures
class NotFoundFailure extends Failure {
  const NotFoundFailure({required super.message, super.code, super.details});
}

/// Permission failures
class PermissionFailure extends Failure {
  const PermissionFailure({required super.message, super.code, super.details});
}
