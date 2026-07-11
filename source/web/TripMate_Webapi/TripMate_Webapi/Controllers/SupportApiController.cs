using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;

namespace TripMate_Webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SupportApiController : ControllerBase
    {
        private readonly ProblemReportService _reportService;
        private readonly ILogger<SupportApiController> _logger;

        public SupportApiController(ProblemReportService reportService, ILogger<SupportApiController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        private string? UserToken =>
            Request.Headers.Authorization.ToString().Replace("Bearer ", "").Trim()
            is { Length: > 0 } t ? t : null;

        private string? UserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

        private bool IsAdmin()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            if (email == "admin@tripmate.com" || email == "admin2@tripmate.com") return true;

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

        // POST: /api/supportapi/reports
        [HttpPost("reports")]
        public async Task<IActionResult> SubmitReport([FromBody] SubmitReportRequest request)
        {
            if (string.IsNullOrEmpty(UserId) || string.IsNullOrEmpty(UserToken))
            {
                return Unauthorized(new { message = "Không tìm thấy thông tin xác thực" });
            }

            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.Type))
            {
                return BadRequest(new { message = "Vui lòng nhập đầy đủ thông tin bắt buộc (loại, tiêu đề, nội dung)" });
            }

            try
            {
                var created = await _reportService.CreateReportAsync(
                    UserId,
                    request.Type,
                    request.BookingId,
                    request.Title,
                    request.Description,
                    request.ImageUrl,
                    UserToken
                );
                return Ok(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting problem report");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: /api/supportapi/my-reports
        [HttpGet("my-reports")]
        public async Task<IActionResult> GetMyReports()
        {
            if (string.IsNullOrEmpty(UserId) || string.IsNullOrEmpty(UserToken))
            {
                return Unauthorized(new { message = "Không tìm thấy thông tin xác thực" });
            }

            try
            {
                var reports = await _reportService.GetReportsByUserAsync(UserId, UserToken);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user problem reports");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: /api/supportapi/admin/reports (Admin only)
        [HttpGet("admin/reports")]
        public async Task<IActionResult> GetAdminReports()
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            try
            {
                var reports = await _reportService.GetAllReportsAsync();
                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all reports for admin");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // PATCH: /api/supportapi/admin/reports/{id}/resolve (Admin only)
        [HttpPatch("admin/reports/{id}/resolve")]
        public async Task<IActionResult> ResolveReport(string id, [FromBody] ResolveReportRequest request)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(request.AdminComment))
            {
                return BadRequest(new { message = "Vui lòng nhập ghi chú xử lý" });
            }

            try
            {
                var success = await _reportService.ResolveReportAsync(id, request.AdminComment);
                if (success)
                {
                    return Ok(new { message = "Báo cáo sự cố đã được xử lý thành công!" });
                }
                return BadRequest(new { message = "Không thể xử lý báo cáo sự cố" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving problem report {Id}", id);
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }

    public class SubmitReportRequest
    {
        public string Type { get; set; } = string.Empty;
        public string? BookingId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    public class ResolveReportRequest
    {
        public string AdminComment { get; set; } = string.Empty;
    }
}
