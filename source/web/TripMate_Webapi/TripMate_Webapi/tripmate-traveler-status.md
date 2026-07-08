# TripMate Traveler Workflow Status

## 1. Các file/component đã hoàn thiện và chức năng:
*   **Survey/Quiz Match (`Matches.cshtml`, `SurveyController.cs`)**: Hệ thống lấy dữ liệu Survey của người dùng (từ DB nếu đã làm, hoặc lưu Session nếu chưa đăng nhập), chạy thuật toán AI-Match và trả về danh sách các Guide phù hợp nhất theo phần trăm. Chức năng lưu kết quả vào DB cũng đã hoạt động.
*   **Authentication Flow (`TravelerController.cs`, `AuthController.cs`)**: Cấu trúc bảo mật với JWT Token. Middleware Auth kết hợp với Token lưu trữ phía Client (`localStorage`) cho phép hiển thị các component Alpine.js (Dashboard, Trips) qua các lời gọi API Ajax. Hỗ trợ cơ chế "Ghost Booking" nếu chưa đăng nhập.
*   **Homepage, Explore & Tours (`Index.cshtml`, `Explore.cshtml`, `Tours.cshtml`)**: Giao diện duyệt danh sách Tour/Guide kết hợp bộ lọc (Destination, Search). Tích hợp Tour Modal thông tin (thời gian, lịch trình chi tiết) bằng Alpine.js thay vì chuyển trang. Custom Itinerary đã được ẩn khỏi View chung.
*   **Guide Profile (`GuideProfile.cshtml`)**: Hiển thị hồ sơ chi tiết của Guide, chứa danh sách các Package. Cho phép Book trực tiếp Package hoặc nếu không có, sẽ cung cấp tuỳ chọn Book "Custom Itinerary".
*   **Dashboard & Trips (`Dashboard.cshtml`, `Trips.cshtml`)**: Quản lý lịch trình, phân loại Upcoming Trips và Past Trips. Hỗ trợ huỷ (Delete/Cancel) các booking đang Pending và cập nhật giao diện thời gian thực (Ajax + Alpine.js).
*   **Booking APIs (`TravelerController.cs`, `BookingRepository.cs`)**: Các API xử lý logic tạo Booking, chặn đặt trùng lịch, tính toán Platform Fee (15%) và doanh thu cho Guide (85%).

## 2. Luồng dữ liệu hiện tại (Traveler Flow):
1. **Khám phá**: Traveler truy cập Homepage, thực hiện bài Quiz để tìm AI-Match Guide, hoặc tự do tìm kiếm Tour / Destinations.
2. **Xem chi tiết & Đặt chỗ**: Bấm vào Tour để xem Modal hoặc vào Guide Profile. Từ đây, người dùng chọn "Book This Tour" / "Book Custom".
3. **Kiểm tra đăng nhập**:
    *   Nếu chưa đăng nhập: Yêu cầu đăng nhập/đăng ký. Giữ lại lựa chọn Booking qua session "Ghost Booking".
    *   Nếu đã đăng nhập: Khởi tạo dữ liệu Booking với trạng thái `Status = 0` (Pending) -> Tránh duplicate -> Trả về `BookingId` thành công.
4. **Quản lý (Dashboard)**: Dữ liệu Booking hiển thị ngay tại trang Dashboard/Trips thông qua API `/GetMyBookings`. Cho phép huỷ bỏ (Cancel) nếu vẫn còn Pending.

## 3. Những phần đang làm dở & Tính năng tiếp theo (Next Steps):
*   **Trang thanh toán Checkout (`Checkout.cshtml`)**: Bước cuối cùng sau khi tạo BookingId thành công (hiện mã code đang route tới `/Traveler/Checkout/{bookingId}` nhưng giao diện và logic tích hợp cổng thanh toán / Escrow vẫn chưa hoàn thành).
*   **Review/Rating (`Review.cshtml`)**: Luồng cho phép Traveler đánh giá Tour/Guide sau khi trạng thái Booking chuyển sang Completed (2).
*   **Luồng Phê duyệt từ phía Guide**: Chờ các thành viên team Guide xử lý giao diện Accept/Decline Booking để trạng thái từ Pending(0) -> Confirmed(1), kích hoạt tính năng **Messages**.
*   **Chức năng Messages**: Trang chat giữa Traveler và Guide sau khi Booking được Confirm (giao cho team Guide hoàn thiện).
