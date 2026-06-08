namespace TripMate_WebAPI.DTOs.Auth;

public record RegisterRequest(string Email, string Password, string FullName);
