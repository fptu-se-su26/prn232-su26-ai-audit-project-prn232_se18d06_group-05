using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Models;
using TripMate_WebAPI.Services;

namespace TripMate_WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SupabaseAuthService _authService;

    public AuthController(SupabaseAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Đăng nhập bằng email + password
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email và mật khẩu không được để trống" });

        try
        {
            var result = await _authService.LoginAsync(request.Email, request.Password);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Đăng ký tài khoản mới
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { message = "Vui lòng điền đầy đủ thông tin" });

        if (request.Password.Length < 6)
            return BadRequest(new { message = "Mật khẩu phải có ít nhất 6 ký tự" });

        try
        {
            var result = await _authService.RegisterAsync(
                request.Email, request.Password, request.FullName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Làm mới access token bằng refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { message = "Refresh token không hợp lệ" });

        try
        {
            var result = await _authService.RefreshAsync(request.RefreshToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy thông tin user hiện tại (cần JWT)
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        // Claims được inject bởi JWT middleware
        var userId = User.FindFirst("sub")?.Value;
        var email = User.FindFirst("email")?.Value;
        var role = User.FindFirst("user_role")?.Value
                ?? User.FindFirst("role")?.Value
                ?? "traveler";

        return Ok(new
        {
            id = userId,
            email,
            role
        });
    }
}
