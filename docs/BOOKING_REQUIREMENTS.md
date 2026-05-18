# 📋 Booking Flow - Requirements Specification

> Tài liệu yêu cầu chi tiết cho luồng đặt tour trong TripMate

## 🎯 Overview

### Mục đích
Hệ thống booking cho phép Traveler đặt tour, Guide quản lý booking và Admin giám sát hoạt động.

### User Roles
| Role | Permissions |
|------|-------------|
| **Traveler** | Xem tour, đặt tour, hủy booking, chat với guide |
| **Guide** | Xem booking của tour mình, xác nhận/từ chối booking, chat với traveler |
| **Admin** | Xem tất cả booking, xử lý dispute |

---

## 📊 Booking States

### State Machine
```
pending ──(guide accepts)──> confirmed ──(24h before)──> preparing
   │                             │
   │                             └──(tour day)──> inProgress ──> completed
   │
   └──(cancel)──> cancelled
```

### State Definitions

| State | Description | Available Actions |
|-------|-------------|-------------------|
| `pending` | Chờ guide xác nhận | cancel (by traveler), accept (by guide), reject (by guide) |
| `confirmed` | Guide đã xác nhận | cancel (nếu >24h), reschedule (nếu >48h) |
| `preparing` | Còn <24h đến tour | contact support |
| `inProgress` | Tour đang diễn ra | - |
| `completed` | Tour hoàn thành | review, rate |
| `cancelled` | Đã hủy | - |

---

## 🔐 Business Rules

### BR-001: Booking Creation
**Khi traveler đặt tour:**
- Ngày tour phải sau ngày hiện tại ít nhất 1 ngày
- Số khách: 1 ≤ guests ≤ maxParticipants
- Tour phải ở trạng thái `active`
- Traveler phải đăng nhập

### BR-002: Cancellation Policy
**Hủy booking:**
- Free cancel: Hủy trước 48h → Hoàn 100%
- Partial refund: Hủy 24h-48h → Hoàn 50%
- No refund: Hủy <24h → Không hoàn tiền

### BR-003: Guide Response Time
**Guide phải phản hồi:**
- Thời gian phản hồi tối đa: 24 giờ
- Auto-accept: Nếu guide không phản hồi sau 24h → Tự động confirm (configurable)

### BR-004: Booking Availability
**Kiểm tra còn chỗ:**
- Hiện tại: Chưa implement real availability check
- Tương lai: Check số booking confirmed so với maxParticipants per slot

---

## 📱 User Stories

### US-001: Browse & Select Tour
**As a** Traveler  
**I want to** browse available tours  
**So that** I can find tours that interest me

**Acceptance Criteria:**
- [ ] Xem danh sách tour với pagination (20 items/page)
- [ ] Filter theo location, price range, duration, rating
- [ ] Search theo tour name, description
- [ ] Xem tour details: images, description, itinerary, price, reviews

---

### US-002: Create Booking
**As a** Traveler  
**I want to** book a tour  
**So that** I can reserve my spot

**Acceptance Criteria:**
- [ ] Chọn ngày tour (từ mai đến 365 ngày sau)
- [ ] Chọn số khách (1 đến maxParticipants)
- [ ] Thêm note (optional)
- [ ] Xem price summary trước khi submit
- [ ] Validation trước khi submit:
  - Ngày tour: required
  - Số khách: required, 1 ≤ guests ≤ maxParticipants
- [ ] Hiển thị loading state khi đang submit
- [ ] Success → Navigate đến payment screen
- [ ] Failure → Hiển thị error message với retry option

---

### US-003: Payment
**As a** Traveler  
**I want to** pay for my booking  
**So that** my reservation is confirmed

**Acceptance Criteria:**
- [ ] Xem booking summary: tour, date, guests, total price
- [ ] Chọn payment method (MoMo, VNPay, ZaloPay, Card)
- [ ] Mock payment flow (2s delay)
- [ ] Success → Booking confirmation screen
- [ ] Failure → Error message với retry option

---

### US-004: Booking Confirmation
**As a** Traveler  
**I want to** receive booking confirmation  
**So that** I have proof of my reservation

**Acceptance Criteria:**
- [ ] Hiển thị booking reference number (UUID first 8 chars)
- [ ] Hiển thị booking details: tour, location, date, guests, total
- [ ] Hiển thị status: "Chờ xác nhận"
- [ ] Hiển thị info: "Guide sẽ xác nhận trong 24h"
- [ ] Action buttons:
  - "Về trang chủ" (primary)
  - "Xem chuyến đi của tôi" (secondary)
  - "Nhắn tin với guide" (tertiary)

---

### US-005: View My Bookings
**As a** Traveler  
**I want to** view my booking history  
**So that** I can track my reservations

**Acceptance Criteria:**
- [ ] Xem danh sách booking theo status filter:
  - All
  - Upcoming (pending, confirmed)
  - Completed
  - Cancelled
- [ ] Hiển thị booking info: tour title, date, status, price
- [ ] Pull-to-refresh để reload data
- [ ] Loading state khi đang fetch data
- [ ] Empty state khi không có booking
- [ ] Error state với retry option

---

### US-006: Cancel Booking
**As a** Traveler  
**I want to** cancel a booking  
**So that** I can free up my reservation if plans change

**Acceptance Criteria:**
- [ ] Chỉ cancel được booking status: pending, confirmed
- [ ] Không cancel được nếu <24h trước tour
- [ ] Hiển thị confirmation dialog trước khi cancel
- [ ] Hiển thị cancellation policy
- [ ] Success → Update booking list, hiển thị success message
- [ ] Failure → Error message

