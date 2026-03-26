# 🔀 ASP.NET MVC vs Web API với Supabase

## 📋 Tổng quan

Bạn có **3 options** để tích hợp ASP.NET với Flutter + Supabase:

## 🎯 Option 1: Web API Only (Đã hướng dẫn)

### Kiến trúc
```
┌─────────────┐
│ Flutter App │ ──HTTP/JSON──┐
└─────────────┘              │
                             ↓
                    ┌─────────────────┐
                    │  ASP.NET Web    │
                    │      API        │
                    └────────┬────────┘
                             │
                             ↓
                    ┌─────────────────┐
                    │    Supabase     │
                    │   (PostgreSQL)  │
                    └─────────────────┘
```

### Đặc điểm
- ✅ RESTful API endpoints
- ✅ Trả về JSON
- ✅ Dùng cho mobile/web apps
- ✅ Stateless
- ❌ Không có UI server-side

### Khi nào dùng
- Flutter app là main UI
- Cần API cho mobile/web
- Microservices architecture

---

## 🎯 Option 2: ASP.NET MVC với Razor/CSHTML

### Kiến trúc
```
┌─────────────┐
│   Browser   │ ──HTTP/HTML──┐
└─────────────┘              │
                             ↓
┌─────────────┐     ┌─────────────────┐
│ Flutter App │ ──┐ │  ASP.NET MVC    │
└─────────────┘   │ │  (Razor/CSHTML) │
                  │ └────────┬────────┘
                  │          │
                  │          ↓
                  │ ┌─────────────────┐
                  └→│    Supabase     │
                    │   (PostgreSQL)  │
                    └─────────────────┘
```

### Đặc điểm
- ✅ Server-side rendering (Razor views)
- ✅ Trả về HTML pages
- ✅ Admin panel, dashboard
- ✅ SEO friendly
- ✅ Session management
- ⚠️ Flutter app vẫn connect trực tiếp Supabase

### Project Structure
```
TripMate.Web/
├── Controllers/
│   ├── HomeController.cs
│   ├── AdminController.cs
│   └── ToursController.cs
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml
│   │   └── About.cshtml
│   ├── Admin/
│   │   ├── Dashboard.cshtml
│   │   ├── Users.cshtml
│   │   └── Tours.cshtml
│   └── Shared/
│       ├── _Layout.cshtml
│       └── _LoginPartial.cshtml
├── Models/
│   ├── Tour.cs
│   └── User.cs
├── Services/
│   └── SupabaseService.cs
└── wwwroot/
    ├── css/
    ├── js/
    └── images/
```

### Code Example

**Controller:**
```csharp
// Controllers/AdminController.cs
public class AdminController : Controller
{
    private readonly ISupabaseService _supabase;

    public AdminController(ISupabaseService supabase)
    {
        _supabase = supabase;
    }

    public async Task<IActionResult> Dashboard()
    {
        var client = _supabase.GetClient();
        
        // Fetch data from Supabase
        var tours = await client.From<Tour>().Get();
        var users = await client.From<Profile>().Get();
        
        var viewModel = new DashboardViewModel
        {
            TotalTours = tours.Models.Count,
            TotalUsers = users.Models.Count,
            RecentTours = tours.Models.Take(10).ToList()
        };
        
        return View(viewModel);
    }

    public async Task<IActionResult> Tours()
    {
        var client = _supabase.GetClient();
        var tours = await client.From<Tour>().Get();
        
        return View(tours.Models);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTour(string id)
    {
        var client = _supabase.GetClient();
        await client.From<Tour>().Where(x => x.Id == id).Delete();
        
        return RedirectToAction(nameof(Tours));
    }
}
```

**View (Razor):**
```cshtml
@* Views/Admin/Dashboard.cshtml *@
@model DashboardViewModel

<div class="container">
    <h1>Admin Dashboard</h1>
    
    <div class="row">
        <div class="col-md-3">
            <div class="card">
                <div class="card-body">
                    <h5>Total Tours</h5>
                    <h2>@Model.TotalTours</h2>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card">
                <div class="card-body">
                    <h5>Total Users</h5>
                    <h2>@Model.TotalUsers</h2>
                </div>
            </div>
        </div>
    </div>
    
    <h3>Recent Tours</h3>
    <table class="table">
        <thead>
            <tr>
                <th>Title</th>
                <th>Location</th>
                <th>Price</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var tour in Model.RecentTours)
            {
                <tr>
                    <td>@tour.Title</td>
                    <td>@tour.Location</td>
                    <td>@tour.Price.ToString("C")</td>
                    <td>
                        <a href="/admin/tours/edit/@tour.Id">Edit</a>
                        <a href="/admin/tours/delete/@tour.Id">Delete</a>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>
```

### Khi nào dùng
- Cần admin panel với UI
- Cần web dashboard
- SEO quan trọng
- Team quen ASP.NET MVC

---

## 🎯 Option 3: Hybrid (MVC + Web API)

### Kiến trúc
```
┌─────────────┐                    ┌─────────────┐
│   Browser   │ ──HTTP/HTML──┐     │ Flutter App │
└─────────────┘              │     └──────┬──────┘
                             │            │
                             ↓            │ HTTP/JSON
                    ┌─────────────────┐   │
                    │  ASP.NET MVC    │   │
                    │  + Web API      │←──┘
                    └────────┬────────┘
                             │
                             ↓
                    ┌─────────────────┐
                    │    Supabase     │
                    └─────────────────┘
```

