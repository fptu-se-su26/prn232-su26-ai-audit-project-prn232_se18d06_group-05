using System.Collections.Generic;

namespace TripMate_WebAPI.DTOs.Tour.Requests
{
    public class SaveTourDraftDto
    {
        public string? Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string MeetingPoint { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DurationHours { get; set; } = 4;
        public decimal PricePerSession { get; set; }
        public decimal PricePerGuest { get; set; }
        public int IncludedGuestCount { get; set; } = 1;
        public int MaxGroupSize { get; set; } = 1;
        public List<string> IncludedServices { get; set; } = [];
        public List<string> Languages { get; set; } = [];
        public List<string> Tags { get; set; } = [];
        public List<Dictionary<string, string>> Timeline { get; set; } = [];
    }
}
