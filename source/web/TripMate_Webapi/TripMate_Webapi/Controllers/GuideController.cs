using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;

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
        public IActionResult Profile(string id = "1")
        {
            return View();
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
