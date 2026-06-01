# 🔀 ASP.NET MVC vs Web API với Supabase

## 📋 Tổng quan

Bạn có **3 options** để sử dụng ASP.NET với Supabase và Flutter:

## 🎯 Option 1: Web API Only (Đã hướng dẫn)

```
Flutter App ←→ ASP.NET Web API ←→ Supabase
```

**Khi nào dùng:**
- ✅ Chỉ có mobile/Flutter app
- ✅ Cần API cho nhiều clients (iOS, Android, Web)
- ✅ Microservices architecture
- ✅ RESTful API

**Ưu điểm:**
- Stateless, dễ scale
- Dùng được cho nhiều platforms
- JSON response, dễ consume

**Nhược điểm:**
- Không có web UI
- Cần frontend riêng

---

## 🎯 Option 2: MVC với Razor Pages (cshtml)

```
Web Browser ←→ ASP.NET MVC (cshtml) ←→ Supabase
Flutter App ←→ ASP.NET MVC (cshtml) ←→ Supabase (không tối ưu)
```

**Khi nào dùng:**
- ✅ Cần admin panel/dashboard web
- ✅ Server-side rendering
- ✅ Traditional web app
- ✅ SEO quan trọng

**Ưu điểm:**
- Full-stack trong 1 project
- Server-side rendering
- Razor views cho UI

**Nhược điểm:**
- Không phù hợp cho mobile API
- HTML response, không phù hợp Flutter
- Khó scale

---

## 🎯 Option 3: Hybrid (MVC + Web API) ⭐ KHUYÊN DÙNG

```
Web Browser ←→ ASP.NET MVC (cshtml) ←→ Supabase
                      ↓
Flutter App ←→ ASP.NET Web API ←→ Supabase
```

**Khi nào dùng:**
- ✅ Cần cả web admin và mobile app
- ✅ Best of both worlds
- ✅ Shared business logic

**Ưu điểm:**
- Web admin panel với Razor
- API cho Flutter app
- Shared services và models
- Một database, nhiều clients

**Nhược điểm:**
- Project phức tạp hơn
- Cần maintain cả 2

---

## 🏗️ Cách implement từng option

### Option 2: ASP.NET MVC với Supabase

#### 1. Tạo MVC Project

```bash
dotnet new mvc -n TripMate.Web
cd TripMate.Web
dotnet add package supabase-csharp
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

#### 2. Configure Supabase

```csharp
// Program.cs
builder.Services.AddSingleton<ISupabaseService, SupabaseService>();
builder.Services.AddScoped<ITourService, TourService>();

builder.Services.AddControllersWithViews();
```

#### 3. Controller với Views

```csharp
// Controllers/ToursController.cs
public class ToursController : Controller
{
    private readonly ITourService _tourService;

    public ToursController(ITourService tourService)
    {
        _tourService = tourService;
    }

    // GET: /Tours
    public async Task<IActionResult> Index()
    {
        var tours = await _tourService.GetToursAsync();
        return View(tours);
    }

    // GET: /Tours/Details/5
    public async Task<IActionResult> Details(string id)
    {
        var tour = await _tourService.GetTourByIdAsync(id);
        if (tour == null)
            return NotFound();
        
        return View(tour);
    }

    // GET: /Tours/Create
    [Authorize(Roles = "guide,admin")]
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Tours/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "guide,admin")]
    public async Task<IActionResult> Create(CreateTourViewModel model)
    {
        if (ModelState.IsValid)
        {
            await _tourService.CreateTourAsync(model);
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }
}
```

#### 4. Razor View

```cshtml
@* Views/Tours/Index.cshtml *@
@model List<TourDto>

<div class="container">
    <h1>Tours</h1>
    
    @if (User.IsInRole("guide") || User.IsInRole("admin"))
    {
        <a asp-action="Create" class="btn btn-primary">Create New Tour</a>
    }
    
    <div class="row mt-4">
        @foreach (var tour in Model)
        {
            <div class="col-md-4 mb-4">
                <div class="card">
                    <img src="@tour.Images.FirstOrDefault()" class="card-img-top" alt="@tour.Title">
                    <div class="card-body">
                        <h5 class="card-title">@tour.Title</h5>
                        <p class="card-text">@tour.Location</p>
                        <p class="card-text">
                            <strong>@tour.Price.ToString("C0", new CultureInfo("vi-VN"))</strong>
                        </p>
                        <a asp-action="Details" asp-route-id="@tour.Id" class="btn btn-primary">
                            View Details
                        </a>
                    </div>
                </div>
            </div>
        }
    </div>
</div>
```

**Vấn đề:** Flutter app không thể dùng HTML response!

---

### Option 3: Hybrid (MVC + API) ⭐

#### 1. Tạo Project

```bash
dotnet new mvc -n TripMate.Web
cd TripMate.Web
dotnet add package supabase-csharp
```

#### 2. Project Structure

```
TripMate.Web/
├── Controllers/
│   ├── Web/                    # MVC Controllers (cshtml)
│   │   ├── HomeController.cs
│   │   ├── ToursController.cs
│   │   └── AdminController.cs
│   └── Api/                    # API Controllers (JSON)
│       ├── ToursApiController.cs
│       ├── BookingsApiController.cs
│       └── AuthApiController.cs
├── Views/                      # Razor views cho web
│   ├── Home/
│   ├── Tours/
│   └── Admin/
├── Services/                   # Shared services
│   ├── TourService.cs
│   └── SupabaseService.cs
├── Models/
│   ├── ViewModels/            # Cho MVC views
│   └── DTOs/                  # Cho API
└── wwwroot/                   # Static files
```

#### 3. MVC Controller (cho Web Admin)

```csharp
// Controllers/Web/ToursController.cs
[Route("tours")]
public class ToursController : Controller
{
    private readonly ITourService _tourService;

