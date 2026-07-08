using TripMate_WebAPI.DTOs.Guide.Requests;
using TripMate_WebAPI.DTOs.Guide.Responses;

namespace TripMate_WebAPI.Services
{
    public interface ICalendarService
    {
        Task<CalendarDataDto> GetCalendarDataAsync(string guideProfileId, string start, string end);
        Task SaveBlockedDatesAsync(string guideProfileId, SaveBlockedDatesRequest req);
    }
}
