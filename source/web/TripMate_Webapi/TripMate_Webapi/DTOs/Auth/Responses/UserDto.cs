namespace TripMate_WebAPI.DTOs.Auth;

public record UserDto(
    string Id,
    string Email,
    string? FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    string Role,
    bool IsActive,
    DateTime CreatedAt
);