    public ToursController(ITourService tourService)
    {
        _tourService = tourService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var tours = await _tourService.GetToursAsync();
        return View(tours);
    }

    [HttpGet("create")]
    [Authorize(Roles = "guide,admin")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "guide,admin")]
    public async Task<IActionResult> Create(CreateTourViewModel model)
    {
        if (ModelState.IsValid)
        {
            await _tourService.CreateTourAsync(model);
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }
}
```

#### 4. API Controller (cho Flutter App)

```csharp
// Controllers/Api/ToursApiController.cs
[ApiController]
[Route("api/tours")]
public class ToursApiController : ControllerBase
{
    private readonly ITourService _tourService;

    public ToursApiController(ITourService tourService)
    {
        _tourService = tourService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTours()
    {
        var tours = await _tourService.GetToursAsync();
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
}
```

#### 5. Shared Service

```csharp
// Services/TourService.cs
public class TourService : ITourService
{
    private readonly ISupabaseService _supabase;

    public TourService(ISupabaseService supabase)
    {
        _supabase = supabase;
    }

    // Dùng chung cho cả MVC và API
    public async Task<List<TourDto>> GetToursAsync()
    {
        var client = _supabase.GetClient();
        var response = await client
            .From<Tour>()
            .Select("*")
            .Where(x => x.Status == "active")
            .Get();
        
        return response.Models.Select(MapToDto).ToList();
    }

    public async Task<TourDto> CreateTourAsync(object request)
    {
        // Logic tạo tour
        // Dùng được cho cả MVC form và API JSON
    }
}
```

#### 6. Program.cs Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add MVC
builder.Services.AddControllersWithViews();

// Add API
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Shared services
builder.Services.AddSingleton<ISupabaseService, SupabaseService>();
builder.Services.AddScoped<ITourService, TourService>();

// Authentication
builder.Services.AddAuthentication(/* ... */);

// CORS cho Flutter app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutterApp", policy =>
    {
        policy.WithOrigins("http://localhost:*")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowFlutterApp");
app.UseAuthentication();
app.UseAuthorization();

// Map routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers(); // For API

app.Run();
```

---

## 📊 So sánh chi tiết

| Feature | Web API Only | MVC Only | Hybrid (MVC + API) |
|---------|-------------|----------|-------------------|
| Flutter App | ✅ Perfect | ❌ Không phù hợp | ✅ Perfect |
| Web Admin | ❌ Cần frontend riêng | ✅ Built-in | ✅ Built-in |
| Complexity | ⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ |
| Maintenance | Easy | Easy | Medium |
| Code Reuse | N/A | N/A | ✅ High |
| SEO | ❌ | ✅ | ✅ |
| Mobile API | ✅ | ❌ | ✅ |
| Learning Curve | Low | Low | Medium |

---

## 🎯 Khuyến nghị

### Cho TripMate Project:

**Nếu chỉ có Flutter app:**
→ Dùng **Web API Only** (đã hướng dẫn)

**Nếu cần admin panel web:**
→ Dùng **Hybrid (MVC + API)** ⭐

**Nếu chỉ làm web app:**
→ Dùng **MVC Only**

---

## 🚀 Quick Start Hybrid

```bash
# 1. Tạo project
dotnet new mvc -n TripMate.Web

# 2. Install packages
dotnet add package supabase-csharp

# 3. Tạo folders
mkdir Controllers/Web
mkdir Controllers/Api

# 4. Move existing controllers
mv Controllers/HomeController.cs Controllers/Web/

# 5. Create API controller
# (Copy code từ trên)

# 6. Run
dotnet run
```

**Access:**
- Web Admin: `https://localhost:5001/tours`
- API: `https://localhost:5001/api/tours`
- Flutter app call: `https://localhost:5001/api/tours`

---

## 💡 Best Practices

### 1. Separate Concerns

```csharp
// ViewModel cho MVC
public class CreateTourViewModel
{
    [Required]
    [Display(Name = "Tour Title")]
    public string Title { get; set; }
    
    [Required]
    public string Location { get; set; }
    
    // HTML form fields
}

// DTO cho API
public class CreateTourRequest
{
    public string Title { get; set; }
    public string Location { get; set; }
    
    // JSON properties
}
```

### 2. Shared Service Layer

```csharp
// Service dùng chung
public interface ITourService
{
    Task<List<TourDto>> GetToursAsync();
    Task<TourDto> CreateTourAsync(CreateTourDto dto);
}

// MVC Controller
var tours = await _tourService.GetToursAsync();
return View(tours);

// API Controller
var tours = await _tourService.GetToursAsync();
return Ok(tours);
```

### 3. Different Authentication

```csharp
// MVC: Cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

// API: JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
```

---

## 📚 Resources

- [ASP.NET MVC Tutorial](https://docs.microsoft.com/aspnet/core/mvc)
- [ASP.NET Web API Tutorial](https://docs.microsoft.com/aspnet/core/web-api)
- [Razor Pages](https://docs.microsoft.com/aspnet/core/razor-pages)

---

## ✅ Kết luận

**Câu trả lời:** KHÔNG, bạn không bắt buộc phải dùng Web API. Bạn có thể:

1. ✅ Dùng MVC với cshtml (nếu chỉ làm web)
2. ✅ Dùng Web API (nếu chỉ có Flutter app)
3. ✅ Dùng cả 2 trong 1 project (Hybrid) ⭐ KHUYÊN DÙNG

Với TripMate, **Hybrid approach** là tốt nhất vì:
- Flutter app dùng API endpoints
- Admin panel dùng MVC views
- Shared business logic
- Một Supabase database cho tất cả

Chọn approach phù hợp với nhu cầu của bạn! 🎯
