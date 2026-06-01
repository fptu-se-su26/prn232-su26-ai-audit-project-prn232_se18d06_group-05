# Razor Pages Admin Dashboard Guide

## ✅ Đã Tạo

### 1. **AdminDashboard.cshtml**
- Razor Page view với Tailwind CSS
- Theme màu cam TripMate (#FF6B35)
- Sidebar navigation
- Metrics cards (Revenue, Bookings, Users)
- Pending approvals table
- Recent activity timeline

### 2. **AdminDashboard.cshtml.cs**
- PageModel (code-behind)
- Load data từ TourService
- Properties cho view binding
- Mock data cho activities

### 3. **Program.cs Updates**
- Added `builder.Services.AddRazorPages()`
- Added `app.MapRazorPages()`

## 🚀 Cách Sử Dụng

### Truy cập trang:
```
http://localhost:5000/AdminDashboard
```

### Route convention:
- File: `Pages/AdminDashboard.cshtml`
- URL: `/AdminDashboard`
- PageModel: `AdminDashboardModel`

## 📁 Cấu Trúc File

```
TripMate_Webapi/
├── Pages/
│   ├── AdminDashboard.cshtml          # View
│   └── AdminDashboard.cshtml.cs       # PageModel (code-behind)
├── Services/
│   └── TourService.cs                 # Data service
└── Program.cs                         # Razor Pages configuration
```

## 🎨 Features

### Server-Side Rendering
- Data được load từ server (TourService)
- No client-side API calls needed
- SEO friendly

### Data Binding
```csharp
// In PageModel
public string AdminName { get; set; } = "Admin";
public List<TourItem> PendingTours { get; set; } = new();

// In View
<p>@Model.AdminName</p>
@foreach (var tour in Model.PendingTours) { ... }
```

### Async Data Loading
```csharp
public async Task<IActionResult> OnGetAsync()
{
    var tours = await _tourService.GetToursAsync();
    PendingTours = tours.Take(3).ToList();
    return Page();
}
```

## 🔧 Customization

### Thêm trang mới:
1. Tạo file `Pages/NewPage.cshtml`
2. Tạo file `Pages/NewPage.cshtml.cs`
3. Access via `/NewPage`

### Thêm route parameters:
```csharp
@page "/AdminDashboard/{id}"

public class AdminDashboardModel : PageModel
{
    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Use id parameter
    }
}
```

### Thêm POST handler:
```csharp
public async Task<IActionResult> OnPostApproveAsync(int tourId)
{
    // Handle approval
    return RedirectToPage();
}
```

## 🆚 So Sánh: HTML vs Razor Pages

### HTML (Static)
```html
<!-- wwwroot/admin-dashboard.html -->
<p id="adminName">Admin</p>
<script>
    // Client-side API call
    fetch('/api/tours').then(...)
</script>
```

### Razor Pages (Server-Side)
```cshtml
<!-- Pages/AdminDashboard.cshtml -->
<p>@Model.AdminName</p>
@foreach (var tour in Model.PendingTours) {
    <div>@tour.Title</div>
}
```

## ✨ Advantages of Razor Pages

1. **Server-Side Rendering**: Faster initial load
2. **Type Safety**: C# models with IntelliSense
3. **Dependency Injection**: Easy service integration
4. **SEO Friendly**: Fully rendered HTML
5. **Security**: No exposed API endpoints needed
6. **Maintainability**: Separation of concerns (View + PageModel)

## 🔒 Security

### Add Authorization:
```csharp
[Authorize(Roles = "admin")]
public class AdminDashboardModel : PageModel
{
    // Only admins can access
}
```

### Check user in PageModel:
```csharp
public async Task<IActionResult> OnGetAsync()
{
    if (!User.IsInRole("admin"))
    {
        return Forbid();
    }
    return Page();
}
```

## 📊 Next Steps

1. **Add Authentication**: Integrate with Supabase JWT
2. **Add More Pages**: Bookings, Tours, Guides
3. **Add Forms**: Create/Edit tours
4. **Add Real-time**: SignalR for live updates
5. **Add Charts**: Chart.js integration

## 🐛 Troubleshooting

### Page not found (404)
- Check `app.MapRazorPages()` in Program.cs
- Check file naming: `AdminDashboard.cshtml` (PascalCase)
- Restart application

### Data not loading
- Check TourService is registered in DI
- Check async/await in OnGetAsync
- Check error logs

### Styling issues
- Tailwind CDN loaded in `<head>`
- Check browser console for errors
- Clear browser cache

## 📚 Resources

- [Razor Pages Documentation](https://learn.microsoft.com/en-us/aspnet/core/razor-pages/)
- [Tailwind CSS](https://tailwindcss.com/)
- [Material Symbols](https://fonts.google.com/icons)
