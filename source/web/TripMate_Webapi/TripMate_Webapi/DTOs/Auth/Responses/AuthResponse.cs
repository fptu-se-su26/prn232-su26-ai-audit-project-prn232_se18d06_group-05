namespace TripMate_WebAPI.DTOs.Auth;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    long ExpiresAt,
    UserDto User
);
