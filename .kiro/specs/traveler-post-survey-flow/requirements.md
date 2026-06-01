# Requirements Document

## Introduction

Tài liệu này mô tả các yêu cầu cho tính năng "Traveler Post-Survey Flow" trong hệ thống TripMate. Tính năng này xác định quy trình xử lý và các hành động tiếp theo sau khi traveler hoàn thành và gửi survey đánh giá tour. Mục tiêu là cung cấp trải nghiệm người dùng mượt mà, thu thập phản hồi chất lượng, và khuyến khích traveler tương tác tiếp với nền tảng.

## Glossary

- **Traveler**: Người dùng có vai trò khách du lịch, đã đặt và tham gia tour
- **Survey**: Bảng khảo sát đánh giá tour bao gồm rating (điểm số) và comment (nhận xét văn bản)
- **Tour**: Chuyến du lịch được tổ chức bởi guide với thông tin cụ thể về địa điểm, thời gian, giá cả
- **Guide**: Hướng dẫn viên tổ chức và dẫn dắt tour
- **Booking**: Đơn đặt tour của traveler với trạng thái cụ thể (pending, confirmed, completed, cancelled)
- **Survey_Submission**: Hành động gửi survey đánh giá tour từ traveler
- **Backend_API**: Hệ thống API backend ASP.NET Core xử lý logic nghiệp vụ
- **Mobile_App**: Ứng dụng Flutter cho traveler
- **Notification**: Thông báo được gửi đến người dùng qua hệ thống
- **Tour_Rating**: Điểm đánh giá trung bình của tour được tính từ tất cả survey
- **Survey_Response**: Phản hồi từ Backend_API sau khi xử lý Survey_Submission

## Requirements

### Requirement 1: Survey Submission Processing

**User Story:** Là một traveler, tôi muốn gửi survey đánh giá tour, để hệ thống ghi nhận phản hồi của tôi và cập nhật thông tin tour.

#### Acceptance Criteria

1. WHEN a traveler submits a survey, THE Mobile_App SHALL validate that rating is between 1 and 5 stars
2. WHEN a traveler submits a survey, THE Mobile_App SHALL validate that comment length is between 10 and 500 characters
3. WHEN survey validation passes, THE Mobile_App SHALL send Survey_Submission to Backend_API with tour ID, booking ID, rating, and comment
4. WHEN Backend_API receives Survey_Submission, THE Backend_API SHALL verify that the booking status is "completed"
5. WHEN Backend_API receives Survey_Submission, THE Backend_API SHALL verify that the traveler has not submitted a survey for this booking previously
6. WHEN survey verification passes, THE Backend_API SHALL store the survey data in the database
7. WHEN survey is stored successfully, THE Backend_API SHALL recalculate Tour_Rating for the tour
8. WHEN survey is stored successfully, THE Backend_API SHALL return Survey_Response with success status to Mobile_App

### Requirement 2: Survey Submission Feedback

**User Story:** Là một traveler, tôi muốn nhận phản hồi ngay lập tức sau khi gửi survey, để biết rằng đánh giá của tôi đã được ghi nhận thành công.

#### Acceptance Criteria

1. WHEN Mobile_App receives successful Survey_Response, THE Mobile_App SHALL display a success message "Cảm ơn bạn đã đánh giá tour!"
2. WHEN Mobile_App receives successful Survey_Response, THE Mobile_App SHALL display a confirmation animation for 2 seconds
3. WHEN Mobile_App displays success message, THE Mobile_App SHALL show the submitted rating and comment to the traveler
4. IF Backend_API returns an error response, THEN THE Mobile_App SHALL display an error message describing the issue
5. IF network connection fails during submission, THEN THE Mobile_App SHALL display "Không thể kết nối. Vui lòng thử lại sau."

### Requirement 3: Post-Survey Navigation Options

**User Story:** Là một traveler, tôi muốn có các lựa chọn hành động tiếp theo sau khi gửi survey, để tôi có thể tiếp tục khám phá nền tảng hoặc quay lại trang chính.

#### Acceptance Criteria

1. WHEN success message is displayed, THE Mobile_App SHALL present a "Khám phá thêm tour" button
2. WHEN success message is displayed, THE Mobile_App SHALL present a "Về trang chủ" button
3. WHEN traveler taps "Khám phá thêm tour" button, THE Mobile_App SHALL navigate to the tour list screen
4. WHEN traveler taps "Về trang chủ" button, THE Mobile_App SHALL navigate to the traveler dashboard home tab
5. WHEN 10 seconds elapse after displaying success message, THE Mobile_App SHALL automatically navigate to traveler dashboard home tab

### Requirement 4: Survey Visibility Update

**User Story:** Là một traveler, tôi muốn thấy đánh giá của mình xuất hiện trong danh sách reviews của tour, để xác nhận rằng phản hồi của tôi đã được công khai.

#### Acceptance Criteria

