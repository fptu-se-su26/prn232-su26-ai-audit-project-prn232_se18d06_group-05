# 📖 TripMate Setup Guide

> Hướng dẫn chi tiết cài đặt và cấu hình TripMate

## 🎯 Tổng quan

Hướng dẫn này sẽ giúp bạn:
- Cài đặt môi trường development
- Cấu hình database và backend
- Chạy ứng dụng trên các platform
- Tạo tài khoản test

## 📋 Yêu cầu hệ thống

### Flutter Development
- **Flutter SDK**: >= 3.11.3
- **Dart SDK**: >= 3.11.3
- **Android Studio** hoặc **VS Code**
- **Git**: Latest version

### Backend Development (Optional)
- **.NET SDK**: >= 7.0
- **Visual Studio** hoặc **VS Code**

### Database
- **Supabase Account**: [supabase.com](https://supabase.com)

## 🚀 Cài đặt từng bước

### Bước 1: Clone Repository

```bash
git clone <repository-url>
cd flutter_tripmate_application
```

### Bước 2: Cài đặt Flutter Dependencies

```bash
flutter pub get
```

### Bước 3: Cấu hình Environment

```bash
# Copy environment template
cp .env.example .env
```

Chỉnh sửa file `.env`:
```env
SUPABASE_URL=https://nvbvvowyjzylllswhynv.supabase.co
SUPABASE_ANON_KEY=your_anon_key_here
```

### Bước 4: Database Setup

1. **Truy cập Supabase Dashboard**
   - Mở [Supabase Dashboard](https://supabase.com/dashboard)
   - Chọn project TripMate

2. **Chạy Migration Scripts**
   ```sql
   -- Trong SQL Editor, chạy lần lượt:
   -- 1. web/TripMate_Webapi/supabase_full_schema.sql
   -- 2. web/TripMate_Webapi/migrations/003_chat_tables.sql
   ```

3. **Verify Tables**
   ```sql
   -- Kiểm tra tables đã được tạo
   SELECT table_name FROM information_schema.tables 
   WHERE table_schema = 'public';
   ```

### Bước 5: Tạo Test Accounts

Chạy app và đăng ký 3 tài khoản:

```bash
flutter run
```

**Tài khoản test:**
- **Traveler**: `traveler@test.com` / `traveler123`
- **Guide**: `guide@test.com` / `guide123`  
- **Admin**: `admin@test.com` / `admin123`

Sau khi đăng ký, update roles trong Supabase:
```sql
-- Update roles
UPDATE profiles SET role = 'guide' WHERE email = 'guide@test.com';
UPDATE profiles SET role = 'admin' WHERE email = 'admin@test.com';
```

### Bước 6: Tạo Sample Data

```sql
-- Tạo sample tours cho guide
DO $
DECLARE
  v_guide_id UUID;
BEGIN
  SELECT id INTO v_guide_id FROM profiles WHERE email = 'guide@test.com';
  
  INSERT INTO tours (guide_id, title, description, location, price, duration_hours, max_participants, images, status)
  VALUES 
    (v_guide_id, 'Khám phá Hà Nội Phố Cổ', 'Tour tham quan khu phố cổ Hà Nội', 'Hà Nội', 500000, 4, 15, ARRAY['https://images.unsplash.com/photo-1555400038-63f5ba517a47?w=800'], 'active'),
    (v_guide_id, 'Vịnh Hạ Long 1 ngày', 'Khám phá kỳ quan thiên nhiên', 'Quảng Ninh', 1500000, 8, 20, ARRAY['https://images.unsplash.com/photo-1528127269322-539801943592?w=800'], 'active'),
    (v_guide_id, 'Sài Gòn về đêm', 'Trải nghiệm Sài Gòn về đêm', 'Hồ Chí Minh', 300000, 3, 10, ARRAY['https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800'], 'active');
END $;
```

## 🖥️ Chạy ứng dụng

### Mobile (Android/iOS)
```bash
# Xem danh sách devices
flutter devices

# Chạy trên device cụ thể
flutter run -d <device-id>

# Chạy debug mode
flutter run --debug

# Chạy release mode
flutter run --release
```

### Web
```bash
# Chạy trên Chrome
flutter run -d chrome

# Chạy với port cụ thể
flutter run -d web-server --web-port 8080
```

### ASP.NET API (Optional)
```bash
cd web/TripMate_Webapi
dotnet run

# API sẽ chạy tại: https://localhost:5001
```

## 🧪 Kiểm tra cài đặt

### 1. Test Authentication
- Đăng nhập với các tài khoản test
- Verify role-based navigation
- Check session persistence

### 2. Test Tour Features
- Browse tour list
- View tour details
- Search and filter tours

### 3. Test Booking Flow
- Book a tour as traveler
- View booking confirmation
- Check booking history

### 4. Test Chat Feature
- Start conversation from booking confirmation
- Send messages between traveler and guide
- Verify real-time updates

## 🔧 Development Tools

### Code Generation
```bash
# Generate code for models
flutter pub run build_runner build --delete-conflicting-outputs

# Watch for changes
flutter pub run build_runner watch
```

### Debugging
```bash
# Enable verbose logging
flutter run --verbose

# Debug specific platform
flutter run -d android --debug
flutter run -d ios --debug
```

### Testing
```bash
# Run all tests
flutter test

# Run specific test file
flutter test test/features/auth/auth_test.dart

# Run with coverage
flutter test --coverage
```

## 📱 Platform-specific Setup

### Android
1. **Android Studio**: Install latest version
2. **SDK**: API level 21+ (Android 5.0+)
3. **Emulator**: Create AVD with API 30+

### iOS
1. **Xcode**: Version 14+
2. **iOS Simulator**: iOS 12+
3. **CocoaPods**: `sudo gem install cocoapods`

### Web
1. **Chrome**: Latest version for development
2. **CORS**: Handled by Supabase configuration

## 🐛 Troubleshooting

### Common Issues

#### 1. Build Errors
```bash
# Clean and rebuild
flutter clean
flutter pub get
flutter run
```

#### 2. Database Connection Issues
- Verify Supabase URL and API key
- Check network connectivity
- Ensure RLS policies are correct

#### 3. Authentication Problems
- Clear app data/cache
- Check email confirmation settings
- Verify user roles in database

#### 4. Chat Not Working
- Ensure chat tables are created
- Check real-time subscriptions
- Verify conversation creation

### Debug Commands
```bash
# Check Flutter doctor
flutter doctor

# Analyze code
flutter analyze

# Check dependencies
flutter pub deps
```

## 📊 Performance Optimization

### Development
- Use `flutter run --profile` for performance testing
- Enable performance overlay: `flutter run --enable-software-rendering`
- Monitor memory usage in DevTools

### Production
- Build with `--release` flag
- Enable code obfuscation
- Optimize images and assets

## 🔐 Security Checklist

- [ ] Environment variables not committed
- [ ] API keys properly secured
- [ ] HTTPS enabled for all communications
- [ ] Input validation implemented
- [ ] RLS policies configured
- [ ] Authentication flows tested

## 📚 Next Steps

After successful setup:

1. **Explore Features**: Test all user flows
2. **Customize**: Modify colors, themes, content
3. **Extend**: Add new features or integrations
4. **Deploy**: Prepare for production deployment

## 🆘 Getting Help

If you encounter issues:

1. **Check Documentation**: Review all docs in `/docs` folder
2. **Common Issues**: See [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
3. **Database Issues**: See [DATABASE_SETUP.md](DATABASE_SETUP.md)
4. **API Issues**: See [API_GUIDE.md](API_GUIDE.md)

---

**Setup Time**: ~30 minutes  
**Difficulty**: Intermediate  
**Last Updated**: December 2024