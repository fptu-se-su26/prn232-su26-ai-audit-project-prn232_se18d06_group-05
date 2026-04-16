# 🔧 TripMate Troubleshooting Guide

> Giải pháp cho các vấn đề thường gặp khi phát triển TripMate

## 🚨 Vấn đề thường gặp

### 1. Build & Compilation Errors

#### ❌ Flutter build failed
```bash
# Error: Build failed with exception
```

**Giải pháp:**
```bash
# Clean và rebuild
flutter clean
flutter pub get
flutter run

# Nếu vẫn lỗi, xóa cache
rm -rf ~/.pub-cache
flutter pub get
```

#### ❌ Gradle build failed (Android)
```bash
# Error: Execution failed for task ':app:processDebugResources'
```

**Giải pháp:**
```bash
# Clean Android build
cd android
./gradlew clean
cd ..
flutter clean
flutter run
```

#### ❌ CocoaPods issues (iOS)
```bash
# Error: CocoaPods not installed
```

**Giải pháp:**
```bash
# Install CocoaPods
sudo gem install cocoapods

# Update pods
cd ios
pod install --repo-update
cd ..
flutter run
```

### 2. Database & Backend Issues

#### ❌ Supabase connection failed
```
Error: Could not connect to Supabase
```

**Kiểm tra:**
1. **Environment variables**:
   ```bash
   # Verify .env file exists and has correct values
   cat .env
   ```

2. **Network connectivity**:
   ```bash
   # Test Supabase URL
   curl https://nvbvvowyjzylllswhynv.supabase.co/rest/v1/
   ```

3. **API Key validity**:
   - Check Supabase dashboard
   - Regenerate if necessary

#### ❌ Table not found errors
```
Error: Could not find table 'conversations'
```

**Giải pháp:**
1. Run migration scripts in Supabase SQL Editor
2. Check [DATABASE_SETUP.md](DATABASE_SETUP.md)
3. Verify RLS policies

#### ❌ Authentication issues
```
Error: Invalid JWT token
```

**Giải pháp:**
```dart
// Clear stored tokens
await TokenStorage.clearTokens();

// Force re-login
await SupabaseConfig.client.auth.signOut();
```

### 3. Chat & Real-time Issues

#### ❌ Messages not appearing
```
Error: Real-time subscription failed
```

**Kiểm tra:**
1. **Database tables**:
   ```sql
   -- Verify tables exist
   SELECT * FROM conversations LIMIT 1;
   SELECT * FROM messages LIMIT 1;
   ```

2. **RLS Policies**:
   ```sql
   -- Check policies
   SELECT * FROM pg_policies WHERE tablename IN ('conversations', 'messages');
   ```

3. **Real-time enabled**:
   - Check Supabase dashboard → Settings → API
   - Ensure real-time is enabled for tables

#### ❌ Conversation creation failed
```
Error: Failed to create conversation
```

**Debug steps:**
```dart
// Check user authentication
final user = SupabaseConfig.client.auth.currentUser;
print('Current user: ${user?.id}');

// Check guide ID exists
final guide = await SupabaseConfig.client
    .from('profiles')
    .select()
    .eq('id', guideId)
    .single();
print('Guide exists: $guide');
```

### 4. Authentication & Authorization

#### ❌ Login failed
```
Error: Invalid login credentials
```

**Giải pháp:**
1. **Check email confirmation**:
   - Disable email confirmation in Supabase Auth settings
   - Or check email for confirmation link

2. **Password requirements**:
   - Minimum 8 characters
   - Check Supabase Auth policies

3. **Clear auth state**:
   ```dart
   await SupabaseConfig.client.auth.signOut();
   // Restart app
   ```

#### ❌ Role-based access not working
```
Error: User role not recognized
```

**Kiểm tra:**
```sql
-- Check user profile and role
SELECT u.email, p.role 
FROM auth.users u
LEFT JOIN profiles p ON u.id = p.id
WHERE u.email = 'your@email.com';
```

**Sửa role:**
```sql
UPDATE profiles 
SET role = 'guide' 
WHERE email = 'guide@test.com';
```

### 5. UI & Navigation Issues

#### ❌ Navigation not working
```
Error: Could not navigate to route
```

**Kiểm tra:**
1. **Route definitions** in `app_router.dart`
2. **Authentication state** for protected routes
3. **Context availability** when navigating

