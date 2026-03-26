import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'core/config/supabase_config.dart';
import 'core/config/supabase_test.dart';
import 'core/theme/app_theme.dart';
import 'core/constants/app_constants.dart';
import 'core/utils/logger.dart';
import 'core/enums/user_role.dart';
import 'features/auth/presentation/screens/login_screen.dart';
import 'features/auth/presentation/screens/home_screen.dart';
import 'features/auth/presentation/providers/auth_state_provider.dart';
import 'features/tour/presentation/screens/tour_list_screen.dart';
import 'features/dashboard/presentation/screens/traveler_dashboard_screen.dart';
import 'features/dashboard/presentation/screens/guide_dashboard_screen.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  try {
    // Load environment variables
    Logger.info('Loading environment variables...');
    await dotenv.load(fileName: '.env');
    Logger.success('Environment variables loaded');

    // Initialize Supabase
    Logger.info('Initializing Supabase...');
    await SupabaseConfig.initialize();
    Logger.success('Supabase initialized successfully');

    // Test Supabase connection
    await testSupabaseConnection();

    runApp(const ProviderScope(child: TripMateApp()));
  } catch (e, stackTrace) {
    Logger.error('Failed to initialize app', e, stackTrace);
    runApp(
      MaterialApp(
        home: Scaffold(
          body: Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Icon(Icons.error, size: 64, color: Colors.red),
                const SizedBox(height: 16),
                Text('Lỗi khởi tạo ứng dụng: $e'),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class TripMateApp extends ConsumerWidget {
  const TripMateApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return MaterialApp(
      title: AppConstants.appName,
      theme: AppTheme.lightTheme,
      debugShowCheckedModeBanner: false,
      home: const AuthWrapper(),
      routes: {
        AppConstants.loginRoute: (context) => const LoginScreen(),
        AppConstants.homeRoute: (context) => const HomeScreen(),
        AppConstants.tourListRoute: (context) => const TourListScreen(),
      },
    );
  }
}

/// Auth wrapper to check authentication status
class AuthWrapper extends ConsumerWidget {
  const AuthWrapper({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authStateProvider);

    if (authState.isLoading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    if (authState.isAuthenticated) {
      final user = authState.user;
      final role = UserRole.fromString(user?.role ?? 'traveler');

      // Route to dashboard based on role
      switch (role) {
        case UserRole.traveler:
          return const TravelerDashboardScreen();
        case UserRole.guide:
          return const GuideDashboardScreen();
        case UserRole.admin:
          return const GuideDashboardScreen(); // TODO: Create AdminDashboard
      }
    }

    return const LoginScreen();
  }
}
