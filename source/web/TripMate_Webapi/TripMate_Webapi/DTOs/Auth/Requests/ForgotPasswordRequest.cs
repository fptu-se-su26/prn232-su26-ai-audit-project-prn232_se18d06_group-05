namespace TripMate_WebAPI.DTOs.Auth;

public record ForgotPasswordRequest(string Email, string CaptchaToken);
