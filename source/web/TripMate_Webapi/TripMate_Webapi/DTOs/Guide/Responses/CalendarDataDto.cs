namespace TripMate_WebAPI.DTOs.Guide.Responses;

public record CalendarDataDto(
    List<BlockedDateItem> BlockedDates,
    List<CalendarBookingItem> Bookings
);

public record BlockedDateItem(
    string Id,
    string Date  // "yyyy-MM-dd"
);

public record CalendarBookingItem(
    string BookingId,
    string BookingDate,
    string GuestName,
    int GuestCount,
    decimal GuideEarnings,
    string PackageTitle,
    string Status
);
