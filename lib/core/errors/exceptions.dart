/// Base exception class
class AppException implements Exception {
  final String message;
  final String? code;
  final dynamic details;

  AppException({required this.message, this.code, this.details});

  @override
  String toString() => 'AppException(message: $message, code: $code)';
}

/// Network exception
class NetworkException extends AppException {
  NetworkException({required super.message, super.code, super.details});
}

/// Server exception
class ServerException extends AppException {
  ServerException({required super.message, super.code, super.details});
}

/// Authentication exception
class AppAuthException extends AppException {
  AppAuthException({required super.message, super.code, super.details});
}

/// Validation exception
class ValidationException extends AppException {
  ValidationException({required super.message, super.code, super.details});
}

/// Cache exception
class CacheException extends AppException {
  CacheException({required super.message, super.code, super.details});
}
