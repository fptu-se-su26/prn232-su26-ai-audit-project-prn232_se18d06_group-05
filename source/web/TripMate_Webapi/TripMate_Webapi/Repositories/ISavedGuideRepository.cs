using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories
{
    public interface ISavedGuideRepository
    {
        Task<SavedGuideEntity> SaveGuideAsync(SavedGuideEntity savedGuide);
        Task DeleteSavedGuideAsync(string travelerId, string guideProfileId);
        Task<List<SavedGuideEntity>> GetSavedGuidesByTravelerAsync(string travelerId);
        Task<bool> IsGuideSavedAsync(string travelerId, string guideProfileId);
    }
}
