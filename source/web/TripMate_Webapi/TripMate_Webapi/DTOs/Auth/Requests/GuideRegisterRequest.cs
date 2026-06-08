namespace TripMate_WebAPI.DTOs.Auth;

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
