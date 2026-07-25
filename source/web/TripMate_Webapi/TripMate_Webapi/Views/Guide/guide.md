# PROMPT THIẾT KẾ CHI TIẾT — TRIPMATE LOCAL GUIDE PORTAL
> Phiên bản: v1.0 | Phạm vi: 9 trang dành cho role Local Guide | Tháng 6, 2026

---

## 📌 PHẦN 0 — BỐI CẢNH DỰ ÁN & YÊU CẦU CHUNG

### 0.1 Tổng quan sản phẩm
TripMate là nền tảng kết nối Traveler (khách du lịch) với Local Guide (người hướng dẫn bản địa) dựa trên thuật toán Matching theo phong cách sống. Mục tiêu của portal Local Guide là cung cấp bộ công cụ chuyên nghiệp giúp Guide **thương mại hóa kiến thức bản địa**, **quản lý lịch trình** và **theo dõi thu nhập** một cách trực quan, tối giản, hiệu quả.

### 0.2 Tech Stack bắt buộc
- **Razor Pages** (.cshtml) — server-side rendering, layout nesting
- **Tailwind CSS** — utility-first, không dùng custom CSS ngoài trừ animation/charts
- **Alpine.js** — reactive state management trong DOM (x-data, x-show, x-bind, x-on)
- **ASP.NET Core SignalR** — real-time: booking requests, chat, notifications
- **Bootstrap Icons (bi-)** — icon system xuyên suốt
- **Chart.js** — biểu đồ thu nhập (Earnings Report)
- **FullCalendar.js** — calendar availability (Calendar page)
- **Dropzone.js** — upload ảnh (Profile Setting)

Typography:
  - Heading: font-bold, tracking-tight, text-gray-900
  - Body: text-sm text-gray-600
  - Label: text-xs font-medium text-gray-500 uppercase tracking-wider
  - Code/ID: font-mono text-xs bg-gray-100

Spacing scale: p-4 (16px) cho card padding, gap-6 (24px) cho grid gaps
Border radius: rounded-xl cho cards, rounded-lg cho buttons, rounded-full cho badges/avatars
Shadow: shadow-sm cho cards mặc định, shadow-lg cho modals/dropdowns
```

### 0.4 Booking Status Color Mapping (dùng xuyên suốt)
```
Status 0 - Pending   → bg-amber-100  text-amber-700  bi-clock
Status 1 - Confirmed → bg-green-100  text-green-700  bi-check-circle
Status 2 - Completed → bg-gray-100   text-gray-600   bi-check2-all
Status 3 - Cancelled → bg-red-100    text-red-700    bi-x-circle
```

---

## 📐 PHẦN 1 — LAYOUT CHUNG (SHARED SHELL)

### 1.1 Cấu trúc tổng thể (Two-column layout)
```
┌──────────────────────────────────────────────────────────┐
│  SIDEBAR (w-64, fixed, bg-slate-800)                      │
│  ┌────────────────────────────────────────────────────┐  │
│  │ [Logo TripMate]                                     │  │
│  │ [Guide Avatar + Name + Verified Badge]              │  │
│  │ ─────────────────────────────────────               │  │
│  │ • Dashboard                                         │  │
│  │ • Bookings          [badge: pending count]          │  │
│  │ • Calendar                                          │  │
│  │ • My Tours                                          │  │
│  │ • Messages          [badge: unread count]           │  │
│  │ • Notifications     [badge: unread count]           │  │
│  │ • Profile Setting                                   │  │
│  │ • Earnings Report                                   │  │
│  │ • Support                                           │  │
│  │ ─────────────────────────────────────               │  │
│  │ [Logout button]                                     │  │
│  └────────────────────────────────────────────────────┘  │
│                                                           │
│  MAIN CONTENT AREA (flex-1, ml-64, bg-gray-50)           │
│  ┌────────────────────────────────────────────────────┐  │
│  │ TOPBAR (sticky, bg-white, border-b, shadow-sm)     │  │
│  │ [Page title]          [Search] [Notif bell] [Avatar]│  │
│  ├────────────────────────────────────────────────────┤  │
│  │                                                     │  │
│  │  PAGE CONTENT (p-6)                                 │  │
│  │                                                     │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
```

### 1.2 Sidebar Navigation Item — Component
Mỗi nav item có 3 state: **default**, **active**, **hover**.
```html
<!-- Nav Item Pattern (Alpine.js + Tailwind) -->
<a href="/guide/dashboard"
   class="flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium
          text-slate-300 hover:bg-slate-700 hover:text-white transition-colors
          [&.active]:bg-blue-600 [&.active]:text-white">
  <i class="bi bi-speedometer2 text-lg w-5"></i>
  <span>Dashboard</span>
  <!-- Badge (chỉ hiện khi có data) -->
  <span class="ml-auto bg-amber-500 text-white text-xs rounded-full px-2 py-0.5"
        x-show="pendingCount > 0" x-text="pendingCount"></span>
</a>
```

### 1.3 Topbar — Component
```
[☰ Menu toggle (mobile)]  [Page Title h1]     [────────────]  [🔔 Bell+badge]  [Avatar dropdown]
```
- Bell icon: hiển thị unread notification count (SignalR realtime update)
- Avatar dropdown: link đến Profile Setting + Logout

### 1.4 SignalR Global Initialization (Shared Layout)
```javascript
// _Layout.cshtml — SignalR khởi tạo 1 lần, inject vào window
const notificationConn = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/notifications", { accessTokenFactory: () => window.__authToken })
  .withAutomaticReconnect()
  .build();

