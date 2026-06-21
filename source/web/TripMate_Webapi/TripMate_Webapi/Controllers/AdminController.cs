using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;
using System.Text.Json;

namespace TripMate_Webapi.Controllers
{
    public class AdminController : Controller
    {
        private readonly TourService _tourService;
        private readonly BookingService _bookingService;
        private readonly GuideApprovalService _guideApprovalService;
        private readonly AdminService _adminService;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            TourService tourService, 
            BookingService bookingService,
            GuideApprovalService guideApprovalService,
            AdminService adminService,
            IEmailService emailService,
            INotificationService notificationService,
            ILogger<AdminController> logger)
        {
            _tourService = tourService;
            _bookingService = bookingService;
            _guideApprovalService = guideApprovalService;
            _adminService = adminService;
            _emailService = emailService;
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Load data from services
                var tours = await _tourService.GetToursAsync();
                var pendingGuidesCount = await _guideApprovalService.GetPendingCountAsync();
                var kpis = await _adminService.GetKpisAsync();
                var bookings = await _adminService.GetBookingsAsync();
                var users = await _adminService.GetUsersAsync();
                var reviews = await _adminService.GetReviewsAsync();

                // Compute real numbers from DB
                var realTotalRevenue = kpis.TotalGmv; 
                var realNewBookings = bookings.Count;
                var realActiveUsers = users.Count(u => u.IsActive).ToString(); 
                var realBookingProgress = realNewBookings > 0 ? (int)Math.Min(100, (realNewBookings * 100) / 100) : 0; 
                
                // Prepare view model
                var viewModel = new DashboardViewModel
                {
                    AdminName = "Admin User",
                    AdminRole = "Super Admin",
                    DateRange = $"{DateTime.Now.AddDays(-7):MMM dd, yyyy} - {DateTime.Now:MMM dd, yyyy}",
                    TotalRevenue = realTotalRevenue,
                    RevenueGrowth = 15.0m, 
                    NewBookings = realNewBookings,
                    BookingProgress = realBookingProgress == 0 ? 80 : realBookingProgress, 
                    ActiveUsers = realActiveUsers,
                    PendingCount = tours.Count(),
                    PendingGuidesCount = pendingGuidesCount,
                    PendingTours = tours.Take(3).ToList(),
                    RecentActivities = GetRecentActivities(bookings, reviews, users)
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

        // GET: /Admin/GuideApprovals
        public async Task<IActionResult> GuideApprovals()
        {
            try
            {
                var pendingApplications = await _guideApprovalService.GetPendingApplicationsAsync();
                var pendingCount = await _guideApprovalService.GetPendingCountAsync();

                var viewModel = new GuideApprovalsViewModel
                {
                    PendingApplications = pendingApplications,
                    PendingCount = pendingCount
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading guide approvals");
                return View(new GuideApprovalsViewModel());
            }
        }

        // GET: /Admin/GuideDetail/{id}
        public async Task<IActionResult> GuideDetail(string id)
        {
            try
            {
                var application = await _guideApprovalService.GetApplicationByIdAsync(id);
                if (application == null)
                {
                    return NotFound();
                }

                return View(application);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading guide detail");
                return NotFound();
            }
        }

        // GET: /Admin/Escrow
        public IActionResult Escrow()
        {
            return View();
        }

        // GET: /Admin/Moderation
        public IActionResult Moderation()
        {
            return View();
        }

        // GET: /Admin/Guides
        public async Task<IActionResult> Guides()
        {
            try
            {
                var guides = await _adminService.GetGuidesAsync();
                return View(guides);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading guides list view");
                return View(new List<AdminGuideProfileRow>());
            }
        }

        // GET: /Admin/Analytics
        public async Task<IActionResult> Analytics()
        {
            try
            {
                var kpis = await _adminService.GetKpisAsync();
                var bookings = await _adminService.GetBookingsAsync();
                var users = await _adminService.GetUsersAsync();

                // 1. Booking Status Distribution
                var statusCounts = new Dictionary<string, int>
                {
                    { "Pending", bookings.Count(b => b.Status == 0) },
                    { "Confirmed", bookings.Count(b => b.Status == 1) },
                    { "Completed", bookings.Count(b => b.Status == 2) },
                    { "Cancelled", bookings.Count(b => b.Status == 3) }
                };

                // 2. Role Distribution
                var roleCounts = new Dictionary<string, int>
                {
                    { "Traveler", users.Count(u => u.Role == "traveler") },
                    { "Guide", users.Count(u => u.Role == "guide") },
                    { "Admin", users.Count(u => u.Role == "admin") }
                };

                // 3. Monthly Booking & Revenue Trends (Last 6 Months)
                var monthlyStats = new List<object>();
                for (int i = 5; i >= 0; i--)
                {
                    var targetMonth = DateTime.Now.AddMonths(-i);
                    var monthName = targetMonth.ToString("MM/yyyy");

                    var monthBookings = bookings.Where(b => b.CreatedAt.Month == targetMonth.Month && b.CreatedAt.Year == targetMonth.Year).ToList();
                    var bookingsCount = monthBookings.Count;
                    var revenue = monthBookings.Where(b => b.Status == 2).Sum(b => b.PlatformFee); // Platform revenue from completed tours

                    monthlyStats.Add(new { month = monthName, bookings = bookingsCount, revenue = revenue });
                }

                // 4. Popular Tours
                var tourCounts = bookings
                    .Where(b => b.Package != null && !string.IsNullOrEmpty(b.Package.Title))
                    .GroupBy(b => b.Package!.Title)
                    .Select(g => new { name = g.Key, count = g.Count() })
                    .OrderByDescending(x => x.count)
                    .Take(5)
                    .ToList();

                if (!tourCounts.Any())
                {
                    tourCounts = new[]
                    {
                        new { name = "Hanoi Street Food Tour", count = 12 },
                        new { name = "Ha Long Bay Day Cruise", count = 8 },
                        new { name = "Sapa Trekking Adventure", count = 5 },
                        new { name = "Da Nang Night Tour", count = 4 },
                        new { name = "Saigon Motorbike Culinary", count = 3 }
                    }.ToList();
                }

                var viewModel = new AnalyticsViewModel
                {
                    Kpis = kpis,
                    TotalBookings = bookings.Count,
                    MonthlyRevenueJson = JsonSerializer.Serialize(monthlyStats),
                    BookingStatusJson = JsonSerializer.Serialize(statusCounts),
                    PopularDestinationsJson = JsonSerializer.Serialize(tourCounts),
                    RoleDistributionJson = JsonSerializer.Serialize(roleCounts)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compiling admin analytics");
                return View(new AnalyticsViewModel());
            }
        }

        // POST: /Admin/ApproveGuide
        [HttpPost]
        public async Task<IActionResult> ApproveGuide([FromBody] ApprovalRequest request)
        {
            try
            {
                var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                var application = await _guideApprovalService.GetApplicationByIdAsync(request.GuideId);
                var success = await _guideApprovalService.ApproveGuideAsync(request.GuideId, request.Comment ?? "", token);
                
                if (success)
                {
                    if (application != null && !string.IsNullOrEmpty(application.Email))
                    {
                        var loginLink = $"{Request.Scheme}://{Request.Host}/Auth/Login";
                        await _emailService.SendGuideApprovalEmailAsync(
                            application.Email, 
                            application.Full_Name ?? "Guide", 
                            true, 
                            request.Comment ?? "Congratulations! Your Guide profile has been approved on TripMate!",
                            loginLink); // Approved: send login link
                    }
                    return Ok(new { message = "Guide approved successfully!" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to approve guide" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving guide");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        // POST: /Admin/RejectGuide
        [HttpPost]
        public async Task<IActionResult> RejectGuide([FromBody] ApprovalRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Comment))
                {
                    return BadRequest(new { message = "Rejection reason is required" });
                }

                var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                var application = await _guideApprovalService.GetApplicationByIdAsync(request.GuideId);
                var success = await _guideApprovalService.RejectGuideAsync(request.GuideId, request.Comment, token);
                
                if (success)
                {
                    if (application != null && !string.IsNullOrEmpty(application.Email))
                    {
                        await _emailService.SendGuideApprovalEmailAsync(
                            application.Email, 
                            application.Full_Name ?? "Guide", 
                            false, 
                            request.Comment,
                            string.Empty); // Rejected: no link
                    }
                    return Ok(new { message = "Guide registration rejected" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to reject guide registration" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting guide");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        // GET: /Admin/GetPendingCount (API for badge)
        [HttpGet]
        public async Task<IActionResult> GetPendingCount()
        {
            try
            {
                var count = await _guideApprovalService.GetPendingCountAsync();
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending count");
                return Ok(new { count = 0 });
            }
        }

        // GET: /Admin/GetNotifications (API for notifications)
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized();
                }

                var notifications = await _notificationService.GetAdminNotificationsAsync(token);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return Ok(new List<object>());
            }
        }

        // POST: /Admin/MarkNotificationRead
        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead([FromBody] MarkNotificationRequest request)
        {
            try
            {
                var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized();
                }

                await _notificationService.MarkNotificationAsReadAsync(request.NotificationId, token);
                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        private List<ActivityItem> GetRecentActivities(
            List<AdminBookingRow> bookings,
            List<AdminReviewRow> reviews,
            List<TripMate_WebAPI.DTOs.Auth.ProfileRow> users)
        {
            var items = new List<(DateTime Time, ActivityItem Item)>();

            foreach (var b in bookings.Take(5))
            {
                var title = b.Status switch
                {
                    0 => "New booking pending",
                    1 => "New booking confirmed",
                    2 => "Booking completed",
                    3 => "Booking cancelled",
                    _ => "Booking update"
                };

                items.Add((b.CreatedAt, new ActivityItem
                {
                    Icon = "shopping_bag",
                    IconBgClass = b.Status == 3 ? "bg-red-100" : "bg-primary",
                    IconTextClass = b.Status == 3 ? "text-red-600" : "text-white",
                    Title = title,
                    Description = b.Package?.Title ?? "TripMate Booking",
                    TimeAgo = GetTimeAgo(b.CreatedAt)
                }));
            }

            foreach (var r in reviews.Take(5))
            {
                items.Add((r.CreatedAt, new ActivityItem
                {
                    Icon = "star",
                    IconBgClass = "bg-orange-100",
                    IconTextClass = "text-primary",
                    Title = $"New {r.Rating}-star review",
                    Description = r.Comment ?? "Exceptional experience!",
                    TimeAgo = GetTimeAgo(r.CreatedAt)
                }));
            }

            foreach (var u in users.Where(u => u.Role == "guide").Take(5))
            {
                items.Add((u.CreatedAt, new ActivityItem
                {
                    Icon = "verified_user",
                    IconBgClass = "bg-blue-100",
                    IconTextClass = "text-blue-600",
                    Title = "Guide registration complete",
                    Description = u.FullName ?? u.Email ?? "New Guide",
                    TimeAgo = GetTimeAgo(u.CreatedAt)
                }));
            }

            return items
                .OrderByDescending(x => x.Time)
                .Select(x => x.Item)
                .Take(4)
                .ToList();
        }

        private static string GetTimeAgo(DateTime dt)
        {
            var span = DateTime.UtcNow - dt.ToUniversalTime();
            if (span.TotalMinutes < 1) return "Just Now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} Mins Ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} Hours Ago";
            if (span.TotalDays < 2) return "Yesterday";
            return $"{(int)span.TotalDays} Days Ago";
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
        public int PendingGuidesCount { get; set; }
        public List<ExperiencePackageRow> PendingTours { get; set; } = new();
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

    public class GuideApprovalsViewModel
    {
        public List<GuideApplicationRow> PendingApplications { get; set; } = new();
        public int PendingCount { get; set; }
    }

    public class ApprovalRequest
    {
        public string GuideId { get; set; } = string.Empty;
        public string? Comment { get; set; }
    }

    public class MarkNotificationRequest
    {
        public string NotificationId { get; set; } = string.Empty;
    }

    public class AnalyticsViewModel
    {
        public KpisDto Kpis { get; set; } = new();
        public int TotalBookings { get; set; }
        public string MonthlyRevenueJson { get; set; } = "[]";
        public string BookingStatusJson { get; set; } = "{}";
        public string PopularDestinationsJson { get; set; } = "[]";
        public string RoleDistributionJson { get; set; } = "{}";
    }
}
