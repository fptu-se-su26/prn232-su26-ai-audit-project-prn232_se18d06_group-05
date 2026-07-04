using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories
{
    public interface ITripRequestRepository
    {
        Task<TripRequestEntity> CreateTripRequestAsync(TripRequestEntity tripRequest);
        Task<List<TripRequestEntity>> GetTripRequestsByTravelerAsync(string travelerId);
        Task<List<TripRequestEntity>> GetAllOpenTripRequestsAsync();
        Task<List<TripRequestEntity>> GetAllTripRequestsAsync();
        Task DeleteTripRequestAsync(string id);
        Task ToggleTripRequestStatusAsync(string id);
    }
}