notificationConn.on("NewBookingRequest",  (payload) => { /* update badge + show modal */ });
notificationConn.on("BookingConfirmed",   (payload) => { /* toast notify */ });
notificationConn.on("BookingCompleted",   (payload) => { /* toast + refresh earnings */ });
notificationConn.start();
```

---

## 📄 TRANG 1 — DASHBOARD (`/guide/dashboard`)

### Mục đích
Trang tổng quan — Guide mở app thấy ngay: thu nhập hôm nay, booking đang chờ duyệt, lịch gần nhất, và thông báo chưa đọc. **Không cần navigate sang trang khác cho các tác vụ khẩn cấp.**

### Layout Grid
```
┌─────────────────────────────────────────────────────┐
│  [Greeting: "Xin chào, Minh! ☀️ Thứ 6, 12/6/2026"] │
│  [Subtitle: "Bạn có 2 yêu cầu đặt lịch cần phản hồi"]│
├──────────┬──────────┬──────────┬──────────────────────┤
│ STAT 1   │ STAT 2   │ STAT 3   │ STAT 4               │
│ Thu nhập │ Tours    │ Đánh giá │ Tỷ lệ                │
│ tháng này│ hoàn thành│ trung bình│ chấp nhận            │
├──────────┴──────────┴──────────┴──────────────────────┤
│  PENDING BOOKINGS (real-time)    │  UPCOMING SCHEDULE  │
│  [List: 2-3 booking card]        │  [Next 7 days list] │
│  [Xem tất cả → /guide/bookings]  │  [→ /guide/calendar]│
├──────────────────────────────────┴─────────────────────┤
│  RECENT MESSAGES                 │  QUICK ACTIONS      │
│  [3 latest chat threads]         │  [+ Tạo Tour mới]   │
│  [→ /guide/messages]             │  [✏️ Cập nhật lịch] │
│                                  │  [💰 Xem thu nhập]  │
└──────────────────────────────────┴─────────────────────┘
```

### Components chi tiết

#### A. Stats Cards Row (4 cards)
Mỗi card: `rounded-xl bg-white shadow-sm p-5 flex items-start justify-between`
```
┌────────────────────────────┐
│ 💰 Thu nhập tháng này       │
│                             │
│  4,250,000đ                 │  ← text-2xl font-bold text-gray-900
│  ↑ +12% so tháng trước      │  ← text-xs text-green-600
└────────────────────────────┘
```
| Card | Icon | Metric | Data source |
|---|---|---|---|
| Thu nhập tháng | bi-currency-dollar | Sum(GuideEarnings) tháng hiện tại | LedgerEntry aggregate |
| Tours hoàn thành | bi-check2-all | Count(Status=2) | Bookings |
| Đánh giá TB | bi-star-fill | AverageRating (0-5) | GuideProfile |
| Tỷ lệ chấp nhận | bi-graph-up | Confirmed/(Pending+Confirmed)% | Bookings ratio |

#### B. Pending Bookings Panel
- Tiêu đề: "Đang chờ phản hồi" + badge số lượng (bg-amber-500)
- Mỗi item là một **Booking Request Mini-card**:
```
┌─────────────────────────────────────────────────────┐
│ [Avatar 40px]  Nguyễn Văn A              ⏱ 18:45:22 │
│                📦 Bình minh Mỹ Sơn                  │
│                📅 15/06/2026  👥 2 người  💰 850,000đ│
│                [Từ chối] (outline-red)  [Chấp nhận] (green) │
└─────────────────────────────────────────────────────┘
```
- **Countdown timer** hiển thị thời gian còn lại trong 24h (Alpine.js `setInterval`)
- Khi timer = 0 → card tự xám + label "Đã hết hạn"
- Nút **Chấp nhận**: `PATCH /api/booking/{id}/confirm` → toast "Đã xác nhận!" + remove card + update badge
- Nút **Từ chối**: mở inline confirm modal nhỏ "Bạn chắc chắn muốn từ chối?" → `PATCH /api/booking/{id}/cancel`
- SignalR `NewBookingRequest` → card mới xuất hiện với animation `slide-in-from-top`

#### C. Upcoming Schedule (7 ngày tới)
- List dọc, mỗi item: `[ngày] [tên traveler] [tên tour] [status badge]`
- Ngày hôm nay: highlight `border-l-4 border-blue-500 bg-blue-50`
- Empty state: "Không có lịch trong 7 ngày tới. Cập nhật lịch rảnh của bạn!"

#### D. Recent Messages
- 3 thread gần nhất, mỗi thread: avatar + tên + tin nhắn cuối + thời gian + unread dot
- Unread dot: `w-2 h-2 rounded-full bg-blue-600`

#### E. Quick Actions (3 nút lớn)
```
[+ Tạo Tour mới]     → /guide/my-tours/create
[📅 Cập nhật lịch]   → /guide/calendar
[💰 Xem thu nhập]    → /guide/earnings
```

### States
- **Loading**: skeleton shimmer cho cả 4 stat cards (Tailwind animate-pulse)
- **Empty (guide mới)**: Hero prompt "Hoàn thiện hồ sơ của bạn để bắt đầu nhận khách!"
  với progress checklist: Avatar ✓ / Bio / Hidden Gems / Tạo gói tour / Lịch rảnh

---

## 📄 TRANG 2 — BOOKINGS (`/guide/bookings`)

### Mục đích
Quản lý toàn bộ booking lifecycle — xem, lọc, phản hồi, tra cứu chi tiết. Đây là trang **vận hành trung tâm** của Guide.

### Layout
```
┌─────────────────────────────────────────────────────┐
│  [Tab: Tất cả | Chờ duyệt (2) | Đã xác nhận | Hoàn thành | Đã hủy] │
├─────────────────────────────────────────────────────┤
│  [🔍 Search by traveler name / booking ID]          │
│  [📅 Date range picker]  [↕ Sort: Mới nhất / Cũ nhất]│
├─────────────────────────────────────────────────────┤
│  BOOKING LIST (paginated 10 items/page)             │
│  ─────────────────────────────────────────────────  │
│  [Booking Card 1]                                   │
│  [Booking Card 2]                                   │
│  ...                                                │
│  [Pagination: ← 1 2 3 →]                           │
└─────────────────────────────────────────────────────┘
```

### Booking Card Component
```
┌────────────────────────────────────────────────────────────────────┐
│ #BK-2024-001A                        🟡 CHỜ DUYỆT  ⏱ 20:12:05 còn │
├────────────────────────────────────────────────────────────────────┤
│ [Avatar]  Trần Thị Bảo Châu                        850,000đ        │
│           📦 Bình minh Mỹ Sơn + Ẩm thực địa phương   +bạn nhận 85%│
│           📅 Thứ 7, 15/06/2026  🕗 06:00   👥 2 người              │
│           💬 "Chúng tôi muốn chụp ảnh hoàng hôn"                  │
├────────────────────────────────────────────────────────────────────┤
│  [Xem chi tiết ↗]    [Từ chối]    [✓ Chấp nhận]                   │
└────────────────────────────────────────────────────────────────────┘
```
- Với Status = Confirmed: nút → [Mở Chat] [Xem chi tiết]
- Với Status = Completed: nút → [Xem đánh giá] [Xem receipt]
- Với Status = Cancelled: nút → [Xem lý do]

### Booking Detail Modal (Slide-over panel từ phải)
Khi click "Xem chi tiết" → slide-over panel width `w-[480px]` xuất hiện từ phải:
```
┌─────────────────────────────────────────────┐
│ ✕  Chi tiết Booking #BK-2024-001A           │
├─────────────────────────────────────────────┤
│ THÔNG TIN KHÁCH                             │
│ [Avatar lớn 56px] Trần Thị Bảo Châu        │
│ ⭐ 4.8 (12 đánh giá)  📍 TP.HCM            │
│ [Nhắn tin]                                  │
├─────────────────────────────────────────────┤
│ CHI TIẾT ĐẶT LỊCH                          │
│ Gói: Bình minh Mỹ Sơn + Ẩm thực           │
│ Ngày: Thứ 7, 15/06/2026                     │
│ Giờ bắt đầu: 06:00 AM                      │
│ Số khách: 2 người                           │
│ Ghi chú: "Chúng tôi muốn chụp ảnh..."     │
├─────────────────────────────────────────────┤
│ THANH TOÁN                                  │
│ Tổng tiền:     1,000,000đ                   │
│ Phí nền tảng:   -150,000đ (15%)             │
│ Bạn nhận:       850,000đ                   │
│ Phương thức: VietQR ✓ Đã thanh toán        │
├─────────────────────────────────────────────┤
│ TIMELINE TRẠNG THÁI                         │
│ ✓ Đặt lịch   13/06 10:23                   │
│ ◉ Đang chờ duyệt                           │
│ ○ Xác nhận                                 │
│ ○ Hoàn thành                               │
├─────────────────────────────────────────────┤
│ [Từ chối (outline)]     [✓ Chấp nhận (green)]│
└─────────────────────────────────────────────┘
```

### States
- **Pending tab + Timer**: countdown Alpine `x-text` + auto-remove khi expire
- **Empty tab**: "Chưa có booking nào. Hồ sơ của bạn đang hoạt động — hãy chờ khách tìm đến!"
- **Real-time**: SignalR `NewBookingRequest` → toast + badge update + insert card vào top of Pending tab

---

## 📄 TRANG 3 — CALENDAR (`/guide/calendar`)

### Mục đích
Guide tự quản lý ngày **không nhận tour** — click chọn ngày bận, hệ thống tự động loại Guide khỏi kết quả Matching vào những ngày đó.

### Layout
```
┌──────────────────────────────────────────────────────────┐
│  HƯỚNG DẪN SỬ DỤNG (collapsible banner, lần đầu login)  │
│  "Click vào ngày muốn chặn. Click lại để bỏ chặn."       │
├──────────────────────────────────────────────────────────┤
│  LEGEND:                                                  │
│  🟢 Sẵn sàng   🔴 Đã chặn   🟡 Đã có booking            │
├───────────────────────────────┬──────────────────────────┤
│  FULLCALENDAR.JS              │  PANEL BÊN PHẢI          │
│  (monthly view, flex-grow)    │                          │
│                               │  THÁNG NÀY               │
│  [< Tháng trước] [Tháng 6]    │  Ngày bận: 5             │
│  [> Tháng sau]                │  Ngày rảnh: 20           │
│                               │  Booking đặt: 3          │
│  T2 T3 T4 T5 T6 T7 CN         │                          │
│  [  ][  ][🔴][  ][🟡][  ][  ]│  NGÀY ĐÃ CHẶN            │
│  [  ][🔴][  ][  ][  ][🟡][  ]│  > 15/06 - Cá nhân       │
│  ...                          │  > 18/06 - Bận           │
│                               │  > 22/06 - Đã đặt riêng  │
│  [Lưu thay đổi]               │  [Bỏ chặn tất cả]        │
└───────────────────────────────┴──────────────────────────┘
```

### FullCalendar.js Configuration
```javascript
const calendar = new FullCalendar.Calendar(calendarEl, {
  initialView: 'dayGridMonth',
  locale: 'vi',
  // Custom day cell render
  dayCellDidMount: (info) => {
    const dateStr = info.date.toISOString().split('T')[0];
    if (blockedDates.includes(dateStr))
      info.el.classList.add('fc-blocked-day');      // bg-red-100
    if (bookedDates.includes(dateStr))
      info.el.classList.add('fc-booked-day');       // bg-amber-100
  },
  // Click to toggle blocked date
  dateClick: (info) => {
    if (bookedDates.includes(info.dateStr)) {
      showToast('warning', 'Ngày này đã có booking, không thể chặn');
      return;
    }
    toggleBlockedDate(info.dateStr);  // Alpine state update
  },
  // Custom CSS
  height: 'auto',
});
```

### Interaction Flow
1. Guide click vào ngày trống → ngày chuyển sang màu đỏ nhạt + thêm vào `pendingChanges[]`
2. Guide click vào ngày đỏ → bỏ chặn, remove khỏi `pendingChanges[]`
3. Click **"Lưu thay đổi"** → `POST /api/guide/availability` với body `{ dates: pendingChanges[] }`
4. Toast xác nhận: "Đã cập nhật lịch. Bạn sẽ không nhận booking vào X ngày đã chặn."
5. **Ngày có booking**: màu vàng nhạt, click hiện tooltip "Đã có booking — xem tại /bookings"

### States
- **Unsaved changes**: banner sticky bottom "Bạn có thay đổi chưa lưu. [Lưu ngay] [Hủy]"
- **Mobile**: chuyển từ monthly sang weekly view tự động (FullCalendar responsive)

---

## 📄 TRANG 4 — MY TOURS (`/guide/my-tours`)

### Mục đích
CRUD toàn bộ ExperiencePackage — Guide tạo, chỉnh sửa, bật/tắt, xóa các gói trải nghiệm của mình. Tối đa 10 gói/guide (MVP).

### Layout
```
┌──────────────────────────────────────────────────────┐
│  MY TOURS   [5/10 gói đang dùng]     [+ Tạo gói mới]│
│  ─────────────────────────────────────────────────── │
│  [🔍 Tìm kiếm theo tên gói]  [Filter: Hoạt động/Ẩn] │
├──────────────────────────────────────────────────────┤
│  TOUR CARD 1                        [HOẠT ĐỘNG 🟢]   │
│  ┌──────────────────────────────────────────────┐    │
│  │ Bình minh Mỹ Sơn + Ẩm thực địa phương       │    │
│  │ ⏱ 4.5 giờ  |  👥 Tối đa 6 người             │    │
│  │ 💰 1,200,000đ / buổi                          │    │
│  │ 🏷 #food #culture #hidden-gems               │    │
│  │ 📊 12 lượt đặt  |  ⭐ 4.9                    │    │
│  │ [Chỉnh sửa]  [Ẩn gói]  [Xóa]                │    │
│  └──────────────────────────────────────────────┘    │
│  TOUR CARD 2  ...                                     │
└──────────────────────────────────────────────────────┘
```

### Create / Edit Tour — Full Page Form (`/guide/my-tours/create` hoặc `/guide/my-tours/{id}/edit`)
```
┌──────────────────────────────────────────────────────┐
│  ← Quay lại   TẠO GÓI TRẢI NGHIỆM MỚI              │
├──────────────────────────────────────────────────────┤
│  THÔNG TIN CƠ BẢN                                    │
│  ┌────────────────────────────────────────────────┐  │
│  │ Tên gói *                                      │  │
│  │ [Bình minh Mỹ Sơn + Ẩm thực địa phương...]    │  │
│  │ VD: "Khám phá phố cổ Hội An về đêm"           │  │
│  ├──────────────────┬─────────────────────────────┤  │
│  │ Thời lượng *     │ Số khách tối đa *            │  │
│  │ [4.5]  giờ       │ [6]  người                  │  │
│  ├──────────────────┴─────────────────────────────┤  │
│  │ Mô tả chi tiết *                               │  │
│  │ [Textarea rich: mô tả lộ trình, điểm dừng,    │  │
│  │  giá trị mang lại cho khách — tối đa 2000 ký tự] │
│  └────────────────────────────────────────────────┘  │
│                                                       │
│  ĐỊNH GIÁ                                            │
│  ┌────────────────────────────────────────────────┐  │
│  │ Giá mỗi buổi *                Giá theo người   │  │
│  │ [1,200,000]  VND              [Optional] VND    │  │
│  │ Doanh thu thực nhận: ~1,020,000đ (sau 15% phí) │  │
│  └────────────────────────────────────────────────┘  │
│                                                       │
│  DỊCH VỤ ĐÃ BAO GỒM                                 │
│  ┌────────────────────────────────────────────────┐  │
│  │ + Thêm dịch vụ  [xe máy] [ăn trưa] [vé vào]   │  │
│  │ [x] Xe máy / Xe đạp      [x] Ăn sáng           │  │
│  │ [x] Nước uống             [ ] Vé tham quan      │  │
│  │ Custom: [_________________] [+ Thêm]            │  │
│  └────────────────────────────────────────────────┘  │
│                                                       │
│  TAGS TÌM KIẾM                                       │
│  ┌────────────────────────────────────────────────┐  │
│  │ [food ×] [culture ×] [hidden-gems ×]           │  │
│  │ Gợi ý: [adventure] [photography] [nightlife]   │  │
│  └────────────────────────────────────────────────┘  │
│                                                       │
│  [Lưu nháp]   [Đăng gói trải nghiệm →]              │
└──────────────────────────────────────────────────────┘
```

### Validation Rules
- Tên gói: required, 10–200 ký tự
- Thời lượng: 0.5–24 giờ, bước 0.5
- Số khách: 1–20 người
- Giá: ≥ 50,000 VND
- Mô tả: required, min 100 ký tự
- Tag: tối đa 5 tags

### States
- **Limit reached (10/10)**: banner warning "Bạn đã đạt giới hạn 10 gói. Ẩn hoặc xóa gói cũ để tạo mới."
- **Empty**: illustration + "Tạo gói trải nghiệm đầu tiên của bạn"
- **Tour bị ẩn**: card có overlay mờ + badge "ĐÃ ẨN"

---

## 📄 TRANG 5 — MESSAGES (`/guide/messages`)

### Mục đích
Nhắn tin trực tiếp với Traveler. Chat chỉ mở khi booking Status = Confirmed. Giao diện dạng messenger hai cột.

### Layout
```
┌────────────────────────────────────────────────────────────┐
│  MESSAGES                           [🔍 Tìm cuộc trò chuyện]│
├────────────────────────┬───────────────────────────────────┤
│  CONVERSATION LIST     │   CHAT WINDOW                      │
│  (w-80, border-r)      │   (flex-1)                        │
│  ─────────────────     │   ────────────────────────────────│
│  [Thread 1]            │   HEADER                          │
│  [Avatar] Bảo Châu     │   [← Back(mobile)] [Avatar 40px]  │
│  "Hẹn gặp lúc 6h nhé"  │   Trần Thị Bảo Châu              │
│  2 phút trước  [🔵 2]  │   📦 Bình minh Mỹ Sơn            │
│  ─────────────────     │   📅 Thứ 7, 15/06/2026            │
│  [Thread 2]            │   ────────────────────────────────│
│  [Avatar] Hùng Anh     │   MESSAGE AREA (scroll)           │
│  "Cảm ơn bạn rất nhiều"│                                   │
│  Hôm qua               │   [bubble: Xin chào Guide Minh!] │
│  ─────────────────     │   [bubble: Chào bạn! Mình đã...] │
│  [Thread 3 - Locked]   │   [message history]               │
│  [🔒] Booking chưa xác │   ────────────────────────────────│
│  nhận                  │   INPUT BAR                       │
│                        │   [📎][Nhập tin nhắn...] [➤ Gửi] │
└────────────────────────┴───────────────────────────────────┘
```

### Message Bubble Component
```
Guide (bên phải, bg-blue-600 text-white, rounded-2xl rounded-br-sm)
Traveler (bên trái, bg-white border, rounded-2xl rounded-bl-sm)

