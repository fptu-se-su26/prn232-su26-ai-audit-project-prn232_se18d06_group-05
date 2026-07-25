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
            vm.Location = guideProfile.CityArea ?? "Not updated";
            vm.IsVerified = guideProfile.IsVerified ?? false;
            vm.AverageRating = guideProfile.AverageRating ?? 0;
            vm.ReviewsCount = guideProfile.TotalReviews ?? 0;
            vm.ProfileViewsThisMonth = guideProfile.TotalViews; // Or ViewsThisMonth if we implement it

            // Bookings and tours are independent once the guide profile is known.
            var bookingsTask = _bookingRepo.GetBookingsForGuideAsync(guideProfile.Id);
            var toursTask = _packageRepo.GetPackagesByGuideIdAsync(guideProfile.Id);
            await Task.WhenAll(bookingsTask, toursTask);

            var bookings = await bookingsTask;
            var tours = await toursTask;

            // 3. Compute Metrics
            var now = DateTime.UtcNow;
            vm.EarningsYear = now.Year;
            var currentMonthBookings = bookings.Where(b => b.CreatedAt.Year == now.Year && b.CreatedAt.Month == now.Month).ToList();
            var lastMonth = now.AddMonths(-1);

            vm.TotalBookings = currentMonthBookings.Count;
            var pendingBookings = bookings
                .Where(b => BookingResponsePolicy.IsAwaitingGuideResponse(b.Status, b.CreatedAt, now))
                .ToList();
            vm.PendingBookingsCount = pendingBookings.Count;
            if (pendingBookings.Any())
            {
                var nearestDeadline = pendingBookings
                    .Select(b => BookingResponsePolicy.GetDeadlineUtc(b.CreatedAt))
                    .Min();
                vm.PendingResponseTimeRemaining = FormatDuration(nearestDeadline - now);
            }

            // A completed booking is released by the admin flow. UpdatedAt therefore
            // represents when this amount became guide earnings more accurately than CreatedAt.
            var completedBookings = bookings.Where(b => b.Status == 2).ToList();
            vm.TotalEarnings = completedBookings
                .Where(b => IsInMonth(b.UpdatedAt, now.Year, now.Month))
                .Sum(b => b.GuideEarnings);

            var lastMonthEarnings = completedBookings
                .Where(b => IsInMonth(b.UpdatedAt, lastMonth.Year, lastMonth.Month))
                .Sum(b => b.GuideEarnings);
            vm.HasPreviousMonthEarnings = lastMonthEarnings > 0;

            if (lastMonthEarnings > 0)
            {
                vm.EarningsGrowth = Math.Round(((vm.TotalEarnings - lastMonthEarnings) / lastMonthEarnings) * 100, 1);
            }
            else
            {
                vm.EarningsGrowth = 0;
            }

            // Acceptance Rate
            var resolvedBookings = bookings.Where(b => b.Status != 0).ToList();
            if (resolvedBookings.Any())
            {
                var acceptedCount = resolvedBookings.Count(b => b.Status == 1 || b.Status == 2);
                vm.AcceptanceRate = (int)Math.Round((double)acceptedCount / resolvedBookings.Count * 100);
                vm.HasAcceptanceRateData = true;
            }
            else
            {
                vm.AcceptanceRate = 0;
                vm.HasAcceptanceRateData = false;
            }

            // Response Time (estimate from UpdatedAt - CreatedAt for responded bookings)
            var respondedBookings = bookings.Where(b => b.Status != 0 && b.UpdatedAt > b.CreatedAt).ToList();
            if (respondedBookings.Any())
            {
                var totalMinutes = respondedBookings.Sum(b => (b.UpdatedAt - b.CreatedAt).TotalMinutes);
                vm.ResponseTimeMinutes = (int)Math.Round(totalMinutes / respondedBookings.Count);
                vm.HasResponseTimeData = true;
            }
            else
            {
                vm.ResponseTimeMinutes = 0;
                vm.HasResponseTimeData = false;
            }

            // 4. Earnings Sparkline (12 months)
            var sparkline = new List<decimal>();
            for (int i = 1; i <= 12; i++)
            {
                var monthEarnings = completedBookings
                    .Where(b => IsInMonth(b.UpdatedAt, now.Year, i))
                    .Sum(b => b.GuideEarnings);
                sparkline.Add(monthEarnings);
            }
            vm.EarningsSparkline = sparkline;
            vm.YearlyEarnings = sparkline.Sum();

            // 5. Active Tours
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
                    Status = GetStatusString(b.Status, b.CreatedAt, now),
                    Amount = b.GuideEarnings,
                    Guests = b.GuestCount
                }).ToList();

            // 7. Upcoming Schedule
            vm.UpcomingSchedule = bookings
                .Where(b => b.Status == 1 && b.BookingDate.Date.Add(b.StartTime.TimeOfDay) >= now)
                .OrderBy(b => b.BookingDate.Date.Add(b.StartTime.TimeOfDay))
                .Take(3)
                .Select(b => new UpcomingTourItem
                {
                    TourName = b.ExperiencePackage?.Title ?? "Tour",
                    TravelerName = b.Traveler?.FullName ?? "Traveler",
                    Date = b.BookingDate.ToString("dd/MM/yyyy"),
                    Time = b.StartTime.ToString("HH:mm"),
                    Guests = b.GuestCount,
                    Status = "Confirmed",
                    StartsIn = FormatDuration(b.BookingDate.Date.Add(b.StartTime.TimeOfDay) - now)
                }).ToList();

            // 8. Recent Activities (Mock for MVP)
            vm.RecentActivities = new List<ActivityItem>
            {
                new ActivityItem { Icon = "person_add", Title = "New booking", Description = "Phạm Thị D booked the Hoi An Ancient Town tour", TimeAgo = "2 hours ago", IconBgClass = "bg-green-100", IconTextClass = "text-green-600" },
                new ActivityItem { Icon = "star", Title = "New review", Description = "Trần Thị B left a 5-star review for the Sapa tour", TimeAgo = "5 hours ago", IconBgClass = "bg-yellow-100", IconTextClass = "text-yellow-600" },
                new ActivityItem { Icon = "check_circle", Title = "Tour completed", Description = "The Da Nang - Hoi An tour was completed successfully", TimeAgo = "1 day ago", IconBgClass = "bg-blue-100", IconTextClass = "text-blue-600" },
                new ActivityItem { Icon = "payments", Title = "Payment received", Description = "₫2,500,000 from booking #1234", TimeAgo = "2 days ago", IconBgClass = "bg-primary/10", IconTextClass = "text-primary" }
            };

            return vm;
        }

        private static bool IsInMonth(DateTime value, int year, int month)
        {
            var utcValue = BookingResponsePolicy.AsUtc(value);
            return utcValue.Year == year && utcValue.Month == month;
        }

        private static string GetStatusString(int status, DateTime createdAt, DateTime utcNow)
        {
            if (BookingResponsePolicy.IsExpired(status, createdAt, utcNow))
            {
                return "Expired";
            }

            return status switch
            {
                -1 => "Pending payment",
                0 => "Pending",
                1 => "Confirmed",
                2 => "Completed",
                3 => "Cancelled",
                _ => "Pending"
            };
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
            {
                return "Starting soon";
            }

            if (duration.TotalDays >= 1)
            {
                var days = (int)Math.Floor(duration.TotalDays);
                var hours = duration.Hours;
                return hours > 0 ? $"{days}d {hours}h remaining" : $"{days}d remaining";
            }

            if (duration.TotalHours >= 1)
            {
                return $"{(int)Math.Floor(duration.TotalHours)}h {duration.Minutes}m remaining";
            }

            return $"{Math.Max(1, duration.Minutes)}m remaining";
        }
    }
}
