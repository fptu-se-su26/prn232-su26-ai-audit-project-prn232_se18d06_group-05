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
    string EndDate,
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
    string Status,
    string DurationType,
    int DurationDays,
    int? DurationMinutes,
    string DurationLabel,
    string TimeZone,
    bool ScheduleConfigured
);

public record SaveBlockedDatesResult(
    List<BlockedDateItem> BlockedDates,
    List<string> ConflictingDates
);
