# ⚠️ Lỗi: Đường dẫn có khoảng trắng

## 🐛 Vấn đề

Project hiện tại đang ở đường dẫn:
```
E:\My Summer 2026 Documents\flutter_tripmate_application
```

Đường dẫn có khoảng trắng (`My Summer 2026 Documents`) gây lỗi khi build native assets:
```
'E:\My' is not recognized as an internal or external command
```

## ✅ Giải pháp

### Option 1: Di chuyển project (Khuyến nghị)

1. **Tạo thư mục mới không có khoảng trắng:**
   ```bash
   mkdir E:\Projects
   ```

2. **Di chuyển project:**
   ```bash
   # Copy toàn bộ project
   xcopy "E:\My Summer 2026 Documents\flutter_tripmate_application" "E:\Projects\flutter_tripmate_application" /E /I /H
   
   # Hoặc dùng File Explorer để copy/paste
   ```

3. **Mở project mới trong VS Code:**
   ```bash
   cd E:\Projects\flutter_tripmate_application
   code .
   ```

4. **Clean và rebuild:**
   ```bash
   flutter clean
   flutter pub get
   flutter run
   ```

### Option 2: Sử dụng subst (Tạm thời)

Tạo virtual drive không có khoảng trắng:

```bash
# Tạo drive T: trỏ đến project
subst T: "E:\My Summer 2026 Documents\flutter_tripmate_application"

# Chuyển đến drive T:
T:
cd \

# Run flutter
flutter run
```

Để xóa virtual drive:
```bash
subst T: /D
```

### Option 3: Rename thư mục cha

Đổi tên thư mục:
```
E:\My Summer 2026 Documents  →  E:\MyDocuments
```

Sau đó:
```bash
cd E:\MyDocuments\flutter_tripmate_application
flutter clean
flutter pub get
flutter run
```

## 🚀 Sau khi fix

1. **Clean project:**
   ```bash
   flutter clean
   ```

2. **Get dependencies:**
   ```bash
   flutter pub get
   ```

3. **Run app:**
   ```bash
   # Web
   flutter run -d chrome
   
   # Windows
   flutter run -d windows
   ```

## 📝 Lưu ý

- Đường dẫn project không nên có:
  - Khoảng trắng
  - Ký tự đặc biệt
  - Ký tự Unicode (tiếng Việt)
  
- Đường dẫn tốt:
  - `E:\Projects\flutter_tripmate`
  - `C:\Dev\tripmate`
  - `D:\flutter_apps\tripmate`

## ✅ Checklist

- [ ] Di chuyển project đến đường dẫn không có khoảng trắng
- [ ] Run `flutter clean`
- [ ] Run `flutter pub get`
- [ ] Test run `flutter run -d chrome`
- [ ] Verify app chạy thành công

---

**Sau khi fix xong, app sẽ chạy bình thường!**
