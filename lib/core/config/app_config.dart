import 'package:flutter_dotenv/flutter_dotenv.dart';

/// Application configuration
/// Centralized configuration management for the entire app
class AppConfig {
  // Supabase Configuration
  static String get supabaseUrl => dotenv.env['SUPABASE_URL'] ?? '';
  static String get supabaseAnonKey => dotenv.env['SUPABASE_ANON_KEY'] ?? '';

  // API Configuration
  static const int apiTimeout = 30000; // 30 seconds
  static const int connectTimeout = 15000; // 15 seconds

  // Pagination
  static const int defaultPageSize = 20;
  static const int maxPageSize = 100;

  // Session
  static const int sessionTimeoutMinutes = 30;

  // Validation
  static const int minPasswordLength = 8;
  static const int maxPasswordLength = 128;

  // Feature Flags
  static const bool enableRealtime = true;
  static const bool enableNotifications = false; // Future feature

  // Environment
  static bool get isProduction => const bool.fromEnvironment('dart.vm.product');
  static bool get isDevelopment => !isProduction;
}
