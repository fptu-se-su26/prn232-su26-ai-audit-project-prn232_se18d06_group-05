using Supabase;
using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories
{
    public class TripRequestRepository : ITripRequestRepository
    {
        private readonly Client _supabase;

        public TripRequestRepository(Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<TripRequestEntity> CreateTripRequestAsync(TripRequestEntity tripRequest)
        {
            var response = await _supabase.From<TripRequestEntity>().Insert(tripRequest);
            return response.Models.FirstOrDefault();
        }

        public async Task<List<TripRequestEntity>> GetTripRequestsByTravelerAsync(string travelerId)
        {
            var response = await _supabase.From<TripRequestEntity>()
                .Where(x => x.TravelerId == travelerId)
                .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Get();
            
            return response.Models;
        }

        public async Task<List<TripRequestEntity>> GetAllOpenTripRequestsAsync()
        {
            var response = await _supabase.From<TripRequestEntity>()
                .Where(x => x.Status == "open")
                .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Get();
                
            return response.Models;
        }

        public async Task<List<TripRequestEntity>> GetAllTripRequestsAsync()
        {
            var response = await _supabase.From<TripRequestEntity>()
                .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Get();
                
            return response.Models;
        }

        public async Task DeleteTripRequestAsync(string id)
        {
            await _supabase.From<TripRequestEntity>()
                .Where(x => x.Id == id)
                .Delete();
        }

        public async Task ToggleTripRequestStatusAsync(string id)
        {
            var request = await _supabase.From<TripRequestEntity>()
                .Where(x => x.Id == id)
                .Single();

            if (request != null)
            {
                request.Status = request.Status == "open" ? "closed" : "open";
                await _supabase.From<TripRequestEntity>().Update(request);
            }
        }
    }
}
