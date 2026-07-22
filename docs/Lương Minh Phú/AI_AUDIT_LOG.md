# AI Audit Log - Lương Minh Phú

## 1. Thông tin chung

| Thông tin | Nội dung |
|---|---|
| Tên bài tập / Project | TripMate - Tour Guide Booking Platform |
| Tên sinh viên | Lương Minh Phú |
| Ngày bắt đầu | 30/05/2026 |
| Ngày hoàn thành | 30/05/2026 |

---

## 2. Công cụ AI đã sử dụng

- [x] Claude (via Kiro)
- [ ] ChatGPT
- [ ] Gemini
- [ ] GitHub Copilot
- [ ] Cursor

---

## 3. Mục tiêu sử dụng AI

```text
Sử dụng AI để:
- Xây dựng ASP.NET Core MVC web application
- Tạo authentication system với role-based access
- Thiết kế UI components với Tailwind CSS
- Debug compilation errors và Razor syntax issues
- Tạo personality survey flow
- Xây dựng admin và guide dashboards
- Fix API endpoint errors
```

---

## 4. Nhật ký sử dụng AI chi tiết

### Lần sử dụng AI số 1 - Fix API 404 Error

| Nội dung | Thông tin |
|---|---|
| Ngày sử dụng | 30/05/2026 |
| Công cụ AI | Claude (via Kiro) |
| Mục đích sử dụng | Fix lỗi 404 cho /api/auth/login endpoint |
| Phần việc liên quan | Backend / Debug |
| Mức độ sử dụng | Sinh chính nội dung |

#### 4.1. Prompt đã sử dụng

```text
Failed to load resource: the server responded with a status of 404 (Not Found)
Login:302 Login error: SyntaxError: Failed to execute 'json' on 'Response': Unexpected end of JSON input
lỗi xảy ra sau khi đăng nhập
```

#### 4.2. Kết quả AI gợi ý

AI phát hiện không có API controller cho `/api/auth/login` và `/api/auth/register`. 
AI đã tạo:
- AuthApiController.cs với [ApiController] và [Route("api/auth")]
- POST endpoints cho login và register
- Cập nhật SupabaseAuthService để hỗ trợ role parameter
- Fix UserDto.Role thay vì UserMetadata

#### 4.3. Phần đã sử dụng từ AI

- Toàn bộ AuthApiController.cs
- Updates cho SupabaseAuthService.RegisterAsync() method
- Updates cho UpsertProfileAsync() method

#### 4.4. Phần đã chỉnh sửa

- Không có chỉnh sửa lớn, code AI generate đã hoạt động tốt
- Verify bằng getDiagnostics tool

#### 4.5. Minh chứng

| Loại minh chứng | Nội dung |
|---|---|
| File liên quan | Controllers/AuthApiController.cs, Services/SupabaseAuthService.cs |
| Kết quả | API endpoints hoạt động, login/register thành công |

#### 4.6. Nhận xét

Học được cách tạo API controller với proper routing attributes và integration với existing services.

---

### Lần sử dụng AI số 2 - Personality Survey

| Nội dung | Thông tin |
|---|---|
| Ngày sử dụng | 30/05/2026 |
| Công cụ AI | Claude (via Kiro) |
| Mục đích sử dụng | Tạo personality survey với UI đẹp |
| Phần việc liên quan | Frontend / Design |
| Mức độ sử dụng | Sinh chính nội dung |

#### 4.1. Prompt đã sử dụng

```text
giờ tiếp tục làm trang personality survey đi, UI cho bạn tùy biến
```

#### 4.2. Kết quả AI gợi ý

AI đã tạo:
- SurveyController.cs với Personality() và Results() actions
- Views/Survey/Personality.cshtml với 20 câu hỏi
- Views/Survey/Results.cshtml với profile summary
- Card-based UI với animations
- Progress bar tracking
- localStorage integration
- Personalized recommendations

