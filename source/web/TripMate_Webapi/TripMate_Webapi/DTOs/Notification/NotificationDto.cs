namespace TripMate_WebAPI.DTOs.Notification;

public class NotificationDto
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }
    public string? GuideId { get; set; }
    public string? GuideName { get; set; }
    public string? GuideEmail { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
