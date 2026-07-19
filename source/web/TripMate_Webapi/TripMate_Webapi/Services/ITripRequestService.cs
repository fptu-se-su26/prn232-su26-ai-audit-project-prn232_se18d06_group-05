using System.Collections.Generic;
using System.Threading.Tasks;
using TripMate_Webapi.DTOs.Guide;

namespace TripMate_Webapi.Services
{
    public interface ITripRequestService
    {
        Task<List<TripRequestDto>> GetOpenRequestsAsync(string guideProfileId);
        Task<List<GuideTripOfferDto>> GetGuideOffersAsync(string guideProfileId);
        Task<(bool Success, string? ErrorMessage, string? OfferId)> SendOfferAsync(string guideProfileId, SendTripOfferRequest request);
        Task<GuideOfferStatsDto> GetGuideOfferStatsAsync(string guideProfileId);
    }
}
