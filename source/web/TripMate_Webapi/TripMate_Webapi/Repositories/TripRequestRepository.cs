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
            return response.Models.FirstOrDefault()
                ?? throw new InvalidOperationException("Supabase did not return the created trip request.");
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

        public async Task<List<TripRequestEntity>> GetUpcomingOpenTripRequestsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var response = await _supabase.From<TripRequestEntity>()
                .Where(x => x.Status == "open" && x.StartDate >= today)
                .Order(x => x.StartDate, Postgrest.Constants.Ordering.Ascending)
                .Get();

            return response.Models;
        }

        public async Task<TripRequestEntity?> GetTripRequestByIdAsync(string id)
        {
            var response = await _supabase.From<TripRequestEntity>()
                .Where(x => x.Id == id)
                .Single();

            return response;
        }

        public async Task<List<TripRequestEntity>> GetTripRequestsByIdsAsync(IReadOnlyCollection<string> ids)
        {
            var normalizedIds = NormalizeIds(ids);
            if (normalizedIds.Count == 0)
            {
                return new List<TripRequestEntity>();
            }

            var response = await _supabase.From<TripRequestEntity>()
                .Filter("id", Postgrest.Constants.Operator.In, normalizedIds.Cast<object>().ToList())
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

        public async Task<TripOfferEntity> CreateTripOfferAsync(TripOfferEntity offer)
        {
            var response = await _supabase.From<TripOfferEntity>().Insert(offer);
            var createdOffer = response.Models.FirstOrDefault();
            if (createdOffer == null || string.IsNullOrWhiteSpace(createdOffer.Id))
            {
                throw new InvalidOperationException("Supabase did not return a persisted trip offer ID.");
            }

            return createdOffer;
        }

        public async Task<List<TripOfferEntity>> GetTripOffersByGuideAsync(string guideProfileId)
        {
            var response = await _supabase.From<TripOfferEntity>()
                .Where(x => x.GuideProfileId == guideProfileId)
                .Get();

            return response.Models;
        }

        public async Task<List<ProfileEntity>> GetProfilesByIdsAsync(IReadOnlyCollection<string> ids)
        {
            var normalizedIds = NormalizeIds(ids);
            if (normalizedIds.Count == 0)
            {
                return new List<ProfileEntity>();
            }

            var response = await _supabase.From<ProfileEntity>()
                .Filter("id", Postgrest.Constants.Operator.In, normalizedIds.Cast<object>().ToList())
                .Get();

            return response.Models;
        }

        private static List<string> NormalizeIds(IEnumerable<string> ids)
        {
            return ids
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
