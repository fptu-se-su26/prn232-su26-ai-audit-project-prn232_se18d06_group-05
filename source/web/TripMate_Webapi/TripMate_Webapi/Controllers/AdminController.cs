using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;

namespace TripMate_Webapi.Controllers
{
    public class AdminController : Controller
    {
        private readonly TourService _tourService;
        private readonly BookingService _bookingService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            TourService tourService, 
            BookingService bookingService,
            ILogger<AdminController> logger)
        {
            _tourService = tourService;
            _bookingService = bookingService;
            _logger = logger;
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Load data from services
                var tours = await _tourService.GetToursAsync();
                
                // Prepare view model
                var viewModel = new DashboardViewModel
                {
                    AdminName = "Admin User",
                    AdminRole = "Super Admin",
                    DateRange = $"{DateTime.Now.AddDays(-7):MMM dd, yyyy} - {DateTime.Now:MMM dd, yyyy}",
                    TotalRevenue = 1284500,
                    RevenueGrowth = 12.5m,
                    NewBookings = 482,
                    BookingProgress = 75,
                    ActiveUsers = "12.4k",
                    PendingCount = tours.Count(),
                    PendingTours = tours.Take(3).ToList(),
                    RecentActivities = GetRecentActivities()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                return View(new DashboardViewModel());
            }
        }

        // GET: /Admin/Survey
        public IActionResult Survey()
        {
            return View();
        }

        private List<ActivityItem> GetRecentActivities()
        {
            return new List<ActivityItem>
            {
                new ActivityItem
                {
                    Icon = "shopping_bag",
                    IconBgClass = "bg-primary",
                    IconTextClass = "text-white",
                    Title = "New booking confirmed",
                    Description = "Ha Long Bay Tour",
                    TimeAgo = "Just Now"
                },
                new ActivityItem
                {
                    Icon = "verified_user",
                    IconBgClass = "bg-blue-100",
                    IconTextClass = "text-blue-600",
                    Title = "Guide verification complete",
                    Description = "Nguyen Van A updated credentials",
                    TimeAgo = "2 Hours Ago"
                },
                new ActivityItem
                {
                    Icon = "chat",
                    IconBgClass = "bg-gray-200",
                    IconTextClass = "text-gray-600",
                    Title = "Customer Inquiry",
                    Description = "Private tour request in Sapa",
                    TimeAgo = "4 Hours Ago"
                },
                new ActivityItem
                {
                    Icon = "star",
                    IconBgClass = "bg-orange-100",
                    IconTextClass = "text-primary",
                    Title = "New 5-star review",
                    Description = "\"Exceptional experience!\"",
                    TimeAgo = "Yesterday"
                }
            };
        }
    }

    // View Models
    public class DashboardViewModel
    {
        public string AdminName { get; set; } = "Admin";
        public string AdminRole { get; set; } = "Super Admin";
        public string DateRange { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public decimal RevenueGrowth { get; set; }
        public int NewBookings { get; set; }
        public int BookingProgress { get; set; }
        public string ActiveUsers { get; set; } = "0";
        public int PendingCount { get; set; }
        public List<TourRow> PendingTours { get; set; } = new();
        public List<ActivityItem> RecentActivities { get; set; } = new();
    }

    public class ActivityItem
    {
        public string Icon { get; set; } = string.Empty;
        public string IconBgClass { get; set; } = string.Empty;
        public string IconTextClass { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
    }
}