#### ❌ State not updating
```
Error: UI not reflecting data changes
```

**Debug Riverpod:**
```dart
// Add logging to providers
class MyNotifier extends StateNotifier<MyState> {
  MyNotifier() : super(MyState.initial()) {
    print('Provider initialized');
  }
  
  void updateState(MyState newState) {
    print('State changing from $state to $newState');
    state = newState;
  }
}
```

### 6. Performance Issues

#### ❌ App running slowly
```
Issue: Poor performance, lag
```

**Optimization:**
```dart
// Use const constructors
const MyWidget();

// Implement proper keys
ListView.builder(
  key: const ValueKey('tour-list'),
  // ...
);

// Optimize images
Image.network(
  url,
  cacheWidth: 300,
  cacheHeight: 200,
);
```

#### ❌ Memory leaks
```
Issue: Memory usage increasing
```

**Fix:**
```dart
// Dispose controllers
@override
void dispose() {
  _controller.dispose();
  _subscription?.cancel();
  super.dispose();
}

// Use AutoDispose providers
final myProvider = StateNotifierProvider.autoDispose<MyNotifier, MyState>(
  (ref) => MyNotifier(),
);
```

## 🛠️ Debug Tools & Commands

### Flutter Debugging
```bash
# Run with verbose logging
flutter run --verbose

# Enable performance overlay
flutter run --enable-software-rendering

# Profile mode
flutter run --profile

# Analyze code
flutter analyze

# Check dependencies
flutter pub deps
```

### Database Debugging
```sql
-- Check all tables
SELECT table_name FROM information_schema.tables 
WHERE table_schema = 'public';

-- Check RLS policies
SELECT schemaname, tablename, policyname, permissive, roles, cmd, qual 
FROM pg_policies;

-- Check user sessions
SELECT * FROM auth.sessions WHERE user_id = 'your-user-id';
```

### API Debugging
```bash
# Test API endpoints
curl -H "Authorization: Bearer YOUR_TOKEN" \
     https://localhost:5001/api/tours

# Check API logs
dotnet run --verbosity detailed

# Test database connection
curl https://localhost:5001/api/debug/db-test
```

## 📱 Platform-specific Issues

### Android
```bash
# Clear app data
adb shell pm clear com.example.tripmate

# Check logs
adb logcat | grep flutter

# Reinstall app
flutter clean
flutter run --uninstall-first
```

### iOS
```bash
# Clean iOS build
cd ios
rm -rf Pods Podfile.lock
pod install
cd ..
flutter clean
flutter run
```

### Web
```bash
# Clear browser cache
# Open DevTools → Application → Storage → Clear storage

# Run with CORS disabled (development only)
flutter run -d chrome --web-browser-flag "--disable-web-security"
```

## 🔍 Diagnostic Checklist

### Before Reporting Issues

- [ ] **Flutter Doctor**: Run `flutter doctor` and fix all issues
- [ ] **Dependencies**: Run `flutter pub get`
- [ ] **Clean Build**: Run `flutter clean && flutter pub get`
- [ ] **Environment**: Check `.env` file exists and is correct
- [ ] **Database**: Verify tables exist in Supabase
- [ ] **Authentication**: Test login/logout flow
- [ ] **Network**: Check internet connectivity
- [ ] **Logs**: Check console for error messages

### Information to Collect

When reporting issues, include:

1. **Flutter version**: `flutter --version`
2. **Platform**: Android/iOS/Web
3. **Error message**: Full error text
4. **Steps to reproduce**: Exact steps
5. **Expected behavior**: What should happen
6. **Actual behavior**: What actually happens
7. **Screenshots**: If UI-related
8. **Logs**: Console output

## 🆘 Getting Help

### Internal Resources
1. **Documentation**: Check all files in `/docs`
2. **Code Comments**: Look for inline documentation
3. **Git History**: Check commit messages for context

### External Resources
1. **Flutter Issues**: [GitHub Flutter Issues](https://github.com/flutter/flutter/issues)
2. **Supabase Docs**: [Supabase Documentation](https://supabase.com/docs)
3. **Stack Overflow**: Search for similar issues

### Contact Support
If issues persist:
1. Create detailed issue report
2. Include diagnostic information
3. Contact development team

---

**Last Updated**: December 2024  
**Maintained by**: TripMate Development Team