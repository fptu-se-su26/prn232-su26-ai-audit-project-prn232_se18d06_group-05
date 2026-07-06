using System.Collections.Generic;
using System.Threading.Tasks;
using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories
{
    public interface IExperiencePackageRepository
    {
        Task<ExperiencePackageEntity> CreatePackageAsync(ExperiencePackageEntity entity);
        Task<ExperiencePackageEntity> UpdatePackageAsync(ExperiencePackageEntity entity);
        Task<ExperiencePackageEntity?> GetPackageByIdAsync(string id, string guideId);
        Task<List<ExperiencePackageEntity>> GetPackagesByGuideIdAsync(string guideId);
        Task<bool> TogglePackageStatusAsync(string id, string guideId);
        Task<bool> DeletePackageAsync(string id, string guideId);
    }
}
