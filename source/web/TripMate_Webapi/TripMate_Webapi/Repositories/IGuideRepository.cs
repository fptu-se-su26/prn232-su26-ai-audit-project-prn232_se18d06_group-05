using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories
{
    public interface IGuideRepository
    {
        Task<List<GuideProfileEntity>> GetAllGuidesAsync();
        Task<List<GuideProfileEntity>> GetGuidesByDestinationAsync(string destination);
        Task<List<GuideProfileEntity>> GetGuidesFilteredAsync(string? destination, string? specialty);
        Task<GuideProfileEntity> GetGuideByIdAsync(string id);
        Task<GuideProfileEntity> GetGuideByProfileIdAsync(string profileId);
    }
}
