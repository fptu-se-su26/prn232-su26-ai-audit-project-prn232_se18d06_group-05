using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace TripMate_WebAPI.DTOs.Tour.Requests
{
    public class CreateTourDto
    {
        public string? Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string MeetingPoint { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DurationHours { get; set; }
        public decimal PricePerSession { get; set; }
        public decimal PricePerGuest { get; set; }
        public int MaxGroupSize { get; set; }
        
        // Nhận JSON string từ Frontend để deserialize
        public string IncludedServices { get; set; } = "[]"; 
        public string Languages { get; set; } = "[]";
        public string Tags { get; set; } = "[]";
        public string TimelineJson { get; set; } = "[]";
        
        // Uploaded files
        public IFormFile? CoverImage { get; set; }
        public List<IFormFile>? GalleryImages { get; set; }
    }
}
