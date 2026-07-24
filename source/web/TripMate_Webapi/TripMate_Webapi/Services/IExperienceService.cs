using System.Collections.Generic;
using System.Threading.Tasks;
using TripMate_WebAPI.DTOs.Tour.Requests;
using TripMate_WebAPI.DTOs.Tour.Responses;
using TripMate_Webapi.Entities;

namespace TripMate_WebAPI.Services
{
    public enum TourRemovalOutcome
    {
        Deleted,
        Archived
    }

    public interface IExperienceService
    {
        Task<ExperiencePackageEntity> CreateTourAsync(CreateTourDto dto, string guideProfileId);
        Task<ExperiencePackageEntity> SaveTourDraftAsync(SaveTourDraftDto dto, string guideProfileId);
        Task<ExperiencePackageEntity?> GetPackageByIdAsync(string id, string guideProfileId);
        Task<List<MyTourDashboardDto>> GetMyToursAsync(string guideProfileId);
        Task<bool> ToggleTourStatusAsync(string tourId, string guideProfileId);
        Task<TourRemovalOutcome> DeleteTourAsync(string tourId, string guideProfileId);
        Task<ExperiencePackageEntity?> DuplicateTourAsync(string tourId, string guideProfileId);
    }
}
