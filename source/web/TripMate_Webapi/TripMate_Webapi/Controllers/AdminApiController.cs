using System.Security.Claims;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using TripMate_WebAPI.Services;

namespace TripMate_Webapi.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize]
    public class AdminApiController : ControllerBase
    {
        private readonly AdminService _adminService;
        private readonly GuideApprovalService _guideApprovalService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AdminApiController> _logger;

        public AdminApiController(
            AdminService adminService,
            GuideApprovalService guideApprovalService,
            IEmailService emailService,
            ILogger<AdminApiController> logger)
        {
            _adminService = adminService;
            _guideApprovalService = guideApprovalService;
            _emailService = emailService;
            _logger = logger;
        }

        private bool IsAdmin()
        {
            // 1. Fallback for seed user email
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            if (email == "admin@tripmate.com" || email == "admin2@tripmate.com") return true;

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
        [HttpPatch("guides/{id}/verify")]
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
        [HttpPost("guides/{id}/reject")]
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
        [HttpPost("escrow/release-bulk")]
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
        [HttpPut("bookings/{id}/fee")]
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
        [HttpPost("bookings/{id}/cancel-approve")]
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
        [HttpGet("escrow/export")]
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

        // GET: /api/admin/users/export
        [HttpGet("users/export")]
        public async Task<IActionResult> ExportUsers()
        {
            if (!IsAdmin()) return Forbid();

            try
            {
                var users = await _adminService.GetUsersAsync();

                var builder = new StringBuilder();
                builder.AppendLine("ID,Email,Full Name,Phone Number,Role,Is Active,Created At");

                foreach (var u in users)
                {
                    var id = u.Id ?? "";
                    var email = u.Email ?? "";
                    var fullName = (u.FullName ?? "").Replace("\"", "\"\"");
                    if (fullName.Contains(",")) fullName = $"\"{fullName}\"";
                    var phone = u.PhoneNumber ?? "";
                    var role = u.Role ?? "";
                    var isActive = u.IsActive ? "Yes" : "No";
                    var createdAt = u.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

                    builder.AppendLine($"{id},{email},{fullName},{phone},{role},{isActive},{createdAt}");
                }

                var content = Encoding.UTF8.GetBytes(builder.ToString());
                var bom = new byte[] { 0xEF, 0xBB, 0xBF };
                var fileContent = new byte[bom.Length + content.Length];
                Buffer.BlockCopy(bom, 0, fileContent, 0, bom.Length);
                Buffer.BlockCopy(content, 0, fileContent, bom.Length, content.Length);

                return File(
                    fileContent,
                    "text/csv",
                    $"users_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting users");
                return StatusCode(500, new { message = "File export error" });
            }
        }

        // POST: /api/admin/users/{id}/role
        [HttpPost("users/{id}/role")]
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
        [HttpPost("users/{id}/toggle-status")]
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
        [HttpGet("reviews")]
        public async Task<IActionResult> GetReviews()
        {
            if (!IsAdmin()) return Forbid();

            var reviews = await _adminService.GetReviewsAsync();
            return Ok(reviews);
        }

        // POST: /api/admin/reviews/{id}/moderate
        [HttpPost("reviews/{id}/moderate")]
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
        [HttpGet("kpi")]
        public async Task<IActionResult> GetKpis()
        {
            if (!IsAdmin()) return Forbid();

            var kpi = await _adminService.GetKpisAsync();
            return Ok(kpi);
        }

        // GET: /api/admin/bookings
        [HttpGet("bookings")]
        public async Task<IActionResult> GetBookings()
        {
            if (!IsAdmin()) return Forbid();

            var bookings = await _adminService.GetBookingsAsync();
            return Ok(bookings);
        }

        // GET: /api/admin/chat/logs
        [HttpGet("chat/logs")]
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
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            if (!IsAdmin()) return Forbid();

            var users = await _adminService.GetUsersAsync();
            return Ok(users);
        }

        // GET: /api/admin/guides
        [HttpGet("guides")]
        public async Task<IActionResult> GetGuides()
        {
            if (!IsAdmin()) return Forbid();

            var guides = await _adminService.GetGuidesAsync();
            return Ok(guides);
        }

        // PATCH: /api/admin/guides/{id}/status
        [HttpPatch("guides/{id}/status")]
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
        [HttpPatch("guides/{id}/toggle-verify")]
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
}
