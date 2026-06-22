namespace TripMate_WebAPI.Configuration;

public class SeedAccountConfig
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "traveler";
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Experience { get; set; }
    public string? Specialization { get; set; }
    public string? Languages { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }
    public string? CityArea { get; set; }
}
