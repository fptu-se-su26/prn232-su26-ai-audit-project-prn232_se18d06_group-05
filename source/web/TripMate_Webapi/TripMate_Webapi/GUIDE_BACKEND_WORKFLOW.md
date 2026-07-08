# Hướng Dẫn Luồng Code (Backend Workflow) - TripMate

Để đảm bảo source code sạch sẽ, dễ bảo trì và không bị lộn xộn, mọi tính năng mới (đặc biệt là tính năng Create Tour và Bookings của Guide) phải tuân thủ nghiêm ngặt mô hình kiến trúc đang có của dự án (**Repository - Service Pattern**).

## Luồng Đi Của Dữ Liệu (Data Flow)
`View (HTML/Alpine.js)` -> `Controller` -> `DTO` -> `Service` -> `Repository` -> `Entity` -> `Database (Supabase)`

---

## 1. Tầng Presentation (Controllers & DTOs)
**Nhiệm vụ:** Nhận Request từ người dùng, điều phối xử lý và trả về kết quả (View hoặc JSON).
- **DTOs (`/DTOs`):** Các class dùng để nhận dữ liệu từ Form (Ví dụ: `CreateTourDto.cs`). 
  - *Lý do:* Không bao giờ dùng trực tiếp Entity để hứng dữ liệu từ Form vì rủi ro bảo mật (Over-posting) và dữ liệu không tương thích (như `IFormFile` cho hình ảnh).
- **Controllers (`/Controllers`):**
  - Ràng buộc quyền truy cập (Ví dụ kiểm tra Guide đã login chưa).
  - Validate dữ liệu cơ bản (kiểm tra `ModelState.IsValid`).
  - Lấy UserId từ Session/Claims.
  - Gọi hàm xử lý từ `Service`.
  - Tuyệt đối không viết các logic như tính toán giá tiền hay câu lệnh lưu DB ở đây.

## 2. Tầng Business Logic (Services)
**Nhiệm vụ:** Nơi chứa "chất xám" và nghiệp vụ của hệ thống. Xử lý logic, gọi các dịch vụ bên ngoài (như Cloudinary).
- **Services (`/Services`):** 
  - Xác thực nghiệp vụ (Business Rules). Ví dụ: Kiểm tra tour bị trùng tên, giá tiền hợp lệ.
  - Xử lý các tác vụ ngoài như gọi API Upload ảnh lên Cloudinary và lấy URL về.
  - Chuyển đổi (Mapping) từ `DTO` sang `Entity`.
  - Gọi tới `Repository` để lưu hoặc lấy dữ liệu.
  - *Tuyệt đối không tham chiếu tới `HttpContext` hay `IActionResult` ở tầng này.*

## 3. Tầng Data Access (Entities & Repositories)
**Nhiệm vụ:** Trực tiếp giao tiếp với Database (Supabase PostgreSQL).
- **Entities (`/Entities`):** Chứa các class map 1-1 với table trong DB (Ví dụ: `ExperiencePackageEntity`).
- **Repositories (`/Repositories`):** Nơi chứa các lệnh CRUD (Select, Insert, Update, Delete) qua `supabase-csharp`.
  - *Nguyên tắc:* Repository chỉ làm nhiệm vụ "Lưu" và "Lấy" data. Mọi thao tác tính toán, if/else phức tạp phải đẩy lên Service.
  - Interface hóa: Mỗi Repository đều nên implement từ một Interface (ví dụ: `IExperienceRepository`) để dễ Dependency Injection.

---

## Quy Trình Chuẩn Để Code Tính Năng "Create Tour"

Khi bắt tay vào code cho Form Create Tour vừa thiết kế, bạn cần follow đúng 4 bước sau (đi từ dưới lên hoặc từ trên xuống đều được, nhưng khuyên dùng từ dưới lên):

### Bước 1: Chuẩn bị DTO
Tạo file `CreateTourDto.cs` trong thư mục `DTOs`.
Khai báo các Property map chuẩn xác với các trường `<input name="...">` hoặc dữ liệu JSON mà Frontend gửi lên. (Ví dụ: `public IFormFile CoverImage { get; set; }`)

### Bước 2: Chuẩn bị Repository
Mở `IExperiencePackageRepository.cs` (nếu chưa có thì tạo mới) và thêm khai báo hàm:
`Task<ExperiencePackageEntity> CreatePackageAsync(ExperiencePackageEntity entity);`
Sau đó implement hàm này trong class `ExperiencePackageRepository.cs` gọi tới Supabase Client để Insert.

### Bước 3: Viết Logic tại Service
Mở `IExperienceService.cs` và `ExperienceService.cs`. Viết hàm `CreateTourAsync(CreateTourDto dto, string guideId)`.
Trong hàm này thực hiện:
1. Gọi `CloudinaryService` để upload `dto.CoverImage` và list `dto.GalleryImages`, lấy danh sách URL về.
2. Khởi tạo `new ExperiencePackageEntity()` và gán các URL vừa lấy cùng các thông tin từ `dto` sang.
3. Gọi `_repository.CreatePackageAsync(entity)` để lưu xuống DB.

### Bước 4: Hoàn thiện Controller
Mở `GuideController.cs`. 
Tạo Action `[HttpPost] public async Task<IActionResult> SubmitCreateTour([FromForm] CreateTourDto dto)`.
Lấy `guideId` từ session người dùng đang đăng nhập, sau đó gọi `await _experienceService.CreateTourAsync(dto, guideId)`. Trả về `Ok()` hoặc Redirect.

---
> Tuân thủ đúng luồng kiến trúc này, source code của bạn sẽ cực kỳ "sạch" (Clean), dễ dàng debug khi có lỗi, và sẵn sàng mở rộng sau này!
