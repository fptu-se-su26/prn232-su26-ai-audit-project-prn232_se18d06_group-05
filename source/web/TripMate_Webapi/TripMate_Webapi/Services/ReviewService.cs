using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TripMate_Webapi.Entities;
using TripMate_Webapi.Repositories;

namespace TripMate_WebAPI.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IGuideRepository _guideRepository;
        private readonly Supabase.Client _supabase;
        private readonly INotificationService _notifications;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(
            IReviewRepository reviewRepository,
            IBookingRepository bookingRepository,
            IGuideRepository guideRepository,
            Supabase.Client supabase,
            INotificationService notifications,
            ILogger<ReviewService> logger)
        {
            _reviewRepository = reviewRepository;
            _bookingRepository = bookingRepository;
            _guideRepository = guideRepository;
            _supabase = supabase;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> SubmitReviewAsync(string travelerId, string bookingId, int rating, string comment)
        {
            if (rating < 1 || rating > 5)
            {
                return (false, "Vui lòng chọn số sao từ 1 đến 5.");
            }

            if (string.IsNullOrWhiteSpace(comment) || comment.Length < 10)
            {
                return (false, "Nhận xét phải có ít nhất 10 ký tự.");
            }

            var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
            if (booking == null || booking.TravelerId != travelerId)
            {
                return (false, "Không tìm thấy booking. Vui lòng thử lại.");
            }

            if (booking.Status != 2)
            {
                return (false, "You can only review completed trips.");
            }

            var alreadyReviewed = await _reviewRepository.HasReviewForBookingAsync(bookingId);
            if (alreadyReviewed)
            {
                return (false, "Bạn đã đánh giá chuyến đi này rồi.");
            }

            var review = new ReviewEntity
            {
                Id = Guid.NewGuid().ToString(),
                BookingId = bookingId,
                TravelerId = travelerId,
                GuideProfileId = booking.GuideProfileId,
                Rating = rating,
                Comment = comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _reviewRepository.CreateReviewAsync(review);
                await NotifyGuideReviewAsync(booking.GuideProfileId, travelerId, bookingId, rating, booking.BookingDate);
                _logger.LogInformation("[Review] Traveler={TravelerId} rated Guide={GuideId} with {Rating}★ for Booking={BookingId}",
                    travelerId, booking.GuideProfileId, rating, bookingId);

                await RecalculateGuideRatingAsync(booking.GuideProfileId);

                return (true, $"Thank you for your {rating}★ review! Your guide will receive your feedback.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Review] Failed to save review for BookingId={BookingId}", bookingId);
                return (false, "Could not save review. Please try again later.");
            }
        }

        private async Task RecalculateGuideRatingAsync(string guideProfileId)
        {
            try
            {
                var allReviews = await _reviewRepository.GetReviewsByGuideAsync(guideProfileId);
                var totalReviews = allReviews.Count;
                var avgRating = totalReviews > 0
                    ? Math.Round(allReviews.Average(r => r.Rating), 2)
                    : 0;

                await _supabase.From<GuideProfileEntity>()
                    .Where(g => g.Id == guideProfileId)
                    .Set(g => g.AverageRating!, (decimal)avgRating)
                    .Set(g => g.TotalReviews!, totalReviews)
                    .Update();

                _logger.LogInformation("[Review] Updated Guide {GuideId}: avg={Avg}, total={Total}",
                    guideProfileId, avgRating, totalReviews);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Review] Could not recalculate rating for Guide {GuideId}", guideProfileId);
            }
        }

        private async Task NotifyGuideReviewAsync(
            string guideProfileId,
            string travelerId,
            string bookingId,
            int rating,
            DateTime bookingDate)
        {
            var guide = await _guideRepository.GetGuideByProfileIdAsync(guideProfileId);
            if (string.IsNullOrWhiteSpace(guide?.UserId)) return;
            var tripName = await _notifications.GetTripNameAsync(bookingId);
            await _notifications.SendAsync(
                guide.UserId,
                NotificationTypes.ReviewReceived,
                "New review received",
                $"A traveler rated \"{tripName}\" {rating} star(s). The trip date was {NotificationTextFormatter.FormatTripDate(bookingDate)}.",
                new { bookingId, travelerId, guideProfileId, rating },
                "/Guide/Profile",
                $"review:{bookingId}");
        }
    }
}
