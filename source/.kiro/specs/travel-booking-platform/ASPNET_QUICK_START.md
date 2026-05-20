# 🚀 ASP.NET Core Quick Start

## 📋 Checklist Setup

### Bước 1: Tạo ASP.NET Core Project (5 phút)

```bash
# Tạo project
dotnet new webapi -n TripMate.API
cd TripMate.API

# Install packages
dotnet add package supabase-csharp
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

### Bước 2: Configure Supabase (2 phút)

**appsettings.json:**
```json
{
  "Supabase": {
    "Url": "https://nvbvvowyjzylllswhynv.supabase.co",
    "Key": "YOUR_ANON_KEY"
  }
}
```

**Program.cs:**
```csharp
builder.Services.AddSingleton<ISupabaseService, SupabaseService>();
```

### Bước 3: Tạo Controllers (10 phút)

```csharp
// Controllers/ToursController.cs
[ApiController]
[Route("api/[controller]")]
public class ToursController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTours()
    {
        // Your logic here
        return Ok(tours);
    }
}
```

### Bước 4: Run API (1 phút)

```bash
dotnet run
# API chạy tại: https://localhost:5001
```

### Bước 5: Update Flutter App (5 phút)

**Install Dio:**
```yaml
dependencies:
  dio: ^5.4.0
```

**Create API Service:**
```dart
class ApiService {
  final dio = Dio(BaseOptions(
    baseUrl: 'https://localhost:5001',
  ));
  
  Future<List<Tour>> getTours() async {
    final response = await dio.get('/api/tours');
    return (response.data as List)
        .map((json) => Tour.fromJson(json))
        .toList();
  }
}
```

### Bước 6: Test (2 phút)

```bash
# Test API
curl https://localhost:5001/api/tours

# Run Flutter app
flutter run
```

## ⚡ Quick Commands

```bash
# Create project
dotnet new webapi -n TripMate.API

# Add packages
dotnet add package supabase-csharp

# Run
dotnet run

# Build
dotnet build

# Publish
dotnet publish -c Release

# Watch (auto-reload)
dotnet watch run
```

## 🔧 Common Issues

### Issue 1: CORS Error
```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("AllowAll");
```

### Issue 2: HTTPS Certificate
```bash
# Trust dev certificate
dotnet dev-certs https --trust
```

### Issue 3: Port already in use
```json
// Properties/launchSettings.json
"applicationUrl": "https://localhost:5002;http://localhost:5001"
```

## 📊 Architecture Flow

```
1. Flutter App
   ↓ HTTP Request
2. ASP.NET API
   ↓ Supabase Client
3. Supabase DB
   ↓ Response
4. ASP.NET API
   ↓ JSON Response
5. Flutter App
```

## 🎯 Next Steps

1. ✅ Setup basic API
2. ✅ Connect to Supabase
3. ✅ Create endpoints
4. ✅ Update Flutter app
5. ⬜ Add authentication
6. ⬜ Add validation
7. ⬜ Add logging
8. ⬜ Deploy to Azure

## 📚 Useful Links

- [ASP.NET Tutorial](https://docs.microsoft.com/aspnet/core/tutorials/first-web-api)
- [Supabase C#](https://github.com/supabase-community/supabase-csharp)
- [Dio Package](https://pub.dev/packages/dio)

Total setup time: ~25 phút! 🎉