Mỗi bubble: text + timestamp nhỏ bên dưới + read receipt (✓ / ✓✓ blue)
```

### Chat Interaction Features
- **Read receipt**: ✓✓ màu xanh khi `IsRead = true` (SignalR `MessagesRead`)
- **Infinite scroll**: load 50 message gần nhất, scroll lên load thêm (cursor-based pagination)
- **Locked chat**: Thread có booking Pending hiển thị overlay "Chat sẽ mở sau khi bạn xác nhận booking"

### Real-time Integration
```javascript
// ChatHub events
chatConn.on("ReceiveMessage", (msg) => {
  appendMessage(msg);
  markAsRead(msg.bookingId, msg.senderId);
});
chatConn.on("MessagesRead", ({ bookingId }) => {
  updateReadReceipts(bookingId);
});
```

### States
- **Empty (no conversations)**: "Khi booking được xác nhận, kênh chat sẽ tự động mở. Chấp nhận booking để bắt đầu trò chuyện!"
- **Mobile**: ẩn conversation list, hiện full-screen chat khi chọn thread

---

## 📄 TRANG 6 — NOTIFICATIONS (`/guide/notifications`)

### Mục đích
Trung tâm thông báo — lưu trữ tất cả sự kiện hệ thống: booking mới, xác nhận, hoàn thành, thanh toán, đánh giá, thông báo từ Admin.

### Layout
```
┌──────────────────────────────────────────────────────┐
│  THÔNG BÁO        [Đánh dấu tất cả đã đọc]  [Lọc ▾] │
│  Filter: [Tất cả ●] [Booking] [Thanh toán] [Hệ thống]│
├──────────────────────────────────────────────────────┤
│  HÔM NAY                                             │
│  ┌──────────────────────────────────────────────┐    │
│  │ 🟡 [●] Booking mới từ Trần Thị Bảo Châu      │    │
│  │     Đặt "Bình minh Mỹ Sơn" vào 15/06/2026    │    │
│  │     Phản hồi trong 24h  ·  10 phút trước      │    │
│  │     [Xem booking →]                           │    │
│  └──────────────────────────────────────────────┘    │
│  ┌──────────────────────────────────────────────┐    │
│  │ 💰 [●] Thanh toán đã được giải ngân           │    │
│  │     850,000đ từ booking #BK-2024-008 đã về ví│    │
│  │     1 giờ trước                              │    │
│  └──────────────────────────────────────────────┘    │
│                                                       │
│  HÔM QUA                                             │
│  ┌──────────────────────────────────────────────┐    │
│  │ ⭐ Đánh giá mới 5 sao từ Nguyễn Văn An       │    │
│  │     "Guide rất nhiệt tình, rất recommend!"    │    │
│  │     Hôm qua 14:23                            │    │
│  └──────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────┘
```

### Notification Types & Icons
| Type | Icon | Color | Action |
|---|---|---|---|
| Booking mới | bi-bell-fill | amber | → /guide/bookings?id=xxx |
| Booking đã xác nhận | bi-check-circle | green | → /guide/bookings?id=xxx |
| Booking hoàn thành | bi-check2-all | gray | → /guide/bookings?id=xxx |
| Booking bị hủy | bi-x-circle | red | → /guide/bookings?id=xxx |
| Thanh toán giải ngân | bi-currency-dollar | green | → /guide/earnings |
| Đánh giá mới | bi-star-fill | amber | → /guide/profile |
| Admin verified | bi-patch-check | blue | → /guide/profile |
| Tin nhắn mới | bi-chat-dots | blue | → /guide/messages |

### States
- **Unread**: `bg-blue-50 border-l-4 border-blue-500` + blue dot `●`
- **Read**: `bg-white` không có border-left
- **Empty**: "Không có thông báo nào. Chúng tôi sẽ thông báo khi có booking mới!"

---

## 📄 TRANG 7 — PROFILE SETTING (`/guide/profile`)

### Mục đích
Guide xây dựng và cập nhật hồ sơ công khai — đây là "shop window" của họ trên TripMate. Bao gồm thông tin cá nhân, chuyên môn, ảnh Hidden Gems và định giá cơ bản.

### Layout — Tab-based Form
```
┌──────────────────────────────────────────────────────────┐
│  HỒ SƠ CỦA TÔI                                           │
│                                                           │
│  [Xem hồ sơ công khai ↗]       [Đã xác minh ✓] / [Chờ duyệt ⏳]│
├──────────────────────────────────────────────────────────┤
│  LIVE PREVIEW (right panel, desktop only)                │
│  ┌──────────────┐  ┌────────────────────────────────┐   │
│  │ FORM (left,  │  │ PREVIEW CARD (right, w-80)     │   │
│  │ flex-grow)   │  │ [Như Traveler thấy trên app]   │   │
│  │              │  │                                │   │
│  │ [Tabs]       │  │ [Avatar] [Tên] ✓               │   │
│  │              │  │ ⭐ 4.9  📍 Hội An              │   │
│  │              │  │ [Bio excerpt]                  │   │
│  │              │  │ 🌐 vi en  |  💰 từ 500k        │   │
│  └──────────────┘  └────────────────────────────────┘   │
└──────────────────────────────────────────────────────────┘
```

### Tab 1: Thông tin cơ bản
```
Avatar Upload:
  - Dropzone.js circle preview (128px)
  - "Nhấp để tải ảnh hoặc kéo thả vào đây"
  - Requirements: JPG/PNG/WebP, max 5MB, tỷ lệ 1:1 khuyến nghị

