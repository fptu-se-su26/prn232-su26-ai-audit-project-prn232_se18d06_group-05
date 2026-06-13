using Supabase;
using System.Collections.Generic;
using System.Threading.Tasks;
using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly Client _supabase;

        public BookingRepository(Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<BookingEntity> CreateBookingAsync(BookingEntity booking)
        {
            var response = await _supabase.From<BookingEntity>().Insert(booking);
            return response.Models.FirstOrDefault() ?? booking;
        }

        public async Task<List<BookingEntity>> GetBookingsByTravelerAsync(string travelerId)
        {
            var response = await _supabase.From<BookingEntity>()
                .Select("*, guide_profiles(*, profiles(*)), experience_packages(*)")
                .Where(b => b.TravelerId == travelerId)
                .Get();
                
            return response.Models;
        }

        public async Task<BookingEntity?> GetBookingByIdAsync(string id)
        {
            var response = await _supabase.From<BookingEntity>()
                .Where(b => b.Id == id)
                .Get();
                
            return response.Models.FirstOrDefault();
        }

        public async Task<BookingEntity> UpdateBookingAsync(BookingEntity booking)
        {
            var response = await _supabase.From<BookingEntity>().Update(booking);
            return response.Models.FirstOrDefault() ?? booking;
        }
    }
}
