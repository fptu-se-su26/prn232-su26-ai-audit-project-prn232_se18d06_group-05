namespace TripMate_WebAPI.Models;

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Email, string Password, string FullName);

public record GuideRegisterRequest(
    string Email, 
    string Password, 
    string FullName, 
    string? Phone, 
    string? Experience, 
    string? Specialization, 
    string? Languages, 
    string? Bio,
    string? CertificateUrl
);

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
    string? Status,
    DateTime CreatedAt
);