Ảnh bìa (Cover Photo):
  - Upload banner 16:9 (hiện ở đầu profile page)
  
Thông tin:
  FullName*:    [__________________________]
  PhoneNumber:  [__________________________] (E.164, VD: +84901234567)
  CityArea*:    [Dropdown: Hội An / Đà Nẵng / Hà Nội / TP.HCM / ...]

Bio*:
  [Textarea, max 1000 ký tự, còn lại: 876 ký tự]
  Placeholder: "Kể về bản thân bạn — bạn là ai, điều gì làm bạn trở thành người hướng dẫn đặc biệt,
               những kỷ niệm đáng nhớ bạn đã tạo ra cho khách..."
  
[Lưu thông tin cơ bản]
```

### Tab 2: Chuyên môn & Ngôn ngữ
```
Ngôn ngữ sử dụng:
  [✓] Tiếng Việt    [✓] Tiếng Anh    [ ] Tiếng Hàn
  [ ] Tiếng Nhật    [ ] Tiếng Pháp   [+ Thêm ngôn ngữ khác]

Chuyên môn (Specialties):
  [✓] Ẩm thực đường phố   [✓] Văn hóa - Lịch sử   [ ] Nhiếp ảnh
  [ ] Mạo hiểm             [ ] Nightlife             [ ] Thiên nhiên
  [ ] Craft & Local Art    [+ Thêm chuyên môn]

