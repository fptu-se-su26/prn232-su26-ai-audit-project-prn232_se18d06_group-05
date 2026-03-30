namespace TripMate_WebAPI.Models;

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Email, string Password, string FullName);

public record RefreshTokenRequest(string RefreshToken);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    long ExpiresAt,
    UserDto User
);

public record UserDto(
    string Id,
    string Email,
    string? FullName,
    string? Phone,
    string? AvatarUrl,
    string Role,
    DateTime CreatedAt
);
