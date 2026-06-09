namespace TripMate_WebAPI.DTOs.Auth;

public record ResetPasswordRequest(string Email, string Token, string NewPassword);