### Project Structure
```
TripMate/
├── Controllers/
│   ├── Web/ (MVC Controllers)
│   │   ├── HomeController.cs
│   │   └── AdminController.cs
│   └── Api/ (API Controllers)
│       ├── ToursApiController.cs
│       └── BookingsApiController.cs
├── Views/ (Razor views cho MVC)
│   ├── Home/
│   └── Admin/
├── Models/
├── Services/
└── wwwroot/
```

### Code Example

**MVC Controller (cho web UI):**
```csharp
// Controllers/Web/AdminController.cs
[Route("admin")]
public class AdminController : Controller
{
    private readonly ITourService _tourService;

    [Route("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var stats = await _tourService.GetStatsAsync();
        return View(stats);
    }

    [Route("tours")]
    public async Task<IActionResult> Tours()
    {
        var tours = await _tourService.GetAllToursAsync();
        return View(tours);
    }
}
```

**API Controller (cho Flutter app):**
```csharp
// Controllers/Api/ToursApiController.cs
[ApiController]
[Route("api/tours")]
public class ToursApiController : ControllerBase
{
    private readonly ITourService _tourService;

    [HttpGet]
    public async Task<IActionResult> GetTours()
    {
        var tours = await _tourService.GetAllToursAsync();
        return Ok(tours); // Returns JSON
    }

    [HttpPost]
    public async Task<IActionResult> CreateTour([FromBody] CreateTourRequest request)
    {
        var tour = await _tourService.CreateTourAsync(request);
        return CreatedAtAction(nameof(GetTour), new { id = tour.Id }, tour);
    }
}
```

**Program.cs:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add MVC
builder.Services.AddControllersWithViews();

// Add API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Shared services
builder.Services.AddScoped<ITourService, TourService>();
builder.Services.AddSingleton<ISupabaseService, SupabaseService>();

var app = builder.Build();

// Configure MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Configure API
app.MapControllers();

app.Run();
```

### Khi nào dùng
- Cần cả admin web UI và mobile API
- Một codebase cho tất cả
- Share business logic
- Team fullstack .NET

---

## 📊 So sánh chi tiết

| Feature | Web API Only | MVC Only | Hybrid |
|---------|-------------|----------|--------|
| **Flutter Support** | ✅ Perfect | ⚠️ Indirect | ✅ Perfect |
| **Admin UI** | ❌ No | ✅ Yes | ✅ Yes |
| **JSON API** | ✅ Yes | ❌ No | ✅ Yes |
| **Server Rendering** | ❌ No | ✅ Yes | ✅ Yes |
| **SEO** | ❌ No | ✅ Yes | ✅ Yes |
| **Complexity** | 🟢 Low | 🟡 Medium | 🔴 High |
| **Maintenance** | 🟢 Easy | 🟡 Medium | 🔴 Complex |
| **Best for** | Mobile apps | Web apps | Enterprise |

---

## 🎯 Khuyến nghị cho TripMate

### Scenario 1: Chỉ có Flutter App
→ **Dùng Web API Only**
- Đơn giản nhất
- Focus vào mobile
- Dễ maintain

### Scenario 2: Cần Admin Panel
→ **Dùng Hybrid (MVC + API)**
- Admin panel với Razor views
- API cho Flutter app
- Share business logic

### Scenario 3: Cần Website + App
→ **Dùng Hybrid hoặc tách riêng**
- Website: ASP.NET MVC
- API: ASP.NET Web API (riêng project)
- App: Flutter

---

## 💡 Setup Hybrid Project

### Bước 1: Tạo project
```bash
dotnet new mvc -n TripMate.Web
cd TripMate.Web
```

### Bước 2: Add API support
```csharp
// Program.cs
builder.Services.AddControllersWithViews(); // MVC
builder.Services.AddControllers(); // API
builder.Services.AddSwaggerGen(); // API docs

app.MapControllerRoute(...); // MVC routes
app.MapControllers(); // API routes
```

### Bước 3: Organize controllers
```
Controllers/
├── Web/
│   ├── HomeController.cs      [Route("")]
│   └── AdminController.cs     [Route("admin")]
└── Api/
    ├── ToursController.cs     [Route("api/tours")]
    └── BookingsController.cs  [Route("api/bookings")]
```

### Bước 4: Share services
```csharp
// Services được dùng chung
builder.Services.AddScoped<ITourService, TourService>();
builder.Services.AddScoped<IBookingService, BookingService>();
```

---

## 🔑 Key Points

1. **Web API** = Trả về JSON, cho mobile/SPA
2. **MVC** = Trả về HTML, server-side rendering
3. **Hybrid** = Cả hai trong một project
4. **Supabase** = Có thể dùng với cả 3 options

### Tất cả đều có thể share Supabase database!

```csharp
// Service được dùng chung cho cả MVC và API
public class TourService : ITourService
{
    private readonly ISupabaseService _supabase;
    
    // Dùng cho cả MVC controllers và API controllers
    public async Task<List<Tour>> GetToursAsync()
    {
        var client = _supabase.GetClient();
        var response = await client.From<Tour>().Get();
        return response.Models;
    }
}
```

---

## 🎓 Kết luận

**Câu trả lời:** KHÔNG, bạn không bắt buộc dùng Web API. Bạn có thể:

1. ✅ Dùng MVC với CSHTML cho admin panel
2. ✅ Flutter app connect trực tiếp Supabase
3. ✅ Hoặc dùng Hybrid: MVC cho web + API cho Flutter

**Khuyến nghị cho TripMate:**
- **Hiện tại:** Flutter + Supabase trực tiếp (đơn giản nhất)
- **Nếu cần admin:** Thêm ASP.NET MVC cho admin panel
- **Nếu cần API layer:** Thêm Web API giữa Flutter và Supabase

Chọn theo nhu cầu thực tế! 🚀
