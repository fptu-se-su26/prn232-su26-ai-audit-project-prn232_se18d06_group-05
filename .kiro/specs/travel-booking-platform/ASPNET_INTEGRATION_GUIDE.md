# 🔗 ASP.NET Core Integration Guide

## 📋 Tổng quan

Hướng dẫn tích hợp ASP.NET Core Web API làm backend layer giữa Flutter app và Supabase database.

## 🏗️ Kiến trúc mới

```
┌─────────────────┐
│  Flutter App    │
│  (Mobile/Web)   │
└────────┬────────┘
         │ HTTP/REST
         ↓
┌─────────────────┐
│  ASP.NET Core   │
│   Web API       │
│  (Backend)      │
└────────┬────────┘
         │ Supabase Client
         ↓
┌─────────────────┐
│   Supabase      │
│  (PostgreSQL)   │
└─────────────────┘
```

## 🎯 Lợi ích

### Tại sao thêm ASP.NET Core?

1. **Business Logic Centralization**
   - Xử lý logic phức tạp ở server
   - Dễ maintain và update
   - Không cần update app khi thay đổi logic

2. **Security**
   - Ẩn Supabase credentials
   - Validate requests ở server
   - Rate limiting
   - Advanced authentication

3. **Performance**
   - Caching ở server
   - Data aggregation
   - Reduce client-side processing

4. **Integration**
   - Kết nối với services khác (Payment, Email, SMS)
   - Background jobs
   - Scheduled tasks

5. **Monitoring & Logging**
   - Centralized logging
   - Error tracking
   - Analytics

## 📦 ASP.NET Core Project Structure

```
TripMate.API/
├── Controllers/
│   ├── AuthController.cs
│   ├── ToursController.cs
│   ├── BookingsController.cs
│   └── ReviewsController.cs
├── Services/
│   ├── IAuthService.cs
│   ├── AuthService.cs
│   ├── ITourService.cs
│   ├── TourService.cs
│   └── SupabaseService.cs
├── Models/
│   ├── DTOs/
│   │   ├── LoginRequest.cs
│   │   ├── SignUpRequest.cs
│   │   ├── TourDto.cs
│   │   └── BookingDto.cs
│   └── Entities/
│       ├── User.cs
│       ├── Tour.cs
│       └── Booking.cs
├── Data/
│   └── SupabaseContext.cs
├── Middleware/
│   ├── AuthenticationMiddleware.cs
│   └── ErrorHandlingMiddleware.cs
├── Configuration/
│   └── SupabaseSettings.cs
├── appsettings.json
└── Program.cs
```

## 🔧 Setup ASP.NET Core Project

### 1. Tạo project

```bash
# Tạo solution
dotnet new sln -n TripMate

# Tạo Web API project
dotnet new webapi -n TripMate.API

# Add project vào solution
dotnet sln add TripMate.API/TripMate.API.csproj

# Navigate vào project
cd TripMate.API
```

### 2. Install NuGet Packages

```bash
# Supabase client
dotnet add package supabase-csharp

# PostgreSQL (nếu cần direct access)
dotnet add package Npgsql
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# JWT Authentication
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

# Other utilities
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package Serilog.AspNetCore
dotnet add package FluentValidation.AspNetCore
```

### 3. Configuration (appsettings.json)

```json
{
  "Supabase": {
    "Url": "https://nvbvvowyjzylllswhynv.supabase.co",
    "Key": "YOUR_SUPABASE_ANON_KEY",
    "ServiceRoleKey": "YOUR_SERVICE_ROLE_KEY"
  },
  "Jwt": {
    "Secret": "YOUR_JWT_SECRET_KEY_HERE_MINIMUM_32_CHARACTERS",
    "Issuer": "TripMate.API",
    "Audience": "TripMate.App",
    "ExpiryMinutes": 60
  },
  "ConnectionStrings": {
    "Supabase": "Host=db.nvbvvowyjzylllswhynv.supabase.co;Database=postgres;Username=postgres;Password=YOUR_DB_PASSWORD"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://your-flutter-web-app.com"
    ]
  }
}
```

### 4. Supabase Service

```csharp
// Services/SupabaseService.cs
using Supabase;

public interface ISupabaseService
{
    Client GetClient();
    Task<Client> GetAuthenticatedClient(string accessToken);
}

public class SupabaseService : ISupabaseService
{
    private readonly Client _client;
    private readonly IConfiguration _configuration;

    public SupabaseService(IConfiguration configuration)
    {
        _configuration = configuration;
        
        var url = configuration["Supabase:Url"];
        var key = configuration["Supabase:Key"];
        
        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
        };
        
        _client = new Client(url, key, options);
    }

    public Client GetClient() => _client;

    public async Task<Client> GetAuthenticatedClient(string accessToken)
    {
        var client = GetClient();
        await client.Auth.SetSession(accessToken, refreshToken: null);
        return client;
    }
}
```

### 5. Auth Controller

```csharp
// Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        try
        {
            var result = await _authService.SignUpAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sign up");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await _authService.LogoutAsync();
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return BadRequest(new { message = ex.Message });
        }
    }
}
```

