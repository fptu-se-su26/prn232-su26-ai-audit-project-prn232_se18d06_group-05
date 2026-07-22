using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TripMate_Webapi.DTOs.Guide;
using TripMate_Webapi.Entities;
using TripMate_Webapi.Repositories;

namespace TripMate_Webapi.Services
{
    public class TripRequestService : ITripRequestService
    {
        private readonly ITripRequestRepository _tripRequestRepository;
        private readonly ILogger<TripRequestService> _logger;

        public TripRequestService(ITripRequestRepository tripRequestRepository, ILogger<TripRequestService> logger)
        {
            _tripRequestRepository = tripRequestRepository;
            _logger = logger;
        }

        public async Task<List<TripRequestDto>> GetOpenRequestsAsync(string guideProfileId)
        {
            try
            {
                var requests = await _tripRequestRepository.GetUpcomingOpenTripRequestsAsync();
                var guideOffers = await _tripRequestRepository.GetTripOffersByGuideAsync(guideProfileId);
                var offeredRequestIds = new HashSet<string>(guideOffers.Select(o => o.TripRequestId));

                var travelerIds = requests
                    .Select(request => request.TravelerId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var travelers = await _tripRequestRepository.GetProfilesByIdsAsync(travelerIds);
                var travelersById = travelers
                    .Where(traveler => !string.IsNullOrWhiteSpace(traveler.Id))
                    .GroupBy(traveler => traveler.Id, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

                return requests
                    .OrderByDescending(request => request.CreatedAt)
                    .Select(request =>
                    {
                        travelersById.TryGetValue(request.TravelerId, out var traveler);
                        return new TripRequestDto
                        {
                            Id = request.Id,
                            TravelerName = traveler?.FullName ?? "Traveler",
                            TravelerAvatar = traveler?.AvatarUrl ?? "/images/AVATAR.png",
                            Destination = request.Destination,
                            StartDate = request.StartDate,
                            EndDate = request.EndDate,
                            GroupSize = request.GroupSize,
                            Budget = request.Budget,
                            Notes = request.Notes,
                            PostedAt = request.CreatedAt,
                            HasSentOffer = offeredRequestIds.Contains(request.Id)
                        };
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting open trip requests");
                return new List<TripRequestDto> {
                    new TripRequestDto {
                        Id = "error-123",
                        TravelerName = "Error",
                        Destination = "Error Details",
                        Notes = ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : "")
                    }
                };
            }
        }

        public async Task<(bool Success, string? ErrorMessage, string? OfferId)> SendOfferAsync(string guideProfileId, SendTripOfferRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TripRequestId))
                {
                    return (false, "Please select a valid trip request.", null);
                }

                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return (false, "Please enter a message for your offer.", null);
                }

                if (request.ProposedPrice <= 0)
                {
                    return (false, "The proposed price must be greater than 0.", null);
                }

                var tripRequest = await _tripRequestRepository.GetTripRequestByIdAsync(request.TripRequestId);
                if (tripRequest == null)
                {
                    return (false, "Trip request not found.", null);
                }

                if (!string.Equals(tripRequest.Status, "open", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "This trip request is no longer open for offers.", null);
                }

                if (tripRequest.StartDate < DateTime.UtcNow.Date)
                {
                    return (false, "This trip request is no longer upcoming.", null);
                }

                var guideOffers = await _tripRequestRepository.GetTripOffersByGuideAsync(guideProfileId);
                var hasExistingOffer = guideOffers.Any(o => string.Equals(o.TripRequestId, request.TripRequestId, StringComparison.OrdinalIgnoreCase));
                if (hasExistingOffer)
                {
                    return (false, "You have already sent an offer for this request.", null);
                }

                var newOffer = new TripOfferEntity
                {
                    TripRequestId = request.TripRequestId,
                    GuideProfileId = guideProfileId,
                    Message = request.Message.Trim(),
                    ProposedPrice = request.ProposedPrice,
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow
                };

                var createdOffer = await _tripRequestRepository.CreateTripOfferAsync(newOffer);
                return (true, null, createdOffer.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending trip offer");
                return (false, "Unable to send the offer right now. Please try again later.", null);
            }
        }