---

### US-007: Chat with Guide
**As a** Traveler  
**I want to** chat with my guide  
**So that** I can ask questions about the tour

**Acceptance Criteria:**
- [ ] Tạo/get conversation từ booking confirmation screen
- [ ] Tạo/get conversation từ booking detail screen
- [ ] Real-time messaging qua Supabase Realtime
- [ ] Xem conversation history
- [ ] Gửi tin nhắn văn bản
- [ ] Hiển thị online/offline status

---

## 🏗️ Technical Requirements

### TR-001: Clean Architecture
**Code organization:**
```
booking/
├── domain/
│   ├── entities/booking_entity.dart
│   ├── repositories/booking_repository.dart
│   └── usecases/
│       ├── create_booking_usecase.dart
│       ├── get_my_bookings_usecase.dart
│       └── cancel_booking_usecase.dart
├── data/
│   ├── models/booking_model.dart
│   ├── datasources/booking_datasource.dart
│   └── repositories/booking_repository_impl.dart
└── presentation/
    ├── providers/booking_provider.dart
    └── screens/
        ├── booking_form_screen.dart
        ├── mock_payment_screen.dart
        └── booking_confirmation_screen.dart
```

### TR-002: State Management (Riverpod)
**Providers:**
- `bookingFormProvider` - Form state với validation
- `myBookingsProvider` - My bookings list với filter
- `createBookingProvider` - Create booking result
- `bookingRepositoryProvider` - Repository dependency

### TR-003: Error Handling
**Error types:**
- `ServerException` - API errors
- `ValidationException` - Form validation errors
- `NetworkException` - Connection errors

**Error display:**
- User-friendly messages (Vietnamese)
- No technical stack traces
- Retry option for network errors

### TR-004: API Contract
**Endpoints:**
```
POST   /bookings          - Create booking
GET    /bookings/my       - Get my bookings
GET    /bookings/:id      - Get booking by ID
DELETE /bookings/:id      - Cancel booking
```

**Request/Response format:**
```json
// POST /bookings - Request
{
  "tourId": "uuid",
  "tourDate": "2026-05-15",
  "guests": 2,
  "note": "Optional note"
}

// Response
{
  "id": "uuid",
  "tourId": "uuid",
  "tourTitle": "Tour name",
  "tourLocation": "Location",
  "travelerId": "uuid",
  "guideId": "uuid",
  "tourDate": "2026-05-15",
  "guests": 2,
  "unitPrice": 1000000,
  "totalPrice": 2000000,
  "note": "Optional note",
  "status": "pending",
  "createdAt": "2026-05-01T10:00:00Z"
}
```

---

## 🎨 UI/UX Requirements

### UR-001: Design System
- **Primary Color**: `#E91E8C` (Pink)
- **Success**: `#4CAF50` (Green)
- **Error**: `#F44336` (Red)
- **Warning**: `#FF9800` (Orange)

### UR-002: Loading States
- LoadingIndicator cho async operations
- Disable buttons khi đang loading
- Skeleton loaders cho content

### UR-003: Error States
- Error icon + message
- Retry button
- User-friendly Vietnamese messages

### UR-004: Empty States
- Illustration/icon
- Descriptive text
- Call-to-action button

---

## 📊 Metrics & Analytics

### Key Metrics
| Metric | Target |
|--------|--------|
| Booking conversion rate | >5% |
| Payment success rate | >90% |
| Guide response time | <12h average |
| Cancellation rate | <10% |

### Events to Track
- `booking_started` - User enters booking form
- `booking_submitted` - User submits booking form
- `payment_completed` - Payment successful
- `booking_cancelled` - User cancels booking
- `chat_initiated` - User starts chat with guide

---

## 🧪 Testing Requirements

### Unit Tests
- [ ] BookingEntity creation
- [ ] BookingModel.fromJson parsing
- [ ] UseCase business logic
- [ ] Provider state transitions

### Integration Tests
- [ ] API endpoint calls
- [ ] Repository methods
- [ ] DataSource error handling

### Widget Tests
- [ ] BookingFormScreen validation
- [ ] BookingConfirmationScreen display
- [ ] MockPaymentScreen interactions

### E2E Tests
- [ ] Complete booking flow
- [ ] Cancellation flow
- [ ] Chat from booking flow

---

## 📈 Implementation Status

| Feature | Status | Notes |
|---------|--------|-------|
| Create Booking | ✅ Complete | Form validation implemented |
| Payment (Mock) | ✅ Complete | Ready for real payment integration |
| My Bookings List | ✅ Complete | Filter by status pending |
| Cancel Booking | ⚠️ Partial | UI ready, backend needed |
| Real-time Updates | ❌ Todo | Supabase Realtime pending |
| Booking Timeline | ❌ Todo | UI component pending |
| Review System | ❌ Todo | Post-tour review pending |

---

## 🔒 Security Considerations

### SC-001: Authentication
- JWT token required for all booking endpoints
- Token stored in secure storage
- Auto-refresh before expiration

### SC-002: Authorization
- Traveler chỉ xem/edit booking của mình
- Guide chỉ xem booking của tour mình
- Admin xem tất cả booking

### SC-003: Data Validation
- Input sanitization
- SQL injection prevention
- XSS prevention

---

## 📝 Revision History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0 | 2026-05-01 | Initial clean requirements | TripMate Team |

---

**Status**: ✅ Approved  
**Next Review**: 2026-06-01
