using System.Collections.Generic;

namespace TripMate_WebAPI.DTOs
{
    public class GuideDashboardViewModel
    {
        // Identity
        public string GuideName { get; set; } = string.Empty;
        public string GuideRole { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = "/images/AVATAR.png";
        public string Location { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        
        // Metrics
        public decimal TotalEarnings { get; set; }
        public decimal EarningsGrowth { get; set; }
        public int ActiveTours { get; set; }
        public int TotalBookings { get; set; }
        public int PendingBookingsCount { get; set; }
        public int AcceptanceRate { get; set; }
        public int ResponseTimeMinutes { get; set; }
        public int ProfileViewsThisMonth { get; set; }
        public decimal AverageRating { get; set; }
        public int ReviewsCount { get; set; }
        
        // Legacy (kept for compat)
        public string DateRange { get; set; } = "Tháng này";
        public int BookingProgress { get; set; } = 75;
        
        // Chart
        public List<decimal> EarningsSparkline { get; set; } = new();
        
        // Data
        public List<ExperiencePackageRow> MyTours { get; set; } = new();
        public List<GuideBookingItem> RecentBookings { get; set; } = new();
        public List<UpcomingTourItem> UpcomingSchedule { get; set; } = new();
        public List<ActivityItem> RecentActivities { get; set; } = new();
    }

    public class ExperiencePackageRow
    {
        // Add necessary properties here if used by Dashboard, 
        // normally Dashboard doesn't show MyTours directly in the hero but maybe it does.
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class GuideBookingItem
    {
        public string TravelerName { get; set; } = string.Empty;
        public string TravelerAvatar { get; set; } = "/images/AVATAR.png";
        public string TourName { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Guests { get; set; }
    }

    public class UpcomingTourItem
    {
        public string TourName { get; set; } = string.Empty;
        public string TravelerName { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public int Guests { get; set; }
        public string Status { get; set; } = "Confirmed";
    }

    public class ActivityItem
    {
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public string IconBgClass { get; set; } = string.Empty;
        public string IconTextClass { get; set; } = string.Empty;
    }
}
