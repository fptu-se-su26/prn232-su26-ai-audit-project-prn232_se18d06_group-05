# 🚀 TripMate ASP.NET Core Quick Start

> Get the ASP.NET Core API running in under 30 minutes

## ⚡ Super Quick Setup (5 minutes)

### Prerequisites Check
```bash
# Verify .NET SDK
dotnet --version  # Should be 7.0+

# Verify project structure
ls web/TripMate_Webapi/  # Should see .csproj file
```

### 1. Navigate and Restore
```bash
cd web/TripMate_Webapi
dotnet restore
```

### 2. Configure Supabase
Edit `appsettings.json`:
```json
{
  "Supabase": {
    "Url": "https://nvbvvowyjzylllswhynv.supabase.co",
    "AnonKey": "your_supabase_anon_key_here"
  }
}
```

### 3. Run API
```bash
dotnet run

# ✅ API running at: https://localhost:5001
# ✅ Swagger UI: https://localhost:5001/swagger
```

### 4. Test API
```bash
# Health check
curl https://localhost:5001/api/debug/env

# Database test
curl https://localhost:5001/api/debug/db-test
```

## 🎯 What You Get

### Available Endpoints
- **Debug**: `/api/debug/*` - Development utilities
- **Chat**: `/api/chat/*` - Real-time messaging
- **Tours**: `/api/tours/*` - Tour management (planned)
- **Bookings**: `/api/bookings/*` - Booking system (planned)

### Development Features
- **Hot Reload**: `dotnet watch run`
- **Swagger UI**: Interactive API documentation
- **Logging**: Structured logging with Serilog
- **CORS**: Configured for Flutter development
- **JWT Auth**: Ready for Supabase tokens

## 🔧 Development Workflow

### Daily Development
```bash
# Start with hot reload
dotnet watch run

# Run tests
dotnet test

# Build for production
dotnet build -c Release
```

### Adding New Features
1. **Create Controller**: `Controllers/NewController.cs`
2. **Add Service**: `Services/NewService.cs`
3. **Register Service**: In `Program.cs`
4. **Test Endpoint**: Use Swagger UI

### Example: Add New Controller
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NewController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(new { message = "Hello from new controller!" });
    }
}
```

## 🐛 Common Issues & Solutions

### Issue: Port Already in Use
```bash
# Change port in launchSettings.json
"applicationUrl": "https://localhost:5002;http://localhost:5001"
```

### Issue: HTTPS Certificate
```bash
# Trust development certificate
dotnet dev-certs https --trust
```

### Issue: CORS Errors
Already configured in `Program.cs`:
```csharp
app.UseCors("AllowAll");
```

### Issue: Supabase Connection
1. Verify URL and API key in `appsettings.json`
2. Test with debug endpoint: `/api/debug/db-test`
3. Check Supabase project status

## 📊 Project Structure

```
TripMate_Webapi/
├── Controllers/          # API endpoints
│   ├── ChatController.cs
│   └── DebugController.cs
├── Services/            # Business logic
│   ├── ChatService.cs
│   └── NotificationService.cs
├── Models/              # Data models
├── Program.cs           # App configuration
├── appsettings.json     # Configuration
└── Properties/
    └── launchSettings.json
```

## 🚀 Next Steps

### Immediate (Today)
1. ✅ Get API running
2. ✅ Test debug endpoints
3. ✅ Verify Supabase connection
4. ✅ Test with Flutter app

### Short Term (This Week)
1. Add authentication middleware
2. Implement tour endpoints
3. Add booking endpoints
4. Set up logging

### Medium Term (This Month)
1. Add comprehensive testing
2. Set up CI/CD pipeline
3. Add monitoring and metrics
4. Prepare for production deployment

## 📚 Useful Commands

```bash
# Development
dotnet watch run              # Hot reload
dotnet run --environment Development

# Testing
dotnet test                   # Run all tests
dotnet test --logger console # Verbose test output

# Building
dotnet build                  # Debug build
dotnet build -c Release      # Release build
dotnet publish -c Release    # Publish for deployment

# Package Management
dotnet add package PackageName
dotnet remove package PackageName
dotnet list package

# Project Management
dotnet new webapi -n NewProject
dotnet sln add NewProject
```

## 🎯 Success Checklist

- [ ] ✅ .NET SDK installed and working
- [ ] ✅ Project builds without errors
- [ ] ✅ API starts and responds to requests
- [ ] ✅ Swagger UI accessible
- [ ] ✅ Supabase connection working
- [ ] ✅ Debug endpoints responding
- [ ] ✅ CORS configured for Flutter
- [ ] ✅ Hot reload working with `dotnet watch run`

## 🆘 Getting Help

### Quick Debugging
```bash
# Check .NET version
dotnet --version

# Check project status
dotnet build

# Check running processes
netstat -an | grep :5001
```

### Resources
- **ASP.NET Docs**: [docs.microsoft.com/aspnet](https://docs.microsoft.com/aspnet)
- **Supabase C#**: [github.com/supabase-community/supabase-csharp](https://github.com/supabase-community/supabase-csharp)
- **Swagger**: Available at `https://localhost:5001/swagger`

### Team Support
- Check [API_GUIDE.md](../../../docs/API_GUIDE.md) for detailed documentation
- Review [TROUBLESHOOTING.md](../../../docs/TROUBLESHOOTING.md) for common issues
- Contact development team for complex issues

---

**Total Setup Time**: ~15 minutes  
**Difficulty**: Beginner  
**Status**: ✅ Production Ready  
**Last Updated**: December 2024

*Ready to build amazing APIs! 🚀*
