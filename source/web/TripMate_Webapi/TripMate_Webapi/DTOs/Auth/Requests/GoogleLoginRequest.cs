namespace TripMate_WebAPI.DTOs.Auth;

public record GoogleLoginRequest(string IdToken, string? AccessToken);
