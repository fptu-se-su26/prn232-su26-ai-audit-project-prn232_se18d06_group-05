namespace TripMate_WebAPI.DTOs.Auth;

public record ResetPasswordRequest(string AccessToken, string RefreshToken, string NewPassword);
