using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories
{
    public interface IReviewRepository
    {
        /// <summary>
        /// Tạo review mới. Traveler chỉ được review 1 lần / booking.
        /// </summary>
        Task<ReviewEntity> CreateReviewAsync(ReviewEntity review);

        /// <summary>
        /// Lấy tất cả reviews của một Guide (dùng cho Guide profile page).
        /// </summary>
        Task<List<ReviewEntity>> GetReviewsByGuideAsync(string guideProfileId);

        /// <summary>
        /// Kiểm tra xem booking này đã được review chưa (tránh review 2 lần).
        /// </summary>
        Task<bool> HasReviewForBookingAsync(string bookingId);
    }
}
