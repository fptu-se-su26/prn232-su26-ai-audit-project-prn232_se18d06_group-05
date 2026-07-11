using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;
using TripMate_Webapi.Entities;
using System.Security.Claims;

namespace TripMate_Webapi.Controllers
{
    public class GuideController : Controller
    {
        private readonly TourService _tourService;
        private readonly BookingService _bookingService;
        private readonly ILogger<GuideController> _logger;

        public GuideController(
            TourService tourService,
            BookingService bookingService,
            ILogger<GuideController> logger)
        {
            _tourService = tourService;
            _bookingService = bookingService;
            _logger = logger;
        }

        // GET: /Guide/Dashboard
        [HttpGet("Guide/Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Load guide's tours
                var tours = await _tourService.GetToursAsync();
                
                // Prepare view model
                var viewModel = new GuideDashboardViewModel
                {
                    GuideName = "Guide User", // TODO: Get from auth
                    GuideRole = "Tour Guide",
                    DateRange = "Last 30 days",
                    
                    // Metrics
                    TotalEarnings = 45000000, // TODO: Calculate from bookings
                    EarningsGrowth = 12.5m,
                    ActiveTours = tours.Count(),
                    TotalBookings = 24, // TODO: Get from bookings
                    BookingProgress = 75,
                    AverageRating = 4.8m,
                    
                    // Tours
                    MyTours = tours.Take(5).ToList(),
                    
                    // Recent bookings
                    RecentBookings = new List<GuideBookingItem>
                    {
                        new GuideBookingItem { TravelerName = "Nguyễn Văn A", TourName = "Hà Nội - Hạ Long", Date = "15/06/2026", Status = "Confirmed", Amount = 2500000 },
                        new GuideBookingItem { TravelerName = "Trần Thị B", TourName = "Sapa 3N2Đ", Date = "18/06/2026", Status = "Pending", Amount = 3200000 },
                        new GuideBookingItem { TravelerName = "Lê Văn C", TourName = "Đà Nẵng - Hội An", Date = "20/06/2026", Status = "Confirmed", Amount = 1800000 },
                    },
                    
                    // Recent activities
                    RecentActivities = new List<ActivityItem>
                    {
                        new ActivityItem { Icon = "person_add", Title = "New Booking", Description = "Nguyễn Văn A booked Hà Nội tour", TimeAgo = "2 hours ago", IconBgClass = "bg-green-100", IconTextClass = "text-green-600" },
                        new ActivityItem { Icon = "star", Title = "New Review", Description = "5-star review from Trần Thị B", TimeAgo = "5 hours ago", IconBgClass = "bg-yellow-100", IconTextClass = "text-yellow-600" },
                        new ActivityItem { Icon = "check_circle", Title = "Tour Completed", Description = "Sapa tour successfully completed", TimeAgo = "1 day ago", IconBgClass = "bg-blue-100", IconTextClass = "text-blue-600" },
                        new ActivityItem { Icon = "payments", Title = "Payment Received", Description = "₫2,500,000 from booking #1234", TimeAgo = "2 days ago", IconBgClass = "bg-green-100", IconTextClass = "text-green-600" },
                    }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading guide dashboard");
                return View(new GuideDashboardViewModel());
            }
        }

        // GET: /Guide/Profile/{id}
        // This is the public profile viewed by the Traveler
        [HttpGet("Guide/Profile/{id}")]
        public IActionResult Profile(string id = "1")
        {
            return View();
        }

        // ponytail ultra: minimal inline update
        public class UpdateGuideProfileDto
        {
            public string? AvatarUrl { get; set; }
            public string? Location { get; set; }
            public string? Bio { get; set; }
            public List<string>? Languages { get; set; }
            public List<string>? Specialties { get; set; }
            public string? CityArea { get; set; }
            public decimal? PricePerHour { get; set; }
            public string? CoverPhotoUrl { get; set; }
            // ponytail: certificate, phone number, full name, email explicitly excluded per requirements
        }

        [HttpPost("Guide/UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateGuideProfileDto dto, [FromServices] Supabase.Client supabase)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var profileResponse = await supabase.From<ProfileEntity>().Where(x => x.Id == userId).Get();
            var profile = profileResponse.Models.FirstOrDefault();
            if (profile != null)
            {
                if (dto.AvatarUrl != null) profile.AvatarUrl = dto.AvatarUrl;
                if (dto.Location != null) profile.Location = dto.Location;
                await supabase.From<ProfileEntity>().Update(profile);
            }

            var guideProfileResponse = await supabase.From<GuideProfileEntity>().Where(x => x.UserId == userId).Get();
            var guideProfile = guideProfileResponse.Models.FirstOrDefault();
            if (guideProfile != null)
            {
                if (dto.Bio != null) guideProfile.Bio = dto.Bio;
                if (dto.Languages != null) guideProfile.Languages = dto.Languages;
                if (dto.Specialties != null) guideProfile.Specialties = dto.Specialties;
                if (dto.CityArea != null) guideProfile.CityArea = dto.CityArea;
                if (dto.PricePerHour != null) guideProfile.PricePerHour = dto.PricePerHour;
                if (dto.CoverPhotoUrl != null) guideProfile.CoverPhotoUrl = dto.CoverPhotoUrl;
                await supabase.From<GuideProfileEntity>().Update(guideProfile);
            }

            return Ok(new { success = true });
        }
    }

    // View Models
    public class GuideDashboardViewModel
    {
        public string GuideName { get; set; } = string.Empty;
        public string GuideRole { get; set; } = string.Empty;
        public string DateRange { get; set; } = string.Empty;
        
        // Metrics
        public decimal TotalEarnings { get; set; }
        public decimal EarningsGrowth { get; set; }
        public int ActiveTours { get; set; }
        public int TotalBookings { get; set; }
        public int BookingProgress { get; set; }
        public decimal AverageRating { get; set; }
        
        // Data
        public List<ExperiencePackageRow> MyTours { get; set; } = new();
        public List<GuideBookingItem> RecentBookings { get; set; } = new();
        public List<ActivityItem> RecentActivities { get; set; } = new();
    }

    public class GuideBookingItem
    {
        public string TravelerName { get; set; } = string.Empty;
        public string TourName { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
