using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;


namespace TripMate_Webapi.Controllers
{
    /// <summary>
    /// API Controller for Authentication (Login, Register)
    /// Routes: /api/auth/login, /api/auth/register
    /// </summary>
        public class AuthController : Controller
    {
        private readonly SupabaseAuthService _authService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IPasswordResetService _passwordResetService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(
            SupabaseAuthService authService,
            IGoogleAuthService googleAuthService,
            IPasswordResetService passwordResetService,
            ILogger<AuthController> logger, 
            IConfiguration configuration)
        {
            _authService = authService;
            _googleAuthService = googleAuthService;
            _passwordResetService = passwordResetService;
            _logger = logger;
            _configuration = configuration;
        }

        // 👱‍♀️ ponytail: merged MVC views
        [HttpGet("/Auth/Login")]
        public IActionResult LoginView() => View("Login");

        [HttpGet("/Auth/Register")]
        public IActionResult RegisterView() => View("Register");

        [HttpGet("/Auth/ResetPassword")]
        public IActionResult ResetPasswordView(string email = "", string token = "")
        {
            ViewBag.Email = email;
            ViewBag.Token = token;
            return View("ResetPassword");
        }

        [HttpGet("/Auth/Logout")]
        public IActionResult MvcLogout()
        {
            Response.Cookies.Delete("access_token");
            return RedirectToAction("LandingPage", "LandingPage");
        }


        /// <summary>
        /// Login endpoint
        /// POST /api/auth/login
        /// </summary>
        [HttpPost("/api/auth/login")]
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
                var userIsActive = result.User?.IsActive ?? true;
                
                // Cố định role cho các tài khoản seed để tránh lỗi sai role từ Database
                if (request.Email == "admin@tripmate.com") userRole = "admin";
                if (request.Email == "guide@tripmate.com") {
                    userRole = "guide";
                    userIsActive = true;
                }

                if (userRole == "guide" && !userIsActive)
                {
                    _logger.LogWarning("Pending or inactive guide login attempt for email: {Email}", request.Email);
                    return Unauthorized(new { message = "Tài khoản của bạn đang chờ Admin phê duyệt hoặc đã bị vô hiệu hóa. Vui lòng quay lại sau." });
                }

                // Set cookie for MVC views to read
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // Set to true in production
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                Response.Cookies.Append("access_token", result.AccessToken, cookieOptions);

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
        [HttpPost("/api/auth/register")]
        public async Task<IActionResult> Register([FromForm] AuthApiRegisterRequest request)
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
                string? certificatePath = null;
                if (request.Role == "guide" && request.Certificate != null)
                {
                    var cloudName = _configuration["Cloudinary:CloudName"];
                    var apiKey = _configuration["Cloudinary:ApiKey"];
                    var apiSecret = _configuration["Cloudinary:ApiSecret"];
                    
                    var account = new CloudinaryDotNet.Account(cloudName, apiKey, apiSecret);
                    var cloudinary = new CloudinaryDotNet.Cloudinary(account);
                    
                    using (var stream = request.Certificate.OpenReadStream())
                    {
                        // Upload PDF as a raw file
                        var uploadParams = new CloudinaryDotNet.Actions.RawUploadParams()
                        {
                            File = new CloudinaryDotNet.FileDescription(request.Certificate.FileName, stream),
                            Folder = "tripmate_certificates"
                        };
                        
                        var uploadResult = await cloudinary.UploadAsync(uploadParams);
                        
                        if (uploadResult.Error != null)
                        {
                            return BadRequest(new { message = $"Lỗi upload file: {uploadResult.Error.Message}" });
                        }
                        
                        certificatePath = uploadResult.SecureUrl.ToString();
                    }
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
        /// Google OAuth login endpoint
        /// POST /api/auth/google-login
        /// </summary>
        [HttpPost("/api/auth/google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                _logger.LogInformation("Google login attempt with token: {TokenStart}...", request.IdToken.Substring(0, 20));

                var result = await _googleAuthService.LoginWithGoogleAsync(request.IdToken, request.AccessToken);

                if (result == null)
                {
                    _logger.LogWarning("Google login failed - invalid token");
                    return Unauthorized(new { message = "Google token không hợp lệ" });
                }

                _logger.LogInformation("Google login successful for email: {Email}", result.User?.Email);

                // Set cookie for MVC views to read
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // Set to true in production
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                Response.Cookies.Append("access_token", result.AccessToken, cookieOptions);

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
                _logger.LogWarning(ex, "Google login error");
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Forgot password endpoint
        /// POST /api/auth/forgot-password
        /// </summary>
        [HttpPost("/api/auth/forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                _logger.LogInformation("Forgot password request for email: {Email}", request.Email);

                // Validate input
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { message = "Email là bắt buộc" });
                }


                var success = await _passwordResetService.SendPasswordResetEmailAsync(request.Email, request.CaptchaToken);

                if (success)
                {
                    return Ok(new ForgotPasswordResponse(
                        "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi link đặt lại mật khẩu.", 
                        true));
                }
                else
                {
                    return BadRequest(new { message = "Không thể gửi email đặt lại mật khẩu" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forgot password for email: {Email}", request.Email);
                return StatusCode(500, new { message = "Có lỗi xảy ra, vui lòng thử lại sau" });
            }
        }

        /// <summary>
        /// Reset password endpoint
        /// POST /api/auth/reset-password
        /// </summary>
        [HttpPost("/api/auth/reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                _logger.LogInformation("Reset password request with access token");

                // Validate input
                if (string.IsNullOrWhiteSpace(request.Email) || 
                    string.IsNullOrWhiteSpace(request.Token) || 
                    string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return BadRequest(new { message = "Thiếu thông tin bắt buộc" });
                }

                if (request.NewPassword.Length < 6)
                {
                    return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự" });
                }

                var success = await _passwordResetService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);

                if (success)
                {
                    return Ok(new { message = "Đặt lại mật khẩu thành công" });
                }
                else
                {
                    return BadRequest(new { message = "Token không hợp lệ hoặc đã hết hạn" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reset password");
                return StatusCode(500, new { message = "Có lỗi xảy ra, vui lòng thử lại sau" });
            }
        }

        /// <summary>
        /// Logout endpoint (optional - mainly client-side)
        /// POST /api/auth/logout
        /// </summary>
        [HttpPost("/api/auth/logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("access_token");
            return Ok(new { message = "Đăng xuất thành công" });
        }
    }
}
