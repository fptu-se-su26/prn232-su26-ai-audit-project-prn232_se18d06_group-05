# 📦 Hướng Dẫn Supabase Storage - Upload Chứng Chỉ Hướng Dẫn Viên

> Tích hợp Supabase Storage để upload và quản lý chứng chỉ hướng dẫn viên

---

## 📋 Mục Lục

1. [Setup Supabase Storage](#setup-supabase-storage)
2. [Database Schema Update](#database-schema-update)
3. [Flutter Implementation](#flutter-implementation)
4. [ASP.NET Core Implementation](#aspnet-core-implementation)
5. [Security & Best Practices](#security--best-practices)

---

## 🗄️ Setup Supabase Storage

### Bước 1: Tạo Storage Bucket

1. **Đăng nhập Supabase Dashboard**
   ```
   https://supabase.com/dashboard
   ```

2. **Chọn Project TripMate**

3. **Vào Storage → Create Bucket**
   - **Name:** `guide-certificates`
   - **Public:** `false` (private bucket)
   - **File size limit:** `10MB`
   - **Allowed MIME types:** `application/pdf`

### Bước 2: Setup Storage Policies

Trong SQL Editor, chạy:

```sql
-- Policy: Guides can upload their own certificates
CREATE POLICY "Guides can upload own certificates"
ON storage.objects FOR INSERT
WITH CHECK (
  bucket_id = 'guide-certificates' AND
  auth.uid()::text = (storage.foldername(name))[1]
);

-- Policy: Guides can read their own certificates
CREATE POLICY "Guides can read own certificates"
ON storage.objects FOR SELECT
USING (
  bucket_id = 'guide-certificates' AND
  auth.uid()::text = (storage.foldername(name))[1]
);

-- Policy: Admins can read all certificates
CREATE POLICY "Admins can read all certificates"
ON storage.objects FOR SELECT
USING (
  bucket_id = 'guide-certificates' AND
  EXISTS (
    SELECT 1 FROM public.profiles
    WHERE id = auth.uid() AND role = 'admin'
  )
);

-- Policy: Guides can update their own certificates
CREATE POLICY "Guides can update own certificates"
ON storage.objects FOR UPDATE
USING (
  bucket_id = 'guide-certificates' AND
  auth.uid()::text = (storage.foldername(name))[1]
);

-- Policy: Guides can delete their own certificates
CREATE POLICY "Guides can delete own certificates"
ON storage.objects FOR DELETE
USING (
  bucket_id = 'guide-certificates' AND
  auth.uid()::text = (storage.foldername(name))[1]
);
```

---

## 🗃️ Database Schema Update

### Update profiles table

Thêm column `certificate_url` vào table `profiles`:

```sql
-- Add certificate_url column to profiles
ALTER TABLE public.profiles
ADD COLUMN IF NOT EXISTS certificate_url TEXT;

-- Add comment
COMMENT ON COLUMN public.profiles.certificate_url 
IS 'URL của chứng chỉ hướng dẫn viên trong Supabase Storage';
```


### File Structure trong Storage

```
guide-certificates/
├── {user_id}/
│   └── certificate.pdf
│   └── certificate_{timestamp}.pdf (nếu upload lại)
```

**Ví dụ:**
```
guide-certificates/
├── 123e4567-e89b-12d3-a456-426614174000/
│   └── certificate_2026-06-04_143520.pdf
```

---

## 📱 Flutter Implementation

### Bước 1: Thêm Dependencies

**File: `source/pubspec.yaml`**

```yaml
dependencies:
  # Existing dependencies
  supabase_flutter: ^2.0.0
  
  # File picker
  file_picker: ^6.1.1
  
  # Permissions (for mobile)
  permission_handler: ^11.1.0
  
  # Mime type detection
  mime: ^1.0.4
```

### Bước 2: Tạo Storage Service

**File: `source/lib/core/services/storage_service.dart`**

```dart
import 'dart:io';
import 'package:supabase_flutter/supabase_flutter.dart';

class StorageService {
  final SupabaseClient _supabase = Supabase.instance.client;
  final String _bucketName = 'guide-certificates';

  /// Upload certificate file
  Future<String> uploadCertificate({
    required File file,
    required String userId,
  }) async {
    try {
      // Generate unique filename
      final timestamp = DateTime.now().millisecondsSinceEpoch;
      final fileName = 'certificate_$timestamp.pdf';
      final filePath = '$userId/$fileName';


      // Upload to Supabase Storage
      await _supabase.storage
          .from(_bucketName)
          .upload(filePath, file);

      // Get public URL (signed URL for private bucket)
      final publicUrl = _supabase.storage
          .from(_bucketName)
          .getPublicUrl(filePath);

      return publicUrl;
    } catch (e) {
      throw Exception('Failed to upload certificate: $e');
    }
  }

  /// Get signed URL (for private access)
  Future<String> getSignedUrl({
    required String filePath,
    int expiresIn = 3600, // 1 hour default
  }) async {
    try {
      final signedUrl = await _supabase.storage
          .from(_bucketName)
          .createSignedUrl(filePath, expiresIn);

      return signedUrl;
    } catch (e) {
      throw Exception('Failed to get signed URL: $e');
    }
  }

  /// Delete certificate
  Future<void> deleteCertificate({
    required String filePath,
  }) async {
    try {
      await _supabase.storage
          .from(_bucketName)
          .remove([filePath]);
    } catch (e) {
      throw Exception('Failed to delete certificate: $e');
    }
  }

  /// Download certificate
  Future<List<int>> downloadCertificate({
    required String filePath,
  }) async {
    try {
      final bytes = await _supabase.storage
          .from(_bucketName)
          .download(filePath);

      return bytes;
    } catch (e) {
      throw Exception('Failed to download certificate: $e');
    }
  }
}
```


### Bước 3: Update Auth Service

**File: `source/lib/features/auth/data/datasources/auth_remote_datasource.dart`**

```dart
import 'dart:io';
import 'package:file_picker/file_picker.dart';
import '../../../core/services/storage_service.dart';

class AuthRemoteDataSource {
  final SupabaseClient _supabase;
  final StorageService _storageService = StorageService();

  // ... existing code ...

  /// Register guide with certificate upload
  Future<User> registerGuide({
    required String email,
    required String password,
    required String fullName,
    required String phoneNumber,
    required String experience,
    required String specialization,
    required String languages,
    required String bio,
    required File? certificateFile,
  }) async {
    try {
      // 1. Create auth user
      final authResponse = await _supabase.auth.signUp(
        email: email,
        password: password,
      );

      if (authResponse.user == null) {
        throw Exception('Failed to create user');
      }

      final userId = authResponse.user!.id;
      String? certificateUrl;

      // 2. Upload certificate if provided
      if (certificateFile != null) {
        certificateUrl = await _storageService.uploadCertificate(
          file: certificateFile,
          userId: userId,
        );
      }

      // 3. Create profile with certificate URL
      await _supabase.from('profiles').insert({
        'id': userId,
        'email': email,
        'full_name': fullName,
        'phone_number': phoneNumber,
        'role': 'guide',
        'experience': experience,
        'specialization': specialization,
        'languages': languages,
        'bio': bio,
        'certificate_url': certificateUrl,
        'status': 'pending', // Admin needs to approve
      });

      return authResponse.user!;
    } catch (e) {
      throw Exception('Registration failed: $e');
    }
  }
}
```


### Bước 4: UI Implementation - Register Screen

**File: `source/lib/features/auth/presentation/screens/register_screen.dart`**

```dart
import 'dart:io';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';

class RegisterScreen extends StatefulWidget {
  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  File? _certificateFile;
  String? _certificateFileName;
  int? _certificateFileSize;
  
  // ... other controllers ...

  /// Pick certificate file
  Future<void> _pickCertificate() async {
    try {
      final result = await FilePicker.platform.pickFiles(
        type: FileType.custom,
        allowedExtensions: ['pdf'],
        allowMultiple: false,
      );

      if (result != null && result.files.isNotEmpty) {
        final file = File(result.files.single.path!);
        
        // Validate file size (10MB max)
        final fileSize = await file.length();
        if (fileSize > 10 * 1024 * 1024) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('File quá lớn! Tối đa 10MB')),
          );
          return;
        }

        setState(() {
          _certificateFile = file;
          _certificateFileName = result.files.single.name;
          _certificateFileSize = fileSize;
        });
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Lỗi khi chọn file: $e')),
      );
    }
  }

  /// Remove certificate
  void _removeCertificate() {
    setState(() {
      _certificateFile = null;
      _certificateFileName = null;
      _certificateFileSize = null;
    });
  }

  /// Format file size
  String _formatFileSize(int bytes) {
    if (bytes < 1024) return '$bytes B';
    if (bytes < 1024 * 1024) return '${(bytes / 1024).toStringAsFixed(1)} KB';
    return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      // ... existing UI ...
      
      // Certificate upload widget
      if (_selectedRole == 'guide')
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'CHỨNG CHỈ HƯỚNG DẪN VIÊN',
              style: Theme.of(context).textTheme.labelSmall?.copyWith(
                fontWeight: FontWeight.bold,
              ),
            ),
            SizedBox(height: 8),
            
            // Upload area
            if (_certificateFile == null)
              InkWell(
                onTap: _pickCertificate,
                child: Container(
                  padding: EdgeInsets.all(24),
                  decoration: BoxDecoration(
                    border: Border.all(
                      color: Theme.of(context).dividerColor,
                      width: 2,
                      style: BorderStyle.solid,
                    ),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Column(
                    children: [
                      Icon(
                        Icons.cloud_upload_outlined,
                        size: 48,
                        color: Theme.of(context).colorScheme.primary,
                      ),
                      SizedBox(height: 12),
                      Text(
                        'Nhấp để tải lên chứng chỉ',
                        style: TextStyle(fontWeight: FontWeight.bold),
                      ),
                      SizedBox(height: 4),
                      Text(
                        'PDF, tối đa 10MB',
                        style: TextStyle(
                          fontSize: 12,
                          color: Colors.grey,
                        ),
                      ),
                    ],
                  ),
                ),
              )
            
            // File preview
            else
              Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Theme.of(context).colorScheme.primaryContainer,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Column(
                  children: [
                    Row(
                      children: [
                        Icon(
                          Icons.picture_as_pdf,
                          color: Colors.red,
                          size: 32,
                        ),
                        SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                _certificateFileName!,
                                style: TextStyle(fontWeight: FontWeight.bold),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                              Text(
                                _formatFileSize(_certificateFileSize!),
                                style: TextStyle(
                                  fontSize: 12,
                                  color: Colors.grey,
                                ),
                              ),
                            ],
                          ),
                        ),
                        IconButton(
                          icon: Icon(Icons.close),
                          onPressed: _removeCertificate,
                        ),
                      ],
                    ),
                  ],
                ),
              ),
          ],
        ),
    );
  }
}
```


---

## 🌐 ASP.NET Core Implementation

### Bước 1: Install NuGet Package

```bash
cd source/web/TripMate_Webapi/TripMate_Webapi
dotnet add package Supabase.Storage
```

### Bước 2: Create Storage Service

**File: `Services/SupabaseStorageService.cs`**

```csharp
using Supabase;
using Supabase.Storage;

namespace TripMate_Webapi.Services
{
    public class SupabaseStorageService
    {
        private readonly Client _supabase;
        private const string BucketName = "guide-certificates";

        public SupabaseStorageService(Client supabase)
        {
            _supabase = supabase;
        }

        /// <summary>
        /// Upload certificate from IFormFile
        /// </summary>
        public async Task<string> UploadCertificateAsync(
            IFormFile file, 
            string userId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            // Validate file type
            if (!file.ContentType.Equals("application/pdf", 
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Only PDF files are allowed");
            }

            // Validate file size (10MB max)
            const long maxFileSize = 10 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                throw new ArgumentException("File size exceeds 10MB limit");
            }

            try
            {
                // Generate unique filename
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var fileName = $"certificate_{timestamp}.pdf";
                var filePath = $"{userId}/{fileName}";

                // Read file to byte array
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                // Upload to Supabase Storage
                await _supabase.Storage
                    .From(BucketName)
                    .Upload(fileBytes, filePath);

                // Get public URL
                var publicUrl = _supabase.Storage
                    .From(BucketName)
                    .GetPublicUrl(filePath);

                return publicUrl;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload certificate: {ex.Message}");
            }
        }


        /// <summary>
        /// Get signed URL for private access
        /// </summary>
        public async Task<string> GetSignedUrlAsync(
            string filePath, 
            int expiresIn = 3600)
        {
            try
            {
                var signedUrl = await _supabase.Storage
                    .From(BucketName)
                    .CreateSignedUrl(filePath, expiresIn);

                return signedUrl;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get signed URL: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete certificate
        /// </summary>
        public async Task DeleteCertificateAsync(string filePath)
        {
            try
            {
                await _supabase.Storage
                    .From(BucketName)
                    .Remove(new List<string> { filePath });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete certificate: {ex.Message}");
            }
        }

        /// <summary>
        /// Download certificate as byte array
        /// </summary>
        public async Task<byte[]> DownloadCertificateAsync(string filePath)
        {
            try
            {
                var bytes = await _supabase.Storage
                    .From(BucketName)
                    .Download(filePath);

                return bytes;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download certificate: {ex.Message}");
            }
        }
    }
}
```

### Bước 3: Register Service

**File: `Program.cs`**

```csharp
// Add Storage Service
builder.Services.AddScoped<SupabaseStorageService>();
```


### Bước 4: Update Register Action

**File: `Controllers/AuthController.cs`**

```csharp
[HttpPost("register")]
public async Task<IActionResult> Register(
    [FromForm] RegisterGuideViewModel model)
{
    try
    {
        // 1. Validate model
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // 2. Create auth user
        var authResponse = await _authService.SignUpAsync(
            model.Email, 
            model.Password);

        if (authResponse?.User == null)
            return BadRequest("Failed to create user");

        var userId = authResponse.User.Id;
        string? certificateUrl = null;

        // 3. Upload certificate if provided
        if (model.CertificateFile != null)
        {
            certificateUrl = await _storageService.UploadCertificateAsync(
                model.CertificateFile, 
                userId);
        }

        // 4. Create profile
        await _supabase.From<Profile>().Insert(new Profile
        {
            Id = userId,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            Role = "guide",
            Experience = model.Experience,
            Specialization = model.Specialization,
            Languages = model.Languages,
            Bio = model.Bio,
            CertificateUrl = certificateUrl,
            Status = "pending" // Admin needs to approve
        });

        return Ok(new
        {
            message = "Registration successful. Awaiting admin approval.",
            userId
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = ex.Message });
    }
}

// ViewModel
public class RegisterGuideViewModel
{
    [Required]
    public string Email { get; set; }

    [Required]
    [MinLength(6)]
    public string Password { get; set; }

    [Required]
    public string FullName { get; set; }

    [Required]
    [Phone]
    public string PhoneNumber { get; set; }

    public string? Experience { get; set; }
    public string? Specialization { get; set; }
    public string? Languages { get; set; }
    public string? Bio { get; set; }

    [AllowedExtensions(new[] { ".pdf" })]
    [MaxFileSize(10 * 1024 * 1024)] // 10MB
    public IFormFile? CertificateFile { get; set; }
}
```


### Bước 5: Custom Validation Attributes

**File: `Attributes/FileValidationAttributes.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace TripMate_Webapi.Attributes
{
    /// <summary>
    /// Validates allowed file extensions
    /// </summary>
    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;

        public AllowedExtensionsAttribute(string[] extensions)
        {
            _extensions = extensions;
        }

        protected override ValidationResult? IsValid(
            object? value, 
            ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!_extensions.Contains(extension))
                {
                    return new ValidationResult(
                        $"Only {string.Join(", ", _extensions)} files are allowed");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates maximum file size
    /// </summary>
    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly long _maxFileSize;

        public MaxFileSizeAttribute(long maxFileSize)
        {
            _maxFileSize = maxFileSize;
        }

        protected override ValidationResult? IsValid(
            object? value, 
            ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                if (file.Length > _maxFileSize)
                {
                    return new ValidationResult(
                        $"Maximum allowed file size is {_maxFileSize / (1024 * 1024)}MB");
                }
            }

            return ValidationResult.Success;
        }
    }
}
```


### Bước 6: Update Register.cshtml JavaScript

**File: `Views/Auth/Register.cshtml` (script section)**

```javascript
// Handle form submission
document.getElementById('registerForm').addEventListener('submit', async function(e) {
    e.preventDefault();
    
    // Validate passwords match
    const password = document.getElementById('password').value;
    const confirmPassword = document.getElementById('confirmPassword').value;
    
    if (password !== confirmPassword) {
        showError('Mật khẩu xác nhận không khớp!');
        return;
    }
    
    // Get role
    const role = document.querySelector('input[name="role"]:checked').value;
    
    // Prepare form data
    const formData = new FormData();
    formData.append('Email', document.getElementById('email').value);
    formData.append('Password', password);
    formData.append('FullName', document.getElementById('fullName').value);
    formData.append('PhoneNumber', document.getElementById('phoneNumber').value);
    
    // Add guide-specific fields
    if (role === 'guide') {
        formData.append('Experience', document.getElementById('experience').value);
        formData.append('Specialization', document.getElementById('specialization').value);
        formData.append('Languages', document.getElementById('languages').value);
        formData.append('Bio', document.getElementById('bio').value);
        
        // Add certificate file
        const certificateFile = document.getElementById('certificateFile').files[0];
        if (certificateFile) {
            formData.append('CertificateFile', certificateFile);
        }
    }
    
    showSpinner();
    
    try {
        const response = await fetch('/Auth/Register', {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        
        hideSpinner();
        
        if (response.ok) {
            // Success - redirect to login
            alert('Đăng ký thành công! ' + result.message);
            window.location.href = '/Auth/Login';
        } else {
            // Error
            showError(result.error || 'Đăng ký thất bại!');
        }
    } catch (error) {
        hideSpinner();
        showError('Lỗi kết nối: ' + error.message);
    }
});
```


---

## 🔒 Security & Best Practices

### 1. File Validation

**Client-side:**
```dart
// Flutter
bool isValidPDF(File file) {
  return file.path.toLowerCase().endsWith('.pdf') &&
         file.lengthSync() <= 10 * 1024 * 1024;
}
```

**Server-side:**
```csharp
// ASP.NET Core
private bool IsValidCertificate(IFormFile file)
{
    // Check content type
    if (!file.ContentType.Equals("application/pdf"))
        return false;
    
    // Check file extension
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (extension != ".pdf")
        return false;
    
    // Check file size
    if (file.Length > 10 * 1024 * 1024)
        return false;
    
    // Check magic number (PDF signature)
    using (var stream = file.OpenReadStream())
    {
        var buffer = new byte[5];
        stream.Read(buffer, 0, 5);
        
        // PDF starts with %PDF-
        return buffer[0] == 0x25 && buffer[1] == 0x50 && 
               buffer[2] == 0x44 && buffer[3] == 0x46 && 
               buffer[4] == 0x2D;
    }
}
```

### 2. Storage Policies Best Practices

```sql
-- ✅ GOOD: User-scoped access
CREATE POLICY "Users can only access their own files"
ON storage.objects FOR ALL
USING (auth.uid()::text = (storage.foldername(name))[1]);

-- ❌ BAD: Public access to private data
CREATE POLICY "Anyone can access"
ON storage.objects FOR SELECT
USING (true);
```

### 3. File Naming Convention

```dart
// ✅ GOOD: Unique, timestamped filename
final timestamp = DateTime.now().millisecondsSinceEpoch;
final fileName = 'certificate_$timestamp.pdf';
final filePath = '$userId/$fileName';

// ❌ BAD: Predictable filename (security risk)
final filePath = '$userId/certificate.pdf';
```

### 4. Error Handling

```dart
Future<String?> uploadCertificateSafely(File file, String userId) async {
  try {
    return await _storageService.uploadCertificate(
      file: file,
      userId: userId,
    );
  } on StorageException catch (e) {
    print('Storage error: ${e.message}');
    return null;
  } on AuthException catch (e) {
    print('Auth error: ${e.message}');
    return null;
  } catch (e) {
    print('Unknown error: $e');
    return null;
  }
}
```

### 5. Cleanup Old Files

```dart
// Delete old certificate before uploading new one
Future<void> replaceCertificate({
  required File newFile,
  required String userId,
  required String? oldFilePath,
}) async {
  // Upload new file first
  final newUrl = await _storageService.uploadCertificate(
    file: newFile,
    userId: userId,
  );
  
  // Delete old file if exists
  if (oldFilePath != null) {
    try {
      await _storageService.deleteCertificate(filePath: oldFilePath);
    } catch (e) {
      print('Failed to delete old file: $e');
      // Non-critical error, continue
    }
  }
  
  return newUrl;
}
```


### 6. Progress Tracking (Optional)

```dart
// Flutter with progress tracking
Future<String> uploadWithProgress({
  required File file,
  required String userId,
  required Function(double) onProgress,
}) async {
  final fileBytes = await file.readAsBytes();
  final timestamp = DateTime.now().millisecondsSinceEpoch;
  final fileName = 'certificate_$timestamp.pdf';
  final filePath = '$userId/$fileName';
  
  // Upload with progress callback
  final response = await _supabase.storage
      .from('guide-certificates')
      .uploadBinary(
        filePath,
        fileBytes,
        fileOptions: FileOptions(
          upsert: false,
        ),
        onUploadProgress: (progress) {
          final percentage = (progress / fileBytes.length) * 100;
          onProgress(percentage);
        },
      );
  
  return _supabase.storage
      .from('guide-certificates')
      .getPublicUrl(filePath);
}
```

---

## 📊 Testing

### 1. Test Storage Policies

```sql
-- Test as guide user
SELECT auth.uid(); -- Should return guide's user ID

-- Try to upload (should succeed)
SELECT storage.upload(
  'guide-certificates',
  '{user_id}/test.pdf',
  'test content'::bytea
);

-- Try to access other user's file (should fail)
SELECT storage.download(
  'guide-certificates',
  '{other_user_id}/certificate.pdf'
);
```

### 2. Flutter Integration Test

```dart
void main() {
  group('Certificate Upload Tests', () {
    test('Upload valid PDF certificate', () async {
      final storageService = StorageService();
      final testFile = File('test/assets/sample_certificate.pdf');
      final userId = 'test-user-id';
      
      final url = await storageService.uploadCertificate(
        file: testFile,
        userId: userId,
      );
      
      expect(url, isNotNull);
      expect(url, contains('guide-certificates'));
      expect(url, contains(userId));
    });
    
    test('Reject non-PDF file', () async {
      final storageService = StorageService();
      final testFile = File('test/assets/invalid.txt');
      final userId = 'test-user-id';
      
      expect(
        () => storageService.uploadCertificate(
          file: testFile,
          userId: userId,
        ),
        throwsException,
      );
    });
  });
}
```

### 3. ASP.NET Core Unit Test

```csharp
[Fact]
public async Task UploadCertificate_ValidPDF_ReturnsUrl()
{
    // Arrange
    var mockFile = CreateMockPdfFile();
    var userId = "test-user-id";
    
    // Act
    var url = await _storageService.UploadCertificateAsync(mockFile, userId);
    
    // Assert
    Assert.NotNull(url);
    Assert.Contains("guide-certificates", url);
    Assert.Contains(userId, url);
}

[Fact]
public async Task UploadCertificate_InvalidFileType_ThrowsException()
{
    // Arrange
    var mockFile = CreateMockTextFile();
    var userId = "test-user-id";
    
    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(
        () => _storageService.UploadCertificateAsync(mockFile, userId)
    );
}
```

---

## 🐛 Troubleshooting

### Issue 1: "Storage bucket not found"

**Solution:**
```sql
-- Check if bucket exists
SELECT * FROM storage.buckets WHERE name = 'guide-certificates';

-- Create if not exists
INSERT INTO storage.buckets (id, name, public)
VALUES ('guide-certificates', 'guide-certificates', false);
```

### Issue 2: "Permission denied" when uploading

**Solution:**
```sql
-- Check current user
SELECT auth.uid();

-- Verify policies
SELECT * FROM pg_policies WHERE tablename = 'objects';

-- Re-create upload policy
DROP POLICY IF EXISTS "Guides can upload own certificates" ON storage.objects;
CREATE POLICY "Guides can upload own certificates"
ON storage.objects FOR INSERT
WITH CHECK (
  bucket_id = 'guide-certificates' AND
  auth.uid()::text = (storage.foldername(name))[1]
);
```

### Issue 3: File upload hangs in Flutter

**Solution:**
```dart
// Add timeout to upload
await _supabase.storage
    .from('guide-certificates')
    .upload(filePath, file)
    .timeout(Duration(seconds: 30));
```

### Issue 4: Cannot download file (404)

**Solution:**
```dart
// Use signed URL for private buckets
final signedUrl = await _supabase.storage
    .from('guide-certificates')
    .createSignedUrl(filePath, 3600); // 1 hour expiry

// Use this URL to download
```

---

## 📚 Resources

- [Supabase Storage Docs](https://supabase.com/docs/guides/storage)
- [Flutter File Picker](https://pub.dev/packages/file_picker)
- [ASP.NET Core File Upload](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads)

---

**Tài liệu này được tạo:** June 4, 2026  
**Phiên bản:** 1.0  
**Tác giả:** TripMate Development Team
