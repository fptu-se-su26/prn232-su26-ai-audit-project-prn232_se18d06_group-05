using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories
{
    public interface ITripRequestRepository
    {
        Task<TripRequestEntity> CreateTripRequestAsync(TripRequestEntity tripRequest);
        Task<List<TripRequestEntity>> GetTripRequestsByTravelerAsync(string travelerId);
        Task<List<TripRequestEntity>> GetAllOpenTripRequestsAsync();
        Task<List<TripRequestEntity>> GetAllTripRequestsAsync();
        Task<List<TripRequestEntity>> GetUpcomingOpenTripRequestsAsync();
        Task<List<TripRequestEntity>> GetTripRequestsByIdsAsync(IReadOnlyCollection<string> ids);
        Task<TripRequestEntity?> GetTripRequestByIdAsync(string id);
        Task DeleteTripRequestAsync(string id);
        Task ToggleTripRequestStatusAsync(string id);

        // Offers
        Task<TripOfferEntity> CreateTripOfferAsync(TripOfferEntity offer);
        Task<List<TripOfferEntity>> GetTripOffersByGuideAsync(string guideProfileId);

        // Profiles
        Task<List<ProfileEntity>> GetProfilesByIdsAsync(IReadOnlyCollection<string> ids);
    }
}