1. WHEN Backend_API stores a survey successfully, THE Backend_API SHALL mark the survey as "published"
2. WHEN a survey is marked as "published", THE Backend_API SHALL include it in the tour reviews list
3. WHEN Mobile_App loads tour detail screen, THE Mobile_App SHALL fetch and display all published surveys for that tour
4. WHEN displaying reviews, THE Mobile_App SHALL show traveler name, rating, comment, and submission date
5. WHEN displaying reviews, THE Mobile_App SHALL sort reviews by submission date in descending order

### Requirement 5: Guide Notification

**User Story:** Là một guide, tôi muốn nhận thông báo khi có traveler đánh giá tour của tôi, để tôi có thể theo dõi phản hồi và cải thiện dịch vụ.

#### Acceptance Criteria

1. WHEN Backend_API stores a survey successfully, THE Backend_API SHALL create a notification for the guide
2. WHEN notification is created, THE Backend_API SHALL include tour title, traveler name, and rating in the notification
3. WHEN guide opens the Mobile_App, THE Mobile_App SHALL display unread notification count
4. WHEN guide taps on notification, THE Mobile_App SHALL navigate to the tour detail screen showing the new review
5. WHEN guide views the notification, THE Backend_API SHALL mark the notification as read

### Requirement 6: Tour Rating Recalculation

**User Story:** Là một traveler, tôi muốn thấy rating trung bình của tour được cập nhật sau khi tôi gửi đánh giá, để phản ánh chính xác chất lượng tour.

#### Acceptance Criteria

1. WHEN Backend_API stores a survey successfully, THE Backend_API SHALL calculate the new average rating from all published surveys
2. WHEN calculating average rating, THE Backend_API SHALL round the result to one decimal place
3. WHEN new average rating is calculated, THE Backend_API SHALL update the Tour_Rating field in the tour record
4. WHEN new average rating is calculated, THE Backend_API SHALL update the total_reviews count for the tour
5. WHEN Mobile_App displays tour information, THE Mobile_App SHALL show the updated Tour_Rating and total_reviews count

### Requirement 7: Survey Edit Restriction

**User Story:** Là một traveler, tôi muốn biết rằng sau khi gửi survey, tôi không thể chỉnh sửa nó, để đảm bảo tính toàn vẹn của đánh giá.

#### Acceptance Criteria

1. WHEN a traveler has submitted a survey for a booking, THE Mobile_App SHALL hide the survey submission form for that booking
2. WHEN a traveler views a completed booking with submitted survey, THE Mobile_App SHALL display "Bạn đã đánh giá tour này" message
3. WHEN a traveler views their submitted survey, THE Mobile_App SHALL display the rating and comment in read-only mode
4. IF a traveler attempts to submit a second survey for the same booking, THEN THE Backend_API SHALL return an error "Survey already submitted"

### Requirement 8: Survey Submission Tracking

**User Story:** Là một traveler, tôi muốn xem lịch sử các survey tôi đã gửi, để theo dõi các đánh giá của mình.

#### Acceptance Criteria

1. WHEN a traveler navigates to their profile, THE Mobile_App SHALL display a "Đánh giá của tôi" section
2. WHEN "Đánh giá của tôi" section is displayed, THE Mobile_App SHALL fetch all surveys submitted by the traveler from Backend_API
3. WHEN displaying survey history, THE Mobile_App SHALL show tour title, rating, comment, and submission date for each survey
4. WHEN displaying survey history, THE Mobile_App SHALL sort surveys by submission date in descending order
5. WHEN a traveler taps on a survey in history, THE Mobile_App SHALL navigate to the corresponding tour detail screen

### Requirement 9: Incentive for Future Bookings

**User Story:** Là một traveler, tôi muốn nhận ưu đãi sau khi gửi survey, để được khuyến khích đặt tour tiếp theo.

#### Acceptance Criteria

1. WHEN Backend_API stores a survey successfully, THE Backend_API SHALL check if this is the traveler's first survey submission
2. IF this is the traveler's first survey, THEN THE Backend_API SHALL create a discount voucher with 5% off for the traveler
3. WHEN discount voucher is created, THE Backend_API SHALL set the voucher expiration date to 30 days from creation
4. WHEN discount voucher is created, THE Backend_API SHALL send a notification to the traveler about the voucher
5. WHEN Mobile_App displays post-survey success screen, THE Mobile_App SHALL show the discount voucher information if applicable

### Requirement 10: Survey Data Analytics

**User Story:** Là một admin, tôi muốn xem thống kê về các survey đã được gửi, để đánh giá chất lượng dịch vụ tổng thể.

#### Acceptance Criteria

1. WHEN admin accesses the analytics dashboard, THE Backend_API SHALL calculate total number of surveys submitted
2. WHEN admin accesses the analytics dashboard, THE Backend_API SHALL calculate average rating across all tours
3. WHEN admin accesses the analytics dashboard, THE Backend_API SHALL identify tours with highest and lowest ratings
4. WHEN admin accesses the analytics dashboard, THE Backend_API SHALL calculate survey submission rate (surveys / completed bookings)
5. WHEN displaying analytics, THE Mobile_App SHALL present data in charts and tables for easy visualization
