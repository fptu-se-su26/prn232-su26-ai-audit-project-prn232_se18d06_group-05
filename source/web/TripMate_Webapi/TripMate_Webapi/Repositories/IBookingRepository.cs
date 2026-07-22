using System.Collections.Generic;
using System.Threading.Tasks;
using TripMate_Webapi.Entities;
using TripMate_Webapi.Repositories.Models;

namespace TripMate_Webapi.Repositories
{
    public interface IBookingRepository
    {
        Task<BookingEntity> CreateBookingAsync(BookingEntity booking, string? userToken = null);
        Task<List<BookingEntity>> GetBookingsByTravelerAsync(string travelerId);
        Task<BookingEntity?> GetBookingByIdAsync(string id);
        Task<BookingEntity> UpdateBookingAsync(BookingEntity booking);
        Task<int> GetPendingBookingsCountAsync(string guideProfileId);
        Task<string?> GetAnyTravelerProfileIdAsync();
        Task<List<BookingEntity>> GetGuideBookingsInRangeAsync(string guideProfileId, string start, string endExclusive);
        Task<List<CalendarBookingRecord>> GetGuideCalendarBookingsInRangeAsync(string guideProfileId, string start, string endExclusive);
        Task<List<BookingEntity>> GetBookingsForGuideAsync(string guideProfileId);
        Task UpdateBookingStatusAsync(string bookingId, int status);
        Task DeleteBookingAsync(string id);
    }
}
