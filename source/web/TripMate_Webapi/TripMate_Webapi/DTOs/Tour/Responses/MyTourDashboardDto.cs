using System.Collections.Generic;

namespace TripMate_WebAPI.DTOs.Tour.Responses
{
    public class MyTourDashboardDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Duration { get; set; }
        public int MaxGuests { get; set; }
        public int IncludedGuests { get; set; }
        public decimal Price { get; set; }
        public decimal AdditionalGuestFee { get; set; }
        public string City { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public bool IsActive { get; set; }
        public string PublicationStatus { get; set; } = "published";
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MeetingPoint { get; set; } = string.Empty;
        public List<string> Languages { get; set; } = [];
        public int BookingCount { get; set; }
        public int CompletedBookingCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal? AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int CompletenessScore { get; set; }
        public List<string> MissingQualityItems { get; set; } = [];
        public DateTime UpdatedAt { get; set; }
    }
}
