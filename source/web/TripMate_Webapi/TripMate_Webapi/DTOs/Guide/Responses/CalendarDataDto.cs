namespace TripMate_WebAPI.DTOs.Guide.Responses;

public record CalendarDataDto(
    List<BlockedDateItem> BlockedDates,
    List<CalendarBookingItem> Bookings
);

public record BlockedDateItem(
    string Id,
    string Date,
    string? Reason
);

public record CalendarBookingItem(
    string BookingId,
    string BookingDate,
    string StartTime,
    string EndTime,
    string TravelerId,
    string GuestName,
    string TravelerAvatarUrl,
    int GuestCount,
    decimal GuideEarnings,
    string PackageId,
    string PackageTitle,
    string CoverImageUrl,
    string MeetingPoint,
    string? TravelerNotes,
    string Status
);

public record SaveBlockedDatesResult(
    List<BlockedDateItem> BlockedDates,
    List<string> ConflictingDates
);
