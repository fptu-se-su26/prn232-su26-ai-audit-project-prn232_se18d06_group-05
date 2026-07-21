namespace TripMate_WebAPI.DTOs.Guide.Requests;

public record SaveBlockedDatesRequest(
    string RangeStart,
    string RangeEnd,
    List<BlockedDateChange>? AddedDates,
    List<string>? RemovedDates
);

public record BlockedDateChange(
    string Date,
    string? Reason
);
