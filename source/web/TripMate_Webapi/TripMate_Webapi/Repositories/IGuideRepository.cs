using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories
{
    public interface IGuideRepository
    {
        Task<List<GuideProfileEntity>> GetAllGuidesAsync();
        Task<List<GuideProfileEntity>> GetGuidesByDestinationAsync(string destination);
        Task<List<GuideProfileEntity>> GetGuidesFilteredAsync(string? destination, string? specialty);
        Task<GuideProfileEntity> GetGuideByIdAsync(string id);
        Task<List<GuideAvailabilityEntity>> GetBlockedDatesInRangeAsync(string guideProfileId, string start, string end);
        Task DeleteBlockedDatesInRangeAsync(string guideProfileId, string start, string end);
        Task InsertBlockedDatesAsync(List<GuideAvailabilityEntity> entities);
        Task<GuideProfileEntity> GetGuideByProfileIdAsync(string profileId);
        Task UpdateGuideViewsAsync(string guideProfileId, int delta);
        Task<GuideProfileEntity?> GetGuideProfileByUserIdAsync(string userId);
    }
}
