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

                return Ok(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    user = new
                    {
                        id = result.User?.Id,
                        email = result.User?.Email,
                        role = result.User?.Role ?? "traveler"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                return StatusCode(500, new { message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Register endpoint
        /// POST /api/auth/register
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Register attempt for email: {Email}", request.Email);

                // Validate request
                if (string.IsNullOrWhiteSpace(request.Email) || 
                    string.IsNullOrWhiteSpace(request.Password) ||
                    string.IsNullOrWhiteSpace(request.FullName))
                {
                    return BadRequest(new { message = "Vui lòng điền đầy đủ thông tin" });
                }

                if (request.Password.Length < 6)
                {
                    return BadRequest(new { message = "Mật khẩu phải có ít nhất 6 ký tự" });
                }

                // Register user
                var result = await _authService.RegisterAsync(
                    request.Email, 
                    request.Password, 
                    request.FullName, 
                    request.Role ?? "traveler"
                );

                if (result == null)
                {
                    _logger.LogWarning("Registration failed for email: {Email}", request.Email);
                    return BadRequest(new { message = "Đăng ký thất bại. Email có thể đã được sử dụng." });
                }

                _logger.LogInformation("Registration successful for email: {Email}", request.Email);

                // Return with token for auto-login
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
                return StatusCode(500, new { message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
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
        public string? Role { get; set; } = "traveler";
    }
}