#### 4.3. Phần đã sử dụng từ AI

- Toàn bộ survey structure và questions
- UI components với Tailwind CSS
- JavaScript logic cho navigation và submission
- Results page với recommendations engine

#### 4.4. Phần đã chỉnh sửa

- Không có chỉnh sửa, design và functionality đã phù hợp

#### 4.5. Minh chứng

| Loại minh chứng | Nội dung |
|---|---|
| File liên quan | Controllers/SurveyController.cs, Views/Survey/*.cshtml |
| Kết quả | Survey flow hoàn chỉnh với 20 questions, results page |

#### 4.6. Nhận xét

Học được cách thiết kế multi-step form với progress tracking và localStorage persistence.

---

### Lần sử dụng AI số 3 - Guide Dashboard

| Nội dung | Thông tin |
|---|---|
| Ngày sử dụng | 30/05/2026 |
| Công cụ AI | Claude (via Kiro) |
| Mục đích sử dụng | Tạo dashboard cho tour guides |
| Phần việc liên quan | Frontend / Backend |
| Mức độ sử dụng | Sinh chính nội dung |

#### 4.1. Prompt đã sử dụng

```text
làm thêm tour guide dashboard với UI tương tự như admin dashboard
```

#### 4.2. Kết quả AI gợi ý

AI đã tạo:
- GuideController.cs với Dashboard() action
- GuideDashboardViewModel với guide-specific metrics
- Views/Guide/Dashboard.cshtml với sidebar navigation
- Metrics cards: Earnings, Active Tours, Bookings, Rating
- Recent bookings table
- Activity timeline
- Same design style as Admin Dashboard

#### 4.3. Phần đã sử dụng từ AI

- Toàn bộ GuideController và view model
- Complete dashboard layout
- All UI components

#### 4.4. Phần đã chỉnh sửa

- Đổi tên BookingItem thành GuideBookingItem để tránh conflict với AdminController
- Verify không có duplicate class definitions

#### 4.5. Minh chứng

| Loại minh chứng | Nội dung |
|---|---|
| File liên quan | Controllers/GuideController.cs, Views/Guide/Dashboard.cshtml |
| Kết quả | Guide dashboard hoạt động với metrics và bookings |

#### 4.6. Nhận xét

Học được cách reuse design patterns và adapt cho different user roles.

---

### Lần sử dụng AI số 4 - Logout Functionality

| Nội dung | Thông tin |
|---|---|
| Ngày sử dụng | 30/05/2026 |
| Công cụ AI | Claude (via Kiro) |
| Mục đích sử dụng | Thêm nút đăng xuất vào header |
| Phần việc liên quan | Frontend |
| Mức độ sử dụng | Hỗ trợ một phần |

#### 4.1. Prompt đã sử dụng

```text
giúp tôi thêm nút đăng xuất tạm thời ở headbar ở trang home, bị bug rồi
```

#### 4.2. Kết quả AI gợi ý

AI đã:
- Thêm logout button vào _Layout.cshtml
- Tạo logout() JavaScript function
- Clear tất cả localStorage items
- Redirect về login page
- Fix Razor syntax error với @ character

#### 4.3. Phần đã sử dụng từ AI

- Logout button HTML
- JavaScript logout function
- Fix cho @ character issue (dùng String.fromCharCode(64))

#### 4.4. Phần đã chỉnh sửa

- Không có, solution đã hoạt động tốt

#### 4.5. Minh chứng

| Loại minh chứng | Nội dung |
|---|---|
| File liên quan | Views/Shared/_Layout.cshtml |
| Kết quả | Logout button hiển thị và hoạt động đúng |

#### 4.6. Nhận xét

Học được cách xử lý special characters trong Razor views và localStorage management.

---

## 5. Bảng tổng hợp mức độ sử dụng AI

| Hạng mục | Không dùng AI | AI hỗ trợ ít | AI hỗ trợ nhiều | AI sinh chính | Ghi chú |
|---|:---:|:---:|:---:|:---:|---|
| Thiết kế kiến trúc hệ thống |  |  | ✓ |  | MVC structure |
| Thiết kế giao diện |  |  |  | ✓ | Solar Concierge theme |
| Code frontend |  |  |  | ✓ | Views, JavaScript |
| Code backend |  |  |  | ✓ | Controllers, Services |
| Debug lỗi |  |  |  | ✓ | Fix 404, Razor syntax |
| Tối ưu code |  |  | ✓ |  | Refactoring |

---

## 6. Các lỗi hoặc hạn chế từ AI

| STT | Lỗi/hạn chế từ AI | Cách phát hiện | Cách xử lý/cải tiến |
|---:|---|---|---|
| 1 | Duplicate ActivityItem class | Compilation error | Đổi tên thành GuideBookingItem |
| 2 | Razor @ character issue | Syntax error | Dùng String.fromCharCode(64) |
| 3 | TourDto vs TourRow confusion | Type error | Verify và fix tên class |
| 4 | Lỗi không submit được form vì tab ẩn | Silent HTML5 validation | Đổi sang JS validation và bỏ thuộc tính required |

---

## 7. Kiểm chứng kết quả AI

```text
Tất cả code được kiểm chứng bằng:
- getDiagnostics tool để check compilation errors
- dotnet build để verify build success
- Manual testing trong browser
- Verify API endpoints với Network tab
- Check localStorage data
- Test role-based redirects
```

---

## 9. Reflection cuối bài

### 9.1. AI đã hỗ trợ ở điểm nào?

```text
AI đã hỗ trợ rất nhiều trong:
- Generate boilerplate code cho MVC structure
- Thiết kế UI components với Tailwind CSS
- Debug các lỗi compilation và syntax
- Tạo comprehensive personality survey
- Xây dựng dashboards với metrics và charts
- Fix API endpoint issues
- Implement authentication flow
```

### 9.2. Phần nào không sử dụng theo gợi ý của AI?

```text
Hầu hết gợi ý của AI đều được sử dụng vì chất lượng tốt.
Chỉ có một số chỗ cần đổi tên class để tránh conflict.
```

### 9.3. Đã kiểm tra tính đúng đắn như thế nào?

```text
- Sử dụng getDiagnostics tool
- Build project với dotnet build
- Test trong browser
- Verify API responses
- Check localStorage data
- Test tất cả user flows
```

### 9.4. Nếu không có AI, phần nào sẽ khó khăn nhất?

```text
- Thiết kế UI components từ đầu sẽ mất nhiều thời gian
- Debug Razor syntax errors sẽ khó hơn
- Tạo 20 câu hỏi survey với UI đẹp
- Implement dashboard layouts với metrics
```

### 9.5. Học được gì về môn học?

```text
- ASP.NET Core MVC pattern và cách tổ chức code
- Razor syntax và cách integrate với JavaScript
- Role-based authentication và authorization
- RESTful API design
- UI/UX design với Tailwind CSS
- localStorage và client-side state management
```

### 9.6. Học được gì về cách sử dụng AI có trách nhiệm?

```text
- Luôn verify code AI generate bằng tools
- Hiểu code trước khi sử dụng
- Test kỹ functionality
- Ghi nhận AI đã hỗ trợ phần nào
- Không copy-paste mù quáng
- Sử dụng AI như một tool hỗ trợ, không phải thay thế hoàn toàn
```

---

## 10. Cam kết học thuật

Sinh viên cam kết rằng:

- Nội dung AI hỗ trợ đã được ghi nhận trung thực.
- Không nộp nguyên văn kết quả AI mà không kiểm tra.
- Có khả năng giải thích các phần đã nộp.
- Chịu trách nhiệm về tính đúng đắn của sản phẩm cuối cùng.

| Sinh viên | Ngày xác nhận |
|---|---|
| Lương Minh Phú | 30/05/2026 |
