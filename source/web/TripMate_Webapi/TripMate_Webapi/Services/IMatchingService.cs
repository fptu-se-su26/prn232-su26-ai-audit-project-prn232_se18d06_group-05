using TripMate_WebAPI.DTOs.Matching;

namespace TripMate_WebAPI.Services;

public interface IMatchingService
{
    Task<MatchingResponse> FindMatchesAsync(
        MatchingRequest request,
        CancellationToken cancellationToken = default);
}
