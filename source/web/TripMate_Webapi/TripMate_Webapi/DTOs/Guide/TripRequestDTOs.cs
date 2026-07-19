using System;

namespace TripMate_Webapi.DTOs.Guide
{
    public class TripRequestDto
    {
        public string Id { get; set; } = string.Empty;
        public string TravelerName { get; set; } = string.Empty;
        public string TravelerAvatar { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int GroupSize { get; set; }
        public string Budget { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime PostedAt { get; set; }
        public bool HasSentOffer { get; set; }
    }

    public class SendTripOfferRequest
    {
        public string TripRequestId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public decimal ProposedPrice { get; set; }
    }

    public class GuideOfferStatsDto
    {
        public int OffersSentThisWeek { get; set; }
        public int AcceptedOffers { get; set; }
        public double SuccessRate { get; set; }
    }

    public class GuideTripOfferDto
    {
        public string Id { get; set; } = string.Empty;
        public string TripRequestId { get; set; } = string.Empty;
        public string TravelerName { get; set; } = string.Empty;
        public string TravelerAvatar { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int GroupSize { get; set; }
        public string Budget { get; set; } = string.Empty;
        public string RequestNotes { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public decimal ProposedPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}