Giá cơ bản mỗi giờ:
  [150,000]  VND / giờ
  ⚠ Giá theo gói được thiết lập trong "My Tours"
  
[Lưu chuyên môn]
```

### Tab 3: Hidden Gems Portfolio
```
┌─────────────────────────────────────────────────────┐
│  HIDDEN GEMS PORTFOLIO  [3 / 20 ảnh]                │
│  "Ảnh địa điểm độc đáo do bạn tự chụp — đây là     │
│   điểm tạo sự khác biệt của bạn với guide khác"     │
├─────────────────────────────────────────────────────┤
│  [Dropzone lớn, dạng grid 4 cột]                    │
│  ┌────┐ ┌────┐ ┌────┐ ┌────┐                        │
│  │ 🖼  │ │ 🖼 │ │ + Thêm ảnh │                       │
│  │[×] │ │[×] │ │ Kéo thả   │                        │
│  └────┘ └────┘ └─────────-─┘                        │
│                                                     │
│  Mỗi ảnh: hover → [✎ Thêm caption] [🗑 Xóa]        │
│  Drag-to-reorder (sortable.js)                      │
│  Formats: JPG, PNG, WebP | Max 5MB/ảnh | Min 3 ảnh  │
└─────────────────────────────────────────────────────┘
```

### Tab 4: Tài khoản & Bảo mật
```
Email:           guide@example.com  [Đã xác minh ✓]
Đổi mật khẩu:   [Mật khẩu hiện tại] [Mật khẩu mới] [Xác nhận]

