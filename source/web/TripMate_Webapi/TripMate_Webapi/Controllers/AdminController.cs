using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;

namespace TripMate_Webapi.Controllers
{
    public class AdminController : Controller
    {
        private readonly TourService _tourService;
        private readonly BookingService _bookingService;
        private readonly GuideApprovalService _guideApprovalService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            TourService tourService, 
            BookingService bookingService,
            GuideApprovalService guideApprovalService,
            ILogger<AdminController> logger)
        {
            _tourService = tourService;
            _bookingService = bookingService;
            _guideApprovalService = guideApprovalService;
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
                    PendingGuidesCount = pendingGuidesCount,
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

        // POST: /Admin/ApproveGuide
        [HttpPost]
        public async Task<IActionResult> ApproveGuide([FromBody] ApprovalRequest request)
        {
            try
            {
                var success = await _guideApprovalService.ApproveGuideAsync(request.GuideId, request.Comment ?? "");
                
                if (success)
                {
                    return Ok(new { message = "Hướng dẫn viên đã được phê duyệt thành công!" });
                }
                else
                {
                    return BadRequest(new { message = "Không thể phê duyệt hướng dẫn viên" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving guide");
                return StatusCode(500, new { message = "Có lỗi xảy ra" });
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
                    return BadRequest(new { message = "Vui lòng nhập lý do từ chối" });
                }

                var success = await _guideApprovalService.RejectGuideAsync(request.GuideId, request.Comment);
                
                if (success)
                {
                    return Ok(new { message = "Đã từ chối hướng dẫn viên" });
                }
                else
                {
                    return BadRequest(new { message = "Không thể từ chối hướng dẫn viên" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting guide");
                return StatusCode(500, new { message = "Có lỗi xảy ra" });
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
        public int PendingGuidesCount { get; set; }
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
}
