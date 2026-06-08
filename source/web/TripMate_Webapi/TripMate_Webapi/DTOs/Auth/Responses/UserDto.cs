namespace TripMate_WebAPI.DTOs.Auth;

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
