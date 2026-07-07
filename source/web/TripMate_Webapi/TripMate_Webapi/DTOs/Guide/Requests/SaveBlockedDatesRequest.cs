namespace TripMate_WebAPI.DTOs.Guide.Requests;

public record SaveBlockedDatesRequest(
    string RangeStart,          // "yyyy-MM-dd"
    string RangeEnd,            // "yyyy-MM-dd"
    List<string> BlockedDates   // ["yyyy-MM-dd", ...]
);
