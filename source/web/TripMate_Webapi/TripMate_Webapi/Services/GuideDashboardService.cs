using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TripMate_WebAPI.DTOs;
using TripMate_Webapi.Entities;
using TripMate_Webapi.Repositories;

namespace TripMate_WebAPI.Services
{
    public class GuideDashboardService : IGuideDashboardService
    {
        private readonly IGuideRepository _guideRepo;
        private readonly IBookingRepository _bookingRepo;
        private readonly IExperiencePackageRepository _packageRepo;

        public GuideDashboardService(
            IGuideRepository guideRepo,
            IBookingRepository bookingRepo,
            IExperiencePackageRepository packageRepo)
        {
            _guideRepo = guideRepo;
            _bookingRepo = bookingRepo;
            _packageRepo = packageRepo;
        }

        public async Task<GuideDashboardViewModel> BuildDashboardAsync(string userId)
        {
            var vm = new GuideDashboardViewModel();

            // 1. Get Guide Profile
            var guideProfile = await _guideRepo.GetGuideProfileByUserIdAsync(userId);
            if (guideProfile == null)
            {
                // Fallback if not found (or return empty model, though user shouldn't reach here if auth'd properly)
                return vm;
            }

            vm.GuideName = guideProfile.Profile?.FullName ?? "Guide";
            vm.AvatarUrl = guideProfile.Profile?.AvatarUrl ?? "/images/AVATAR.png";
            vm.Location = guideProfile.CityArea ?? "Chưa cập nhật";
            vm.IsVerified = guideProfile.IsVerified ?? false;
            vm.AverageRating = guideProfile.AverageRating ?? 0;
            vm.ReviewsCount = guideProfile.TotalReviews ?? 0;
            vm.ProfileViewsThisMonth = guideProfile.TotalViews; // Or ViewsThisMonth if we implement it

            // 2. Get all Bookings for this Guide
            var bookings = await _bookingRepo.GetBookingsForGuideAsync(guideProfile.Id);

            // 3. Compute Metrics
            var now = DateTime.UtcNow;
            var currentMonthBookings = bookings.Where(b => b.CreatedAt.Year == now.Year && b.CreatedAt.Month == now.Month).ToList();
            var lastMonth = now.AddMonths(-1);
            var lastMonthBookings = bookings.Where(b => b.CreatedAt.Year == lastMonth.Year && b.CreatedAt.Month == lastMonth.Month).ToList();

            vm.TotalBookings = currentMonthBookings.Count;
            vm.PendingBookingsCount = bookings.Count(b => b.Status == 0);

            // Earnings
            vm.TotalEarnings = currentMonthBookings
                .Where(b => b.Status == 1 || b.Status == 2) // Confirmed or Completed
                .Sum(b => b.GuideEarnings);

            var lastMonthEarnings = lastMonthBookings
                .Where(b => b.Status == 1 || b.Status == 2)
                .Sum(b => b.GuideEarnings);

            if (lastMonthEarnings > 0)
            {
                vm.EarningsGrowth = Math.Round(((vm.TotalEarnings - lastMonthEarnings) / lastMonthEarnings) * 100, 1);
            }
            else
            {
                vm.EarningsGrowth = vm.TotalEarnings > 0 ? 100 : 0;
            }

            // Acceptance Rate
            var resolvedBookings = bookings.Where(b => b.Status != 0).ToList();
            if (resolvedBookings.Any())
            {
                var acceptedCount = resolvedBookings.Count(b => b.Status == 1 || b.Status == 2);
                vm.AcceptanceRate = (int)Math.Round((double)acceptedCount / resolvedBookings.Count * 100);
            }
            else
            {
                vm.AcceptanceRate = 100; // Default if no resolved bookings
            }

            // Response Time
            var respondedBookings = bookings.Where(b => b.GuideResponseAt != null).ToList();
            if (respondedBookings.Any())
            {
                var totalMinutes = respondedBookings.Sum(b => (b.GuideResponseAt!.Value - b.CreatedAt).TotalMinutes);
                vm.ResponseTimeMinutes = (int)Math.Round(totalMinutes / respondedBookings.Count);
            }
            else
            {
                vm.ResponseTimeMinutes = 15; // Default mock value if none responded yet
            }

            // 4. Earnings Sparkline (12 months)
            var sparkline = new List<decimal>();
            for (int i = 1; i <= 12; i++)
            {
                var monthEarnings = bookings
                    .Where(b => b.CreatedAt.Year == now.Year && b.CreatedAt.Month == i && (b.Status == 1 || b.Status == 2))
                    .Sum(b => b.GuideEarnings);
                sparkline.Add(monthEarnings);
            }
            vm.EarningsSparkline = sparkline;

            // 5. Active Tours
            var tours = await _packageRepo.GetPackagesByGuideIdAsync(guideProfile.Id);
            vm.ActiveTours = tours.Count(t => t.IsActive);

            // 6. Recent Bookings (Top 4)
            vm.RecentBookings = bookings
                .OrderByDescending(b => b.CreatedAt)
                .Take(4)
                .Select(b => new GuideBookingItem
                {
                    TravelerName = b.Traveler?.FullName ?? "Traveler",
                    TravelerAvatar = b.Traveler?.AvatarUrl ?? "/images/AVATAR.png",
                    TourName = b.ExperiencePackage?.Title ?? "Tour",
                    Date = b.BookingDate.ToString("dd/MM/yyyy"),
                    Time = b.StartTime.ToString("HH:mm"),
                    Status = GetStatusString(b.Status),
                    Amount = b.TotalAmount,
                    Guests = b.GuestCount
                }).ToList();

            // 7. Upcoming Schedule
            vm.UpcomingSchedule = bookings
                .Where(b => b.Status == 1 && b.StartTime >= now)
                .OrderBy(b => b.StartTime)
                .Take(3)
                .Select(b => new UpcomingTourItem
                {
                    TourName = b.ExperiencePackage?.Title ?? "Tour",
                    TravelerName = b.Traveler?.FullName ?? "Traveler",
                    Date = b.StartTime.ToString("dd/MM/yyyy"),
                    Time = b.StartTime.ToString("HH:mm"),
                    Guests = b.GuestCount,
                    Status = "Confirmed"
                }).ToList();

            // 8. Recent Activities (Mock for MVP)
            vm.RecentActivities = new List<ActivityItem>
            {
                new ActivityItem { Icon = "person_add", Title = "Booking mới", Description = "Phạm Thị D đặt tour Hội An Phố Cổ", TimeAgo = "2 giờ trước", IconBgClass = "bg-green-100", IconTextClass = "text-green-600" },
                new ActivityItem { Icon = "star", Title = "Đánh giá mới", Description = "Trần Thị B đánh giá 5 sao cho tour Sapa", TimeAgo = "5 giờ trước", IconBgClass = "bg-yellow-100", IconTextClass = "text-yellow-600" },
                new ActivityItem { Icon = "check_circle", Title = "Tour hoàn thành", Description = "Tour Đà Nẵng - Hội An kết thúc thành công", TimeAgo = "1 ngày trước", IconBgClass = "bg-blue-100", IconTextClass = "text-blue-600" },
                new ActivityItem { Icon = "payments", Title = "Nhận thanh toán", Description = "₫2,500,000 từ booking #1234", TimeAgo = "2 ngày trước", IconBgClass = "bg-primary/10", IconTextClass = "text-primary" }
            };

            return vm;
        }

        private string GetStatusString(int status)
        {
            return status switch
            {
                0 => "Pending",
                1 => "Confirmed",
                2 => "Completed",
                3 => "Cancelled",
                _ => "Pending"
            };
        }
    }
}
