using TripMate_WebAPI.DTOs;

namespace TripMate_WebAPI.Services
{
    public interface IGuideDashboardService
    {
        Task<GuideDashboardViewModel> BuildDashboardAsync(string userId);
    }
}
