using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;

namespace TripMate_Webapi.Controllers
{
    /// <summary>
    /// API Controller for Authentication (Login, Register)
    /// Routes: /api/auth/login, /api/auth/register
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly SupabaseAuthService _authService;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(SupabaseAuthService authService, ILogger<AuthApiController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Login endpoint
        /// POST /api/auth/login
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", request.Email);

                var result = await _authService.LoginAsync(request.Email, request.Password);

                if (result == null)
                {
                    _logger.LogWarning("Login failed for email: {Email}", request.Email);
                    return Unauthorized(new { message = "Email hoặc mật khẩu không đúng" });
                }

                _logger.LogInformation("Login successful for email: {Email}", request.Email);

                var userRole = result.User?.Role ?? "traveler";
                
                // Cố định role cho các tài khoản seed để tránh lỗi sai role từ Database
                if (request.Email == "admin@tripmate.com") userRole = "admin";
                if (request.Email == "guide@tripmate.com") userRole = "guide";

                return Ok(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    user = new
                    {
                        id = result.User?.Id,
                        email = result.User?.Email,
                        role = userRole
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Login error for email: {Email}", request.Email);
                // Trả message cụ thể từ Supabase/GoTrue thay vì generic 500
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Register endpoint
        /// POST /api/auth/register
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Register attempt for email: {Email}", request.Email);

                // Validate request
                if (string.IsNullOrWhiteSpace(request.Email) || 
                    string.IsNullOrWhiteSpace(request.Password) ||
                    string.IsNullOrWhiteSpace(request.FullName) ||
                    string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    return BadRequest(new { message = "Vui lòng điền đầy đủ thông tin" });
                }

                if (request.Password.Length < 6)
                {
                    return BadRequest(new { message = "Mật khẩu phải có ít nhất 6 ký tự" });
                }

                // Validate phone number
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.PhoneNumber, @"^[0-9]{10,11}$"))
                {
                    return BadRequest(new { message = "Số điện thoại không hợp lệ" });
                }

                // Guide-specific validation
                if (request.Role == "guide")
                {
                    if (string.IsNullOrWhiteSpace(request.Experience) ||
                        string.IsNullOrWhiteSpace(request.Specialization))
                    {
                        return BadRequest(new { message = "Vui lòng điền đầy đủ thông tin hướng dẫn viên" });
                    }

                    if (request.Certificate == null)
                    {
                        return BadRequest(new { message = "Vui lòng tải lên chứng chỉ hướng dẫn viên" });
                    }

                    // Validate certificate file
                    if (request.Certificate.ContentType != "application/pdf")
                    {
                        return BadRequest(new { message = "Chứng chỉ phải là file PDF" });
                    }

                    if (request.Certificate.Length > 10 * 1024 * 1024) // 10MB
                    {
                        return BadRequest(new { message = "File chứng chỉ quá lớn (tối đa 10MB)" });
                    }
                }

                // Handle file upload for guides
                string certificatePath = null;
                if (request.Role == "guide" && request.Certificate != null)
                {
                    // Create uploads directory if it doesn't exist
                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "certificates");
                    Directory.CreateDirectory(uploadsDir);

                    // Generate unique filename
                    var fileName = $"{Guid.NewGuid()}_{request.Certificate.FileName}";
                    certificatePath = Path.Combine(uploadsDir, fileName);

                    // Save file
                    using (var stream = new FileStream(certificatePath, FileMode.Create))
                    {
                        await request.Certificate.CopyToAsync(stream);
                    }

                    // Store relative path for database
                    certificatePath = $"/uploads/certificates/{fileName}";
                }

                // Register user
                var result = await _authService.RegisterAsync(
                    request.Email, 
                    request.Password, 
                    request.FullName, 
                    request.Role ?? "traveler",
                    request.PhoneNumber,
                    request.Experience,
                    request.Specialization,
                    request.Languages,
                    request.Bio,
                    certificatePath
                );

                if (result == null)
                {
                    _logger.LogWarning("Registration failed for email: {Email}", request.Email);
                    return BadRequest(new { message = "Đăng ký thất bại. Email có thể đã được sử dụng." });
                }

                _logger.LogInformation("Registration successful for email: {Email}", request.Email);

                // Return with token for auto-login (except for guides who need approval)
                if (request.Role == "guide")
                {
                    return Ok(new
                    {
                        message = "Đăng ký thành công! Tài khoản hướng dẫn viên của bạn đang được xem xét.",
                        requiresApproval = true
                    });
                }
                else
                {
                    return Ok(new
                    {
                        accessToken = result.AccessToken,
                        refreshToken = result.RefreshToken,
                        user = new
                        {
                            id = result.User?.Id,
                            email = result.User?.Email,
                            role = request.Role ?? "traveler"
                        },
                        message = "Đăng ký thành công!"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during registration for email: {Email}", request.Email);
                // Trả message cụ thể từ Supabase/GoTrue thay vì generic 500
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Logout endpoint (optional - mainly client-side)
        /// POST /api/auth/logout
        /// </summary>
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Logout is mainly handled client-side by clearing localStorage
            // Server-side logout would involve invalidating the token (if using sessions)
            return Ok(new { message = "Đăng xuất thành công" });
        }
    }

    // Request DTOs
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Role { get; set; } = "traveler";
        
        // Guide-specific fields
        public string? Experience { get; set; }
        public string? Specialization { get; set; }
        public string? Languages { get; set; }
        public string? Bio { get; set; }
        public IFormFile? Certificate { get; set; }
    }
}