        public async Task<List<GuideTripOfferDto>> GetGuideOffersAsync(string guideProfileId)
        {
            try
            {
                var offers = await _tripRequestRepository.GetTripOffersByGuideAsync(guideProfileId);
                if (offers.Count == 0)
                {
                    return new List<GuideTripOfferDto>();
                }

                var requestIds = offers
                    .Select(x => x.TripRequestId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var requests = await _tripRequestRepository.GetTripRequestsByIdsAsync(requestIds);
                var requestsById = requests
                    .Where(request => !string.IsNullOrWhiteSpace(request.Id))
                    .GroupBy(request => request.Id, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

                var travelerIds = requestsById.Values
                    .Select(x => x.TravelerId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var travelers = await _tripRequestRepository.GetProfilesByIdsAsync(travelerIds);
                var travelersById = travelers
                    .Where(traveler => !string.IsNullOrWhiteSpace(traveler.Id))
                    .GroupBy(traveler => traveler.Id, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

                return offers
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(offer =>
                    {
                        requestsById.TryGetValue(offer.TripRequestId, out var request);
                        ProfileEntity? traveler = null;
                        if (request != null && !string.IsNullOrWhiteSpace(request.TravelerId))
                        {
                            travelersById.TryGetValue(request.TravelerId, out traveler);
                        }

                        return new GuideTripOfferDto
                        {
                            Id = offer.Id,
                            TripRequestId = offer.TripRequestId,
                            TravelerName = traveler?.FullName ?? "Traveler",
                            TravelerAvatar = traveler?.AvatarUrl ?? "/images/AVATAR.png",
                            Destination = request?.Destination ?? "Request unavailable",
                            StartDate = request?.StartDate ?? DateTime.MinValue,
                            EndDate = request?.EndDate ?? DateTime.MinValue,
                            GroupSize = request?.GroupSize ?? 0,
                            Budget = request?.Budget ?? string.Empty,
                            RequestNotes = request?.Notes ?? string.Empty,
                            RequestStatus = request?.Status ?? "unavailable",
                            Message = offer.Message,
                            ProposedPrice = offer.ProposedPrice,
                            Status = GetEffectiveOfferStatus(offer, request),
                            SentAt = offer.CreatedAt
                        };
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting guide offers");
                return new List<GuideTripOfferDto>();
            }
        }

        public async Task<GuideOfferStatsDto> GetGuideOfferStatsAsync(string guideProfileId)
        {
            try
            {
                var allOffers = await _tripRequestRepository.GetTripOffersByGuideAsync(guideProfileId);
                
                // Offers sent this week
                var startOfWeek = DateTime.UtcNow.AddDays(-7);
                var offersThisWeek = allOffers.Count(x => x.CreatedAt >= startOfWeek);
                
                var acceptedOffers = allOffers.Count(x => x.Status == "accepted");
                var successRate = allOffers.Count > 0 ? Math.Round((double)acceptedOffers / allOffers.Count * 100, 1) : 0;

                return new GuideOfferStatsDto
                {
                    OffersSentThisWeek = offersThisWeek,
                    AcceptedOffers = acceptedOffers,
                    SuccessRate = successRate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting guide offer stats");
                return new GuideOfferStatsDto();
            }
        }

        private static string GetEffectiveOfferStatus(TripOfferEntity offer, TripRequestEntity? request)
        {
            var status = string.IsNullOrWhiteSpace(offer.Status)
                ? "pending"
                : offer.Status.Trim().ToLowerInvariant();

            if (status != "pending")
            {
                return status;
            }

            var requestUnavailable = request == null ||
                                     !string.Equals(request.Status, "open", StringComparison.OrdinalIgnoreCase) ||
                                     request.StartDate.Date < DateTime.UtcNow.Date;

            return requestUnavailable ? "expired" : "pending";
        }
    }
}