### 6. Tours Controller

```csharp
// Controllers/ToursController.cs
[ApiController]
[Route("api/[controller]")]
public class ToursController : ControllerBase
{
    private readonly ITourService _tourService;

    public ToursController(ITourService tourService)
    {
        _tourService = tourService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTours([FromQuery] TourQueryParams queryParams)
    {
        var tours = await _tourService.GetToursAsync(queryParams);
        return Ok(tours);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTour(string id)
    {
        var tour = await _tourService.GetTourByIdAsync(id);
        if (tour == null)
            return NotFound();
        
        return Ok(tour);
    }

    [HttpPost]
    [Authorize(Roles = "guide,admin")]
    public async Task<IActionResult> CreateTour([FromBody] CreateTourRequest request)
    {
        var tour = await _tourService.CreateTourAsync(request);
        return CreatedAtAction(nameof(GetTour), new { id = tour.Id }, tour);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "guide,admin")]
    public async Task<IActionResult> UpdateTour(string id, [FromBody] UpdateTourRequest request)
    {
        var tour = await _tourService.UpdateTourAsync(id, request);
        return Ok(tour);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "guide,admin")]
    public async Task<IActionResult> DeleteTour(string id)
    {
        await _tourService.DeleteTourAsync(id);
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchTours([FromQuery] string query)
    {
        var tours = await _tourService.SearchToursAsync(query);
        return Ok(tours);
    }
}
```

### 7. Program.cs Configuration

```csharp
// Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Supabase
builder.Services.AddSingleton<ISupabaseService, SupabaseService>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITourService, TourService>();
builder.Services.AddScoped<IBookingService, BookingService>();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"];
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutterApp", policy =>
    {
        policy.WithOrigins(
            builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>())
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFlutterApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## 🔌 Kết nối Supabase

### Option 1: Sử dụng Supabase Client Library

```csharp
// Services/TourService.cs
public class TourService : ITourService
{
    private readonly ISupabaseService _supabase;

    public TourService(ISupabaseService supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<TourDto>> GetToursAsync(TourQueryParams queryParams)
    {
        var client = _supabase.GetClient();
        
        var query = client
            .From<Tour>()
            .Select("*")
            .Where(x => x.Status == "active");

        if (!string.IsNullOrEmpty(queryParams.Location))
        {
            query = query.Where(x => x.Location.Contains(queryParams.Location));
        }

        var response = await query.Get();
        var tours = response.Models;

        return tours.Select(t => new TourDto
        {
            Id = t.Id,
            Title = t.Title,
            Location = t.Location,
            Price = t.Price,
            // ... map other properties
        }).ToList();
    }
}
```

### Option 2: Sử dụng PostgreSQL Direct Connection

```csharp
// Data/SupabaseContext.cs
using Microsoft.EntityFrameworkCore;

public class SupabaseContext : DbContext
{
    public SupabaseContext(DbContextOptions<SupabaseContext> options)
        : base(options)
    {
    }

    public DbSet<Tour> Tours { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Profile> Profiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tour>()
            .ToTable("tours")
            .HasKey(t => t.Id);

        modelBuilder.Entity<Profile>()
            .ToTable("profiles")
            .HasKey(p => p.Id);

        // Configure relationships
        modelBuilder.Entity<Tour>()
            .HasOne(t => t.Guide)
            .WithMany(p => p.Tours)
            .HasForeignKey(t => t.GuideId);
    }
}

// Usage in service
public class TourService : ITourService
{
    private readonly SupabaseContext _context;

    public TourService(SupabaseContext context)
    {
        _context = context;
    }

    public async Task<List<TourDto>> GetToursAsync()
    {
        var tours = await _context.Tours
            .Where(t => t.Status == "active")
            .Include(t => t.Guide)
            .ToListAsync();

        return tours.Select(t => MapToDto(t)).ToList();
    }
}
```

### Option 3: Hybrid Approach

```csharp
// Sử dụng Supabase cho Auth, PostgreSQL cho data
public class HybridService
{
    private readonly ISupabaseService _supabase;
    private readonly SupabaseContext _context;

    public HybridService(ISupabaseService supabase, SupabaseContext context)
    {
        _supabase = supabase;
        _context = context;
    }

    // Auth qua Supabase
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var client = _supabase.GetClient();
        var session = await client.Auth.SignIn(request.Email, request.Password);
        return new AuthResponse { Token = session.AccessToken };
    }

    // Data qua PostgreSQL
    public async Task<List<Tour>> GetToursAsync()
    {
        return await _context.Tours.ToListAsync();
    }
}
```

## 📱 Update Flutter App

### 1. Thay đổi cấu trúc

```
lib/
├── core/
│   ├── config/
│   │   ├── api_config.dart          # NEW: API endpoints
│   │   └── supabase_config.dart     # REMOVE hoặc giữ cho realtime
│   └── services/
│       └── api_service.dart         # NEW: HTTP client
├── features/
│   ├── auth/
│   │   └── data/
│   │       └── datasources/
│   │           └── auth_api_datasource.dart  # NEW: Call ASP.NET API
│   └── tour/
│       └── data/
│           └── datasources/
│               └── tour_api_datasource.dart  # NEW: Call ASP.NET API
```

### 2. API Config

```dart
// lib/core/config/api_config.dart
class ApiConfig {
  static const String baseUrl = 'https://your-api.azurewebsites.net';
  // hoặc local: 'http://localhost:5000'
  