Tài khoản ngân hàng (cho nhận thanh toán):
  Ngân hàng:    [Dropdown: VCB / TCB / MBBank / Vietinbank...]
  Số tài khoản: [__________________________]
  Tên chủ TK:   [__________________________]
  [Lưu tài khoản ngân hàng]

Xóa tài khoản:  [Yêu cầu xóa tài khoản] (text-red-600, cần confirm modal)
```

### States
- **Chưa verified**: Banner sticky "Hồ sơ của bạn đang chờ Admin xem xét. Thời gian duyệt: 24–48 giờ."
- **Live preview**: Debounce 500ms — mỗi lần edit field → preview card bên phải tự cập nhật (Alpine reactive)
- **Unsaved changes**: Nút "Lưu" highlight + browser beforeunload warning

---

## 📄 TRANG 8 — EARNINGS REPORT (`/guide/earnings`)

### Mục đích
Financial dashboard — Guide theo dõi toàn bộ dòng tiền: đã nhận, đang chờ, lịch sử giao dịch, và biểu đồ xu hướng.

### Layout
```
┌──────────────────────────────────────────────────────────┐
│  THU NHẬP            [Tuần này ▾]  [Tháng này]  [Năm này]│
├────────────┬─────────────┬──────────────┬─────────────────┤
│ Đã nhận    │ Đang tạm giữ│ Tour         │ Đánh giá        │
│ 8,500,000đ │ 1,700,000đ  │ hoàn thành:8 │ trung bình: 4.9  │
│ (completed)│ (confirmed) │              │                 │
├────────────┴─────────────┴──────────────┴─────────────────┤
│  BIỂU ĐỒ THU NHẬP 12 THÁNG (Chart.js Bar Chart)          │
│                                                           │
│  1.5M ┤                              ████                │
│  1.0M ┤              ████      ████  ████ ████            │
│  0.5M ┤  ████  ████  ████  ██  ████  ████ ████ ████       │
│       └──T7────T8────T9────T10──T11──T12──T1───T2──...    │
│                                                           │
├───────────────────────────────────────────────────────────┤
│  LỊCH SỬ GIAO DỊCH                [Xuất CSV] [Lọc ▾]    │
│  ─────────────────────────────────────────────────────── │
│  Ngày      Khách           Gói tour         Thu về        │
│  12/06/26  Bảo Châu        Bình minh...     850,000đ  ✓  │
│  10/06/26  Hùng Anh        Phố cổ đêm       680,000đ  ✓  │
│  08/06/26  Linh Nguyễn     ...              510,000đ  ⏳  │
│  ─────────────────────────────────────────────────────── │
│  [← 1 2 3 →]                                             │
└───────────────────────────────────────────────────────────┘
```

### Stats Cards Detail
| Card | Formula | Note |
|---|---|---|
| Đã nhận | Sum(GuideEarnings) WHERE Status=2 & EscrowReleased=true | Hiển thị theo period filter |
| Đang tạm giữ | Sum(GuideEarnings) WHERE Status=1 & EscrowReleased=false | Tooltip "Sẽ được giải ngân sau khi tour hoàn thành" |
| Tours hoàn thành | Count(Status=2) | Theo period filter |
| Đánh giá TB | AverageRating | Không filter theo period |

### Chart.js Configuration
```javascript
const ctx = document.getElementById('earningsChart');
new Chart(ctx, {
  type: 'bar',
  data: {
    labels: monthLabels,  // ['T7/25', 'T8/25', ...]
    datasets: [{
      label: 'Thu nhập (VND)',
      data: earningsData,
      backgroundColor: 'rgba(37, 99, 235, 0.8)',  // blue-600
      borderRadius: 6,
      hoverBackgroundColor: 'rgba(37, 99, 235, 1)',
    }]
  },
  options: {
    responsive: true,
    plugins: {
      tooltip: {
        callbacks: {
          label: (ctx) => `${formatVND(ctx.parsed.y)}`
        }
      }
    },
    scales: {
      y: { ticks: { callback: (v) => formatVND(v, true) } }
    }
  }
});
```

### Transaction Table
- Columns: Ngày | Tên khách | Gói tour | Tổng tour | Phí nền tảng | Thu về | Trạng thái
- Row click → expand: xem Booking ID, Payment Reference, ngày giải ngân
- **Xuất CSV**: `GET /api/guide/earnings/export?from=&to=` trả về CSV download
- **Pending rows** (Status=1): text-amber-600 + icon ⏳ + tooltip "Chờ tour hoàn thành"

---

## 📄 TRANG 9 — SUPPORT (`/guide/support`)

### Mục đích
Trung tâm trợ giúp — Guide tìm câu trả lời nhanh qua FAQ, hoặc tạo ticket khi gặp vấn đề booking, thanh toán, tài khoản.

### Layout
```
┌──────────────────────────────────────────────────────────┐
│  TRỢ GIÚP & HỖ TRỢ                                       │
│                                                           │
│  [🔍 Tìm kiếm câu hỏi thường gặp...]                     │
├─────────────────────────────────────┬────────────────────┤
│  CÂU HỎI THƯỜNG GẶP (FAQ)           │  LIÊN HỆ TRỰC TIẾP │
│                                     │                    │
│  NHÓM: Booking & Lịch               │ 📧 Email           │
│  [▼] Tôi có thể hủy booking không?  │  support@tripmate  │
│  [▼] Khách không đến, tôi phải làm? │                    │
│  [▼] Bao lâu tôi nhận được tiền?    │ 💬 Live Chat       │
│                                     │  [Bắt đầu chat]    │
│  NHÓM: Thanh toán                   │  Thứ 2–6, 8h–18h   │
│  [▼] Phí nền tảng tính như thế nào? │                    │
│  [▼] Cách nhận tiền về ngân hàng?   │ 📋 Tạo Ticket      │
│                                     │  [→ Tạo ticket mới]│
│  NHÓM: Hồ sơ & Xác minh            │                    │
│  [▼] Tại sao hồ sơ chưa được duyệt?│ ⏱ Thời gian phản   │
│  [▼] Cách thêm ảnh Hidden Gems?     │  hồi: < 4 giờ      │
│                                     │                    │
└─────────────────────────────────────┴────────────────────┘
│  TICKET CỦA TÔI                          [Tạo ticket mới]│
├──────────────────────────────────────────────────────────┤
│ #TK-001  Khách hủy sát giờ    Đang xử lý 🟡  12/06/2026 │
│ #TK-002  Lỗi không nhận được  Đã giải quyết ✓ 05/06/2026│
└──────────────────────────────────────────────────────────┘
```

### Create Ticket Form (Modal)
```
Loại vấn đề*:  [Dropdown: Booking tranh chấp / Thanh toán / Tài khoản / Kỹ thuật / Khác]
Booking liên quan (nếu có): [Search booking by ID]
Tiêu đề*:     [_____________________________________________]
Mô tả chi tiết*: [Textarea, min 50 ký tự]
Upload bằng chứng: [Dropzone, max 3 files, ảnh/PDF]

