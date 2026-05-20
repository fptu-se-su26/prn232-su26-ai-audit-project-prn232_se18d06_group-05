# 🔌 TripMate API Guide

> Hướng dẫn sử dụng và phát triển ASP.NET Core API

## 🎯 Tổng quan

TripMate sử dụng hybrid architecture:
- **Supabase**: Direct client access cho auth, realtime
- **ASP.NET Core**: Business logic, complex operations
- **Flutter**: Frontend với dual data sources

## 🏗️ Architecture

```
Flutter App
├── Supabase Client ──────► Supabase (Auth, Realtime, Simple CRUD)
└── Dio HTTP Client ──────► ASP.NET API ──────► Supabase (Complex Logic)
```

## 🚀 Quick Start

### 1. Setup ASP.NET Project

```bash
# Navigate to API directory
cd web/TripMate_Webapi

# Restore packages
dotnet restore

# Run API
dotnet run

# API available at: https://localhost:5001
```

### 2. Configuration

**appsettings.json:**
```json
{
  "Supabase": {
    "Url": "https://nvbvvowyjzylllswhynv.supabase.co",
    "AnonKey": "your_anon_key_here"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### 3. Test API

```bash
# Health check
curl https://localhost:5001/api/debug/env

# Database test
curl https://localhost:5001/api/debug/db-test

# Chat test
curl https://localhost:5001/api/debug/chat-test
```

## 📡 API Endpoints

### Authentication
```http
# Note: Auth handled by Supabase directly
# API uses JWT tokens from Supabase
```

### Tours
```http
GET    /api/tours              # Get all tours
GET    /api/tours/{id}         # Get tour by ID
POST   /api/tours              # Create tour (Guide only)
PUT    /api/tours/{id}         # Update tour (Guide/Admin)
DELETE /api/tours/{id}         # Delete tour (Guide/Admin)
```

### Bookings
```http
GET    /api/bookings/my        # Get user bookings
POST   /api/bookings           # Create booking
DELETE /api/bookings/{id}      # Cancel booking
```

### Chat
```http
GET    /api/chat/conversations                    # Get user conversations
POST   /api/chat/conversations                    # Create conversation
GET    /api/chat/conversations/{id}/messages      # Get messages
POST   /api/chat/conversations/{id}/messages      # Send message
```

### Debug (Development only)
```http
GET    /api/debug/env          # Environment info
GET    /api/debug/db-test      # Database connection test
GET    /api/debug/chat-test    # Chat service test
```

## 🔐 Authentication

### JWT Token Flow

1. **Flutter** authenticates with **Supabase**
2. **Supabase** returns JWT token
3. **Flutter** sends token to **ASP.NET API**
4. **API** validates token with Supabase

### Implementation

**Controller:**
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires JWT token
public class ToursController : ControllerBase
{
    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    
    [HttpGet]
    public async Task<IActionResult> GetTours()
    {
        // Access authenticated user ID
        var userId = UserId;
        // Your logic here
    }
}
```

**Flutter:**
```dart
// Get token from Supabase
final token = await TokenStorage.getAccessToken();

// Send to API
final response = await dio.get(
  '/api/tours',
  options: Options(
    headers: {'Authorization': 'Bearer $token'},
  ),
);
```

## 🛠️ Services

### ChatService
Handles real-time messaging through Supabase:

```csharp
public class ChatService
{
    public async Task<ConversationDto> GetOrCreateConversationAsync(
        string travelerId, string guideId, string bookingId, string userToken)
    {
        // Check existing conversation
        // Create new if not exists
        // Return conversation details
    }
    
    public async Task<MessageDto> SendMessageAsync(
        string conversationId, string senderId, string content, string userToken)
    {
        // Insert message to Supabase
        // Trigger real-time notification
        // Return message details
    }
}
```

### NotificationService
Handles push notifications:

```csharp
public class NotificationService
{
    public async Task SendBookingConfirmationAsync(string userId, BookingDto booking)
    {
        // Send push notification
        // Store in notifications table
    }
}
```

## 📊 Data Models

### DTOs (Data Transfer Objects)

