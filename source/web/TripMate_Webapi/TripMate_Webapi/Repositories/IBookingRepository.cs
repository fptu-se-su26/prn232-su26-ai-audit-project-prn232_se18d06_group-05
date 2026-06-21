using System.Collections.Generic;
using System.Threading.Tasks;
using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories
{
    public interface IBookingRepository
    {
        Task<BookingEntity> CreateBookingAsync(BookingEntity booking, string? userToken = null);
        Task<List<BookingEntity>> GetBookingsByTravelerAsync(string travelerId);
        Task<BookingEntity?> GetBookingByIdAsync(string id);
        Task<BookingEntity> UpdateBookingAsync(BookingEntity booking);
        Task<string?> GetAnyTravelerProfileIdAsync();
    }
}