[Hủy]  [Gửi yêu cầu →]
```

### FAQ Accordion (Alpine.js)
```html
<div x-data="{ open: null }">
  <button @click="open === 1 ? open = null : open = 1"
          class="w-full flex justify-between items-center py-4 text-left">
    <span class="font-medium text-gray-900">Tôi có thể hủy booking không?</span>
    <i class="bi" :class="open === 1 ? 'bi-chevron-up' : 'bi-chevron-down'"></i>
  </button>
  <div x-show="open === 1" x-collapse>
    <p class="pb-4 text-gray-600 text-sm">Bạn có thể hủy booking ở trạng thái Chờ duyệt...</p>
  </div>
</div>
```

---

## 🔔 PHẦN 5 — REAL-TIME BOOKING REQUEST MODAL (GLOBAL)

Modal này xuất hiện **trên mọi trang** khi có booking mới — không cần Guide đang ở trang Bookings.

```html
<!-- Được inject vào _Layout.cshtml -->
<div class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center"
     x-data="bookingRequestModal()"
     x-show="open"
     x-transition>
  <div class="bg-white rounded-2xl shadow-2xl w-full max-w-md mx-4 overflow-hidden">
    
    <!-- Header với countdown -->
    <div class="bg-gradient-to-r from-blue-600 to-blue-700 px-6 py-4 text-white">
      <div class="flex items-center justify-between">
        <h3 class="font-bold text-lg flex items-center gap-2">
          <i class="bi bi-bell-fill animate-bounce"></i>
          Yêu cầu đặt tour mới!
        </h3>
        <!-- Countdown 24h timer -->
        <span class="bg-amber-400 text-amber-900 text-sm font-bold
                     px-3 py-1 rounded-full" x-text="formatTime(secondsLeft)"></span>
      </div>
    </div>
    
    <!-- Booking Summary -->
    <div class="p-6 space-y-4">
      <div class="flex items-center gap-3">
        <img :src="booking.travelerAvatar" class="w-12 h-12 rounded-full object-cover">
        <div>
          <p class="font-semibold text-gray-900" x-text="booking.travelerName"></p>
          <p class="text-sm text-gray-500">⭐ <span x-text="booking.travelerRating"></span></p>
        </div>
      </div>
      
      <div class="bg-gray-50 rounded-xl p-4 space-y-2 text-sm">
        <div class="flex justify-between">
          <span class="text-gray-500">Gói tour</span>
          <span class="font-medium" x-text="booking.packageName"></span>
        </div>
        <div class="flex justify-between">
          <span class="text-gray-500">Ngày đi</span>
          <span class="font-medium" x-text="booking.bookingDate"></span>
        </div>
        <div class="flex justify-between">
          <span class="text-gray-500">Số khách</span>
          <span class="font-medium" x-text="booking.guestCount + ' người'"></span>
        </div>
        <div class="flex justify-between border-t pt-2">
          <span class="text-gray-500">Bạn nhận được</span>
          <span class="font-bold text-green-600 text-base" x-text="booking.guideEarnings"></span>
        </div>
      </div>
      
      <!-- Traveler note if exists -->
      <div class="text-sm text-gray-600 italic bg-blue-50 rounded-lg p-3"
           x-show="booking.travelerNotes">
        💬 "<span x-text="booking.travelerNotes"></span>"
      </div>
    </div>
    
    <!-- Action buttons -->
    <div class="px-6 pb-6 flex gap-3">
      <button @click="reject()"
              class="flex-1 py-3 border-2 border-red-200 text-red-600
                     rounded-xl font-semibold hover:bg-red-50 transition">
        <i class="bi bi-x-lg me-1"></i> Từ chối
      </button>
      <button @click="accept()"
              class="flex-2 py-3 bg-green-600 text-white rounded-xl
                     font-semibold hover:bg-green-700 transition flex-grow">
        <i class="bi bi-check-lg me-1"></i> Chấp nhận ngay
      </button>
    </div>
  </div>