```csharp
public record TourDto(
    string Id,
    string Title,
    string Description,
    string Location,
    decimal Price,
    int DurationHours,
    int MaxParticipants,
    string[] Images,
    string Status,
    DateTime CreatedAt
);

public record BookingDto(
    string Id,
    string TourId,
    string TravelerId,
    string GuideId,
    DateTime TourDate,
    int Guests,
    decimal TotalPrice,
    string Status,
    DateTime CreatedAt
);

public record ConversationDto(
    string Id,
    string TravelerId,
    string GuideId,
    string? BookingId,
    DateTime CreatedAt
);
```

## 🔄 Error Handling

### Global Exception Handler

```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var response = new { message = ex.Message, type = ex.GetType().Name };
        context.Response.StatusCode = ex switch
        {
            UnauthorizedAccessException => 401,
            ArgumentException => 400,
            _ => 500
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

### Controller Error Handling

```csharp
[HttpPost]
public async Task<IActionResult> CreateTour([FromBody] CreateTourRequest request)
{
    try
    {
        var tour = await _tourService.CreateAsync(request, UserId);
        return Ok(tour);
    }
    catch (UnauthorizedAccessException)
    {
        return Unauthorized(new { message = "Only guides can create tours" });
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Internal server error" });
    }
}
```

## 🧪 Testing

### Unit Tests

```csharp
[Test]
public async Task CreateTour_ValidRequest_ReturnsOk()
{
    // Arrange
    var request = new CreateTourRequest { Title = "Test Tour" };
    
    // Act
    var result = await _controller.CreateTour(request);
    
    // Assert
    Assert.IsInstanceOf<OkObjectResult>(result);
}
```

### Integration Tests

```csharp
[Test]
public async Task GetTours_ReturnsAllTours()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/api/tours");
    
    // Assert
    response.EnsureSuccessStatusCode();
    var tours = await response.Content.ReadFromJsonAsync<TourDto[]>();
    Assert.IsNotNull(tours);
}
```

## 📈 Performance

### Caching

```csharp
[HttpGet]
[ResponseCache(Duration = 300)] // Cache for 5 minutes
public async Task<IActionResult> GetTours()
{
    var tours = await _tourService.GetAllAsync();
    return Ok(tours);
}
```

### Pagination

```csharp
[HttpGet]
public async Task<IActionResult> GetTours(
    [FromQuery] int page = 1,
    [FromQuery] int limit = 20)
{
    var tours = await _tourService.GetPagedAsync(page, limit);
    return Ok(new { tours, page, limit, total = tours.TotalCount });
}
```

## 🚀 Deployment

### Development
```bash
dotnet run --environment Development
```

### Production
```bash
# Build
dotnet build -c Release

# Publish
dotnet publish -c Release -o ./publish

# Run
dotnet ./publish/TripMate_WebAPI.dll
```

### Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY ./publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "TripMate_WebAPI.dll"]
```

## 🔧 Development Tools

### Swagger/OpenAPI
```csharp
// Program.cs
builder.Services.AddSwaggerGen();

app.UseSwagger();
app.UseSwaggerUI();

// Available at: https://localhost:5001/swagger
```

### Hot Reload
```bash
dotnet watch run
```

### Logging
```csharp
public class ToursController : ControllerBase
{
    private readonly ILogger<ToursController> _logger;
    
    public ToursController(ILogger<ToursController> logger)
    {
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetTours()
    {
        _logger.LogInformation("Getting all tours");
        // Your logic
    }
}
```

## 🐛 Troubleshooting

### Common Issues

#### 1. CORS Errors
```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutter", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://yourapp.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("AllowFlutter");
```

#### 2. HTTPS Certificate Issues
```bash
# Trust development certificate
dotnet dev-certs https --trust

# Clear and regenerate
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

#### 3. Supabase Connection Issues
- Verify URL and API key in appsettings.json
- Check network connectivity
- Ensure Supabase project is active

### Debug Commands
```bash
# Check .NET version
dotnet --version

# List installed SDKs
dotnet --list-sdks

# Restore packages
dotnet restore

# Clean build
dotnet clean
dotnet build
```

## 📚 Resources

### Documentation
- [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core/)
- [Supabase C# Client](https://github.com/supabase-community/supabase-csharp)
- [JWT Authentication](https://docs.microsoft.com/aspnet/core/security/authentication/jwt-authn)

### Tools
- [Postman](https://www.postman.com/) - API testing
- [Swagger](https://swagger.io/) - API documentation
- [Docker](https://www.docker.com/) - Containerization

---

**API Version**: 1.0.0  
**Framework**: ASP.NET Core 7.0  
**Last Updated**: December 2024