namespace TripMate_WebAPI.DTOs.Chat;

public class ActiveBookingDto
{
    public string BookingId { get; set; } = string.Empty;
    public int Status { get; set; }

    // Guide-side fields
    public string? GuideProfileId { get; set; }
    public string? GuideUserId { get; set; }
    public string? GuideName { get; set; }
    public string? GuideAvatar { get; set; }

    // Traveler-side fields
    public string? TravelerId { get; set; }
    public string? TravelerName { get; set; }
    public string? TravelerAvatar { get; set; }

    public string? TourName { get; set; }
    public string? BookingDate { get; set; }
}
