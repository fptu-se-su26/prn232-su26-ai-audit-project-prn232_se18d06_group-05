import 'package:flutter_dotenv/flutter_dotenv.dart';
import '../utils/logger.dart';
import 'supabase_config.dart';

/// Test Supabase connection
Future<void> testSupabaseConnection() async {
  try {
    Logger.info('=== Testing Supabase Connection ===');

    // Check environment variables
    Logger.info('Checking environment variables...');
    final url = dotenv.env['SUPABASE_URL'];
    final key = dotenv.env['SUPABASE_ANON_KEY'];

    if (url == null || url.isEmpty) {
      Logger.error('SUPABASE_URL not found in .env');
      return;
    }

    if (key == null || key.isEmpty) {
      Logger.error('SUPABASE_ANON_KEY not found in .env');
      return;
    }

    Logger.success('Environment variables loaded');
    Logger.info('URL: $url');
    Logger.info('Key: ${key.substring(0, 20)}...');

    // Check Supabase initialization
    if (!SupabaseConfig.isInitialized) {
      Logger.error('Supabase not initialized');
      return;
    }

    Logger.success('Supabase client initialized');

    // Test connection by checking auth state
    final client = SupabaseConfig.client;
    final session = client.auth.currentSession;

    if (session == null) {
      Logger.info('No active session (user not logged in)');
    } else {
      Logger.success('Active session found');
      Logger.info('User ID: ${session.user.id}');
    }

    Logger.success('=== Supabase Connection Test Passed ===');
  } catch (e, stackTrace) {
    Logger.error('Supabase connection test failed', e, stackTrace);
  }
}
