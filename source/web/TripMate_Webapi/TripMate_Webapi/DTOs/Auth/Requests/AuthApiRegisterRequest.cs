using Microsoft.AspNetCore.Http;

namespace TripMate_WebAPI.DTOs.Auth;

public class AuthApiRegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Role { get; set; }
    
    // For guide
    public string? Experience { get; set; }
    public string? Specialization { get; set; }
    public IFormFile? Certificate { get; set; }
    public string? Languages { get; set; }
    public string? Bio { get; set; }
}