  static const String apiVersion = 'api';
  
  // Endpoints
  static const String authEndpoint = '$apiVersion/auth';
  static const String toursEndpoint = '$apiVersion/tours';
  static const String bookingsEndpoint = '$apiVersion/bookings';
  
  // Full URLs
  static String get loginUrl => '$baseUrl/$authEndpoint/login';
  static String get signupUrl => '$baseUrl/$authEndpoint/signup';
  static String get toursUrl => '$baseUrl/$toursEndpoint';
}
```

### 3. API Service

```dart
// lib/core/services/api_service.dart
import 'package:dio/dio.dart';

class ApiService {
  late final Dio _dio;
  
  ApiService() {
    _dio = Dio(BaseOptions(
      baseUrl: ApiConfig.baseUrl,
      connectTimeout: const Duration(seconds: 30),
      receiveTimeout: const Duration(seconds: 30),
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
    ));
    
    // Add interceptors
    _dio.interceptors.add(LogInterceptor(
      requestBody: true,
      responseBody: true,
    ));
    
    _dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        // Add auth token
        final token = await getStoredToken();
        if (token != null) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        handler.next(options);
      },
      onError: (error, handler) {
        // Handle errors
        Logger.error('API Error', error);
        handler.next(error);
      },
    ));
  }
  
  Future<Response> get(String path, {Map<String, dynamic>? queryParameters}) {
    return _dio.get(path, queryParameters: queryParameters);
  }
  
  Future<Response> post(String path, {dynamic data}) {
    return _dio.post(path, data: data);
  }
  
  Future<Response> put(String path, {dynamic data}) {
    return _dio.put(path, data: data);
  }
  
  Future<Response> delete(String path) {
    return _dio.delete(path);
  }
}
```

### 4. Update Data Source

```dart
// lib/features/tour/data/datasources/tour_api_datasource.dart
class TourApiDataSource {
  final ApiService _apiService;
  
  TourApiDataSource(this._apiService);
  
  Future<List<TourModel>> getTours() async {
    try {
      final response = await _apiService.get('/api/tours');
      final List<dynamic> data = response.data;
      return data.map((json) => TourModel.fromJson(json)).toList();
    } catch (e) {
      throw ServerException(message: 'Failed to fetch tours');
    }
  }
  
  Future<TourModel> createTour(CreateTourRequest request) async {
    try {
      final response = await _apiService.post(
        '/api/tours',
        data: request.toJson(),
      );
      return TourModel.fromJson(response.data);
    } catch (e) {
      throw ServerException(message: 'Failed to create tour');
    }
  }
}
```

### 5. Update pubspec.yaml

```yaml
dependencies:
  # HTTP client
  dio: ^5.4.0
  
  # Optional: Remove supabase_flutter nếu không dùng realtime
  # supabase_flutter: ^2.9.1
```

## 🚀 Deployment

### ASP.NET Core API

**Option 1: Azure App Service**
```bash
# Publish
dotnet publish -c Release

# Deploy to Azure
az webapp up --name tripmate-api --resource-group TripMate
```

**Option 2: Docker**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY bin/Release/net8.0/publish/ .
ENTRYPOINT ["dotnet", "TripMate.API.dll"]
```

**Option 3: IIS**
- Publish to folder
- Copy to IIS wwwroot
- Configure IIS site

### Flutter App

Update API URL trong production:
```dart
class ApiConfig {
  static const String baseUrl = kReleaseMode
      ? 'https://tripmate-api.azurewebsites.net'
      : 'http://localhost:5000';
}
```

## 📊 So sánh Approaches

### Direct Supabase (Hiện tại)
✅ Pros:
- Setup nhanh
- Ít code hơn
- Realtime built-in
- Auto-generated API

❌ Cons:
- Expose credentials
- Limited business logic
- Khó scale
- Khó integrate services khác

### ASP.NET Core + Supabase
✅ Pros:
- Secure credentials
- Complex business logic
- Easy integration
- Better monitoring
- Caching
- Rate limiting

❌ Cons:
- More code
- Extra server cost
- Maintenance overhead
- Latency tăng

## 🎯 Khi nào nên dùng ASP.NET Core?

**Nên dùng khi:**
- App có business logic phức tạp
- Cần integrate nhiều services (Payment, Email, SMS)
- Cần advanced security
- Team có expertise .NET
- Cần background jobs
- Scale lớn

**Không cần khi:**
- App đơn giản, CRUD basic
- Team chỉ biết Flutter/Dart
- Budget hạn chế
- Cần ship nhanh
- Supabase features đủ dùng

## 📚 Resources

- [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core)
- [Supabase C# Client](https://github.com/supabase-community/supabase-csharp)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [Dio HTTP Client](https://pub.dev/packages/dio)

Hướng dẫn hoàn tất! 🎉
