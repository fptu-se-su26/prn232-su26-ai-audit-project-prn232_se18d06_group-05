# MVC Redesign Plan - Solar Concierge Style

> **📅 Last Updated**: May 30, 2026  
> **✅ Status**: Step 1-4 COMPLETED  
> **📄 Details**: See `MVC_STEP1_COMPLETED.md` for full documentation

## 🎯 Mục Tiêu
Tạo lại toàn bộ UI với:
- Design: Solar Concierge (màu cam #ff7a00)
- Logo: `/images/AVATAR.png`
- Architecture: MVC với .cshtml views
- Integration: Sử dụng Controllers hiện có

## 📁 Cấu Trúc Cần Tạo

```
Controllers/
├── HomeController.cs          # NEW - Home, About, Contact
├── AuthController.cs          # EXISTING - Login, Register
├── AdminController.cs         # EXISTING - Dashboard, Survey
└── TourController.cs          # EXISTING - Tours API

Views/
├── Home/
│   ├── Index.cshtml          # NEW - Home page
│   └── _Layout.cshtml        # NEW - Shared layout
├── Auth/
│   ├── Login.cshtml          # NEW - Login page
│   └── Register.cshtml       # NEW - Register page
└── Admin/
    ├── Dashboard.cshtml      # EXISTING
    └── Survey.cshtml         # EXISTING

wwwroot/
└── images/
    └── AVATAR.png            # Logo file
```

## ✅ Bước 1: Tạo HomeController - COMPLETED

```csharp
public class HomeController : Controller
{
    private readonly TourService _tourService;
    
    public async Task<IActionResult> Index()
    {
        var tours = await _tourService.GetToursAsync();
        return View(tours);
    }
}
```

**Status**: ✅ Done - File created at `Controllers/HomeController.cs`

## ✅ Bước 2: Tạo _Layout.cshtml (Shared) - COMPLETED

Chứa:
- Header với logo AVATAR.png
- Navigation
- Footer
- Tailwind CSS config

**Status**: ✅ Done - File created at `Views/Shared/_Layout.cshtml`

## ✅ Bước 3: Tạo Home/Index.cshtml - COMPLETED

Features:
- Hero section với search bar
- Category filters
- Featured tours grid
- Curated stays section

**Status**: ✅ Done - File created at `Views/Home/Index.cshtml`

## ✅ Bước 4: Tạo Auth Views - COMPLETED

### Login.cshtml
- Form với email/password
- POST đến `/api/auth/login`
- Lưu token vào localStorage
- Redirect based on role

**Status**: ✅ Done - File created at `Views/Auth/Login.cshtml`

### Register.cshtml
- Form với full name, email, password, role
- POST đến `/api/auth/register`
- Auto login sau register

**Status**: ✅ Done - File created at `Views/Auth/Register.cshtml`

### AuthController.cs
- MVC controller for serving auth views

**Status**: ✅ Done - File created at `Controllers/AuthController.cs`

## 🎨 Theme Colors

```css
primary: #ff7a00
on-primary: #ffffff
primary-container: #ffebd9
surface: #fbf9f8
on-surface: #1f1b18
```

## 📝 Next Steps

1. **Tạo HomeController.cs**
2. **Tạo Views/Shared/_Layout.cshtml**
3. **Tạo Views/Home/Index.cshtml**
4. **Tạo Views/Auth/Login.cshtml**
5. **Tạo Views/Auth/Register.cshtml**
6. **Update Program.cs** để set default route
7. **Copy AVATAR.png** vào wwwroot/images/

## 🔧 Program.cs Updates Needed

```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

## ⚠️ Important Notes

- Sử dụng `@model` để bind data từ Controller
- Form POST sử dụng JavaScript fetch() đến API endpoints
- Token management với localStorage
- Role-based redirects sau login
- Responsive design với Tailwind

## 📊 Integration với API

### Login Flow:
1. User submit form → JavaScript
2. POST `/api/auth/login` với credentials
3. Nhận token + user info
4. Save vào localStorage
5. Redirect based on role:
   - admin → `/Admin/Dashboard`
   - guide → `/Guide/Dashboard`
   - traveler → `/Home/Index`

### Tours Display:
1. Controller gọi `TourService.GetToursAsync()`
2. Pass data vào View
3. Render với Razor syntax
4. No client-side API calls needed

## 🎯 Ưu Điểm MVC Approach

✅ Server-side rendering (faster initial load)
✅ SEO friendly
✅ Type-safe với C# models
✅ Reuse existing services
✅ Clean separation of concerns
✅ Easy to maintain

Bạn muốn tôi bắt đầu tạo file nào trước?