</div>

<script>
function bookingRequestModal() {
  return {
    open: false,
    booking: {},
    secondsLeft: 86400, // 24h
    timer: null,
    
    init() {
      window.notificationConn.on("NewBookingRequest", (b) => {
        this.booking = b;
        this.secondsLeft = b.secondsUntilExpiry;
        this.open = true;
        this.startCountdown();
        // Play notification sound
        new Audio('/sounds/booking-request.mp3').play().catch(() => {});
      });
    },
    
    startCountdown() {
      this.timer = setInterval(() => {
        this.secondsLeft--;
        if (this.secondsLeft <= 0) { this.close(); }
      }, 1000);
    },
    
    formatTime(s) {
      const h = Math.floor(s/3600), m = Math.floor((s%3600)/60), sec = s%60;
      return `${String(h).padStart(2,'0')}:${String(m).padStart(2,'0')}:${String(sec).padStart(2,'0')}`;
    },
    
    async accept() {
      await fetch(`/api/booking/${this.booking.id}/confirm`, { method: 'PATCH',
        headers: { 'Authorization': `Bearer ${window.__authToken}` }
      });
      this.close();
      showToast('success', 'Đã xác nhận booking! Kênh chat đã được mở.');
    },
    
    async reject() {
      if (!confirm('Bạn chắc chắn muốn từ chối booking này?')) return;
      await fetch(`/api/booking/${this.booking.id}/cancel`, { method: 'PATCH',
        headers: { 'Authorization': `Bearer ${window.__authToken}` }
      });
      this.close();
      showToast('info', 'Đã từ chối. Khách sẽ được hoàn tiền tự động.');
    },
    
    close() {
      clearInterval(this.timer);
      this.open = false;
    }
  };
}
</script>
```

---

## ♿ PHẦN 6 — ACCESSIBILITY & RESPONSIVE

### Mobile (< 768px)
- Sidebar chuyển thành bottom nav bar (5 icon chính: Dashboard, Bookings, Calendar, Messages, Earnings)
- Booking detail: full-screen sheet thay vì slide-over
- Chat: ẩn conversation list, chỉ show khi tap "← Danh sách"
- Stats cards: scroll ngang (overflow-x-scroll snap-x)

### Accessibility
- Tất cả interactive elements có `focus:ring-2 focus:ring-blue-500`
- Modal: `role="dialog" aria-modal="true" aria-labelledby`
- Form labels: luôn có `<label for>` tương ứng, không dùng placeholder thay label
- Color không là thông tin duy nhất: luôn kết hợp icon + text cho status badges
- Loading states: `aria-busy="true"` + skeleton components (không dùng spinner đơn lẻ)

---

## 🚀 PHẦN 7 — TOAST NOTIFICATION SYSTEM (GLOBAL)

```javascript
// Shared across all pages
function showToast(type, message, duration = 4000) {
  const colors = {
    success: 'bg-green-600',
    error:   'bg-red-600',
    warning: 'bg-amber-500',
    info:    'bg-blue-600',
  };
  const icons = {
    success: 'bi-check-circle-fill',
    error:   'bi-x-circle-fill',
    warning: 'bi-exclamation-triangle-fill',
    info:    'bi-info-circle-fill',
  };
  
  const toast = document.createElement('div');
  toast.className = `fixed bottom-6 right-6 z-[100] flex items-center gap-3
    text-white px-5 py-3 rounded-xl shadow-lg ${colors[type]}
    animate-slide-in-from-bottom`;
  toast.innerHTML = `<i class="bi ${icons[type]}"></i><span>${message}</span>`;
  document.body.appendChild(toast);
  setTimeout(() => { toast.classList.add('animate-fade-out'); 
    setTimeout(() => toast.remove(), 300); 
  }, duration);
}
```
