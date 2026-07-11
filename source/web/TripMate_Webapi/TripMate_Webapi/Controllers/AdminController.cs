using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;
using System.Text.Json;
using System.Security.Claims;
using ClosedXML.Excel;

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
                var realTotalRevenue = kpis.PlatformRevenue; 
                var realNewBookings = bookings.Count;
                var realActiveUsers = users.Count(u => u.IsActive).ToString(); 
                var realBookingProgress = (int)Math.Min(100, (realNewBookings * 100.0) / 50.0); // Target: 50 bookings

                // Compute revenue growth dynamically (This month platform fee vs last month)
                var now = DateTime.UtcNow;
                var thisMonthBookings = bookings.Where(b => b.CreatedAt.Month == now.Month && b.CreatedAt.Year == now.Year && b.Status == 2).ToList();
                var lastMonthBookings = bookings.Where(b => b.CreatedAt.Month == now.AddMonths(-1).Month && b.CreatedAt.Year == now.AddMonths(-1).Year && b.Status == 2).ToList();
                
                decimal thisMonthRevenue = thisMonthBookings.Sum(b => b.PlatformFee);
                decimal lastMonthRevenue = lastMonthBookings.Sum(b => b.PlatformFee);
                decimal revenueGrowth = 0.0m;
                if (lastMonthRevenue > 0)
                {
                    revenueGrowth = Math.Round(((thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100, 1);
                }
                else if (thisMonthRevenue > 0)
                {
                    revenueGrowth = 100.0m; // 100% growth if there is new revenue and none last month
                }
                
                // Load pending applications
                var pendingApps = await _guideApprovalService.GetPendingApplicationsAsync();

                // Prepare view model
                var viewModel = new DashboardViewModel
                {
                    AdminName = "Admin User",
                    AdminRole = "Super Admin",
                    DateRange = $"{DateTime.Now.AddDays(-7):MMM dd, yyyy} - {DateTime.Now:MMM dd, yyyy}",
                    TotalRevenue = realTotalRevenue,
                    RevenueGrowth = revenueGrowth, 
                    NewBookings = realNewBookings,
                    BookingProgress = realBookingProgress, 
                    ActiveUsers = realActiveUsers,
                    PendingCount = pendingGuidesCount,
                    PendingGuidesCount = pendingGuidesCount,
                    PendingTours = tours.Take(3).ToList(),
                    PendingApplications = pendingApps.Take(3).ToList(),
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

        // GET: /Admin/Users
        public async Task<IActionResult> Users()
        {
            try
            {
                var users = await _adminService.GetUsersAsync();
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users list view");
                return View(new List<TripMate_WebAPI.DTOs.Auth.ProfileRow>());
            }
        }

        // GET: /Admin/Bookings
        public async Task<IActionResult> Bookings()
        {
            try
            {
                var bookings = await _adminService.GetBookingsAsync();
                return View(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bookings list view");
                return View(new List<AdminBookingRow>());
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
                    .Select(g => new { name = g.Key ?? "", count = g.Count() })
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

        // 👱‍♀️ ponytail: merged API endpoints


        private bool IsAdmin()
        {
            // 1. Fallback for seed user email
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            if (email == "admin@tripmate.com") return true;

            // 2. Main claim check (JWT user_metadata claim)
            var metadataClaim = User.FindFirst("user_metadata")?.Value;
            if (!string.IsNullOrEmpty(metadataClaim))
            {
                try
                {
                    using var doc = JsonDocument.Parse(metadataClaim);
                    if (doc.RootElement.TryGetProperty("role", out var roleProp))
                    {
                        return roleProp.GetString() == "admin";
                    }
                }
                catch {}
            }
            return false;
        }

        // PATCH: /api/admin/guides/{id}/verify
        [HttpPatch("/api/admin/guides/{id}/verify")]
        public async Task<IActionResult> VerifyGuide(string id, [FromBody] GuideVerifyRequest? body)
        {
            if (!IsAdmin()) return Forbid();

            try
            {
                var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "").Trim();
                var application = await _guideApprovalService.GetApplicationByIdAsync(id);
                if (application == null) return NotFound(new { message = "Guide profile not found" });

                var success = await _guideApprovalService.ApproveGuideAsync(id, body?.Comment ?? "", token);
                if (success)
                {
                    if (!string.IsNullOrEmpty(application.Email))
                    {
                        var loginLink = $"{Request.Scheme}://{Request.Host}/Auth/Login";
                        await _emailService.SendGuideApprovalEmailAsync(
                            application.Email,
                            application.Full_Name ?? "Guide",
                            true,
                            body?.Comment ?? "Congratulations! Your Guide profile has been approved on TripMate!",
                            loginLink);
                    }
                    return Ok(new { message = "Guide approved successfully!" });
                }
                return BadRequest(new { message = "Failed to approve guide" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying guide {Id}", id);
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        // POST: /api/admin/guides/{id}/reject
        [HttpPost("/api/admin/guides/{id}/reject")]
        public async Task<IActionResult> RejectGuide(string id, [FromBody] GuideVerifyRequest body)
        {
            if (!IsAdmin()) return Forbid();

            if (string.IsNullOrWhiteSpace(body.Comment))
            {
                return BadRequest(new { message = "Rejection reason is required" });
            }

            try
            {
                var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "").Trim();
                var application = await _guideApprovalService.GetApplicationByIdAsync(id);
                if (application == null) return NotFound(new { message = "Guide profile not found" });

                var success = await _guideApprovalService.RejectGuideAsync(id, body.Comment, token);
                if (success)
                {
                    if (!string.IsNullOrEmpty(application.Email))
                    {
                        await _emailService.SendGuideApprovalEmailAsync(
                            application.Email,
                            application.Full_Name ?? "Guide",
                            false,
                            body.Comment,
                            string.Empty);
                    }
                    return Ok(new { message = "Guide registration rejected." });
                }
                return BadRequest(new { message = "Failed to reject registration" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting guide {Id}", id);
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        // POST: /api/admin/escrow/release-bulk
        [HttpPost("/api/admin/escrow/release-bulk")]
        public async Task<IActionResult> ReleaseEscrowBulk([FromBody] BulkReleaseRequest request)
        {
            if (!IsAdmin()) return Forbid();

            if (request.BookingIds == null || !request.BookingIds.Any())
            {
                return BadRequest(new { message = "ID list cannot be empty" });
            }

            var success = await _adminService.ReleaseEscrowBulkAsync(request.BookingIds);
            if (success)
            {
                return Ok(new { message = "Escrow released successfully for selected bookings." });
            }
            return BadRequest(new { message = "Escrow release failed or encountered an error." });
        }

        // PUT: /api/admin/bookings/{id}/fee
        [HttpPut("/api/admin/bookings/{id}/fee")]
        public async Task<IActionResult> OverrideFee(string id, [FromBody] FeeOverrideRequest request)
        {
            if (!IsAdmin()) return Forbid();

            if (request.PlatformFee < 0)
            {
                return BadRequest(new { message = "Invalid platform fee" });
            }

            var success = await _adminService.OverridePlatformFeeAsync(id, request.PlatformFee);
            if (success)
            {
                return Ok(new { message = "Commission updated successfully" });
            }
            return BadRequest(new { message = "Failed to update commission" });
        }

        // POST: /api/admin/bookings/{id}/cancel-approve
        [HttpPost("/api/admin/bookings/{id}/cancel-approve")]
        public async Task<IActionResult> ApproveBookingCancel(string id, [FromBody] CancelApprovalRequest request)
        {
            if (!IsAdmin()) return Forbid();

            var success = await _adminService.ApproveCancelAsync(id, request.Approve);
            if (success)
            {
                return Ok(new { message = request.Approve ? "Tour cancellation approved successfully" : "Cancellation request rejected" });
            }
            return BadRequest(new { message = "Tour cancellation processing failed" });
        }

        // GET: /api/admin/escrow/export
        [HttpGet("/api/admin/escrow/export")]
        public async Task<IActionResult> ExportTransactions()
        {
            if (!IsAdmin()) return Forbid();

            try
            {
                var entries = await _adminService.GetLedgerEntriesAsync();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Transactions");

                    // Title / Headers
                    worksheet.Cell(1, 1).Value = "Ledger ID";
                    worksheet.Cell(1, 2).Value = "Booking ID";
                    worksheet.Cell(1, 3).Value = "User ID";
                    worksheet.Cell(1, 4).Value = "Type";
                    worksheet.Cell(1, 5).Value = "Amount (₫)";
                    worksheet.Cell(1, 6).Value = "Created At";
                    worksheet.Cell(1, 7).Value = "Tour Title";

                    worksheet.Row(1).Style.Font.Bold = true;
                    worksheet.Row(1).Style.Fill.BackgroundColor = XLColor.Orange;
                    worksheet.Row(1).Style.Font.FontColor = XLColor.White;

                    for (int i = 0; i < entries.Count; i++)
                    {
                        var e = entries[i];
                        int row = i + 2;
                        worksheet.Cell(row, 1).Value = e.Id;
                        worksheet.Cell(row, 2).Value = e.BookingId;
                        worksheet.Cell(row, 3).Value = e.UserId;
                        worksheet.Cell(row, 4).Value = e.Type;
                        worksheet.Cell(row, 5).Value = e.Amount;
                        worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                        worksheet.Cell(row, 6).Value = e.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                        worksheet.Cell(row, 7).Value = e.Booking?.Package?.Title ?? "N/A";
                    }

                    worksheet.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(
                            content,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            $"transactions_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting transactions");
                return StatusCode(500, new { message = "File export error" });
            }
        }

        // POST: /api/admin/users/{id}/role
        [HttpPost("/api/admin/users/{id}/role")]
        public async Task<IActionResult> ForceUserRole(string id, [FromBody] RoleUpdateRequest request)
        {
            if (!IsAdmin()) return Forbid();

            if (string.IsNullOrWhiteSpace(request.Role))
            {
                return BadRequest(new { message = "Invalid role" });
            }

            var success = await _adminService.UpdateUserRoleAsync(id, request.Role);
            if (success)
            {
                return Ok(new { message = $"User role updated to {request.Role}." });
            }
            return BadRequest(new { message = "Failed to update user role." });
        }

        // POST: /api/admin/users/{id}/toggle-status
        [HttpPost("/api/admin/users/{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(string id, [FromBody] StatusToggleRequest request)
        {
            if (!IsAdmin()) return Forbid();

            var success = await _adminService.ToggleUserActiveAsync(id, request.IsActive);
            if (success)
            {
                return Ok(new { message = request.IsActive ? "Account activated successfully" : "Account suspended successfully" });
            }
            return BadRequest(new { message = "Failed to update account status" });
        }

        // GET: /api/admin/reviews
        [HttpGet("/api/admin/reviews")]
        public async Task<IActionResult> GetReviews()
        {
            if (!IsAdmin()) return Forbid();

            var reviews = await _adminService.GetReviewsAsync();
            return Ok(reviews);
        }

        // POST: /api/admin/reviews/{id}/moderate
        [HttpPost("/api/admin/reviews/{id}/moderate")]
        public async Task<IActionResult> ModerateReview(string id, [FromBody] ReviewModerateRequest request)
        {
            if (!IsAdmin()) return Forbid();

            var success = await _adminService.ModerateReviewAsync(id, request.AdminNote);
            if (success)
            {
                return Ok(new { message = "Review hidden and warning sent." });
            }
            return BadRequest(new { message = "Failed to moderate review" });
        }

        // GET: /api/admin/kpi
        [HttpGet("/api/admin/kpi")]
        public async Task<IActionResult> GetKpis()
        {
            if (!IsAdmin()) return Forbid();

            var kpi = await _adminService.GetKpisAsync();
            return Ok(kpi);
        }

        // GET: /api/admin/bookings
        [HttpGet("/api/admin/bookings")]
        public async Task<IActionResult> GetBookings()
        {
            if (!IsAdmin()) return Forbid();

            var bookings = await _adminService.GetBookingsAsync();
            return Ok(bookings);
        }

        // GET: /api/admin/chat/logs
        [HttpGet("/api/admin/chat/logs")]
        public async Task<IActionResult> GetChatLogs([FromQuery] string bookingId)
        {
            if (!IsAdmin()) return Forbid();

            if (string.IsNullOrEmpty(bookingId))
            {
                return BadRequest(new { message = "Booking ID cannot be empty" });
            }

            var messages = await _adminService.GetChatMessagesAsync(bookingId);
            return Ok(messages);
        }

        // GET: /api/admin/users
        [HttpGet("/api/admin/users")]
        public async Task<IActionResult> GetUsers()
        {
            if (!IsAdmin()) return Forbid();

            var users = await _adminService.GetUsersAsync();
            return Ok(users);
        }

        // GET: /api/admin/guides
        [HttpGet("/api/admin/guides")]
        public async Task<IActionResult> GetGuides()
        {
            if (!IsAdmin()) return Forbid();

            var guides = await _adminService.GetGuidesAsync();
            return Ok(guides);
        }

        // PATCH: /api/admin/guides/{id}/status
        [HttpPatch("/api/admin/guides/{id}/status")]
        public async Task<IActionResult> UpdateGuideStatus(string id, [FromBody] GuideStatusUpdateRequest request)
        {
            if (!IsAdmin()) return Forbid();

            if (string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new { message = "Invalid status" });
            }

            var success = await _adminService.UpdateGuideStatusAsync(id, request.Status);
            if (success)
            {
                return Ok(new { message = $"Guide status updated to {request.Status}." });
            }
            return BadRequest(new { message = "Failed to update guide status." });
        }

        // PATCH: /api/admin/guides/{id}/toggle-verify
        [HttpPatch("/api/admin/guides/{id}/toggle-verify")]
        public async Task<IActionResult> ToggleGuideVerification(string id, [FromBody] GuideToggleVerifyRequest request)
        {
            if (!IsAdmin()) return Forbid();

            var success = await _adminService.ToggleGuideVerificationAsync(id, request.IsVerified);
            if (success)
            {
                return Ok(new { message = request.IsVerified ? "Verification badge granted to guide" : "Verification badge revoked from guide" });
            }
            return BadRequest(new { message = "Failed to update guide verification status" });
        }
        }

    // Request DTOs
    public class GuideVerifyRequest
    {
        public string? Comment { get; set; }
    }

    public class BulkReleaseRequest
    {
        public List<string>? BookingIds { get; set; }
    }

    public class FeeOverrideRequest
    {
        public decimal PlatformFee { get; set; }
    }

    public class CancelApprovalRequest
    {
        public bool Approve { get; set; }
    }

    public class RoleUpdateRequest
    {
        public string? Role { get; set; }
    }

    public class StatusToggleRequest
    {
        public bool IsActive { get; set; }
    }

    public class ReviewModerateRequest
    {
        public string AdminNote { get; set; } = string.Empty;
    }

    public class GuideStatusUpdateRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class GuideToggleVerifyRequest
    {
        public bool IsVerified { get; set; }
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
        public List<GuideApplicationRow> PendingApplications { get; set; } = new();
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
