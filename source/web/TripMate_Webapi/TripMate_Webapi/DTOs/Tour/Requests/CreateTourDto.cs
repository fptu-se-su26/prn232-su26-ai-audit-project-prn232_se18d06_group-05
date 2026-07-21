using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TripMate_WebAPI.DTOs.Tour.Requests
{
    public class CreateTourDto
    {
        public string? Id { get; set; }

        [Required, StringLength(120, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(80)]
        public string City { get; set; } = string.Empty;

        [Required, StringLength(240)]
        public string MeetingPoint { get; set; } = string.Empty;

        [Required, StringLength(4000, MinimumLength = 20)]
        public string Description { get; set; } = string.Empty;

        [Range(0.5, 72)]
        public decimal DurationHours { get; set; }

        [Range(1, 1_000_000_000)]
        public decimal PricePerSession { get; set; }

        [Range(0, 1_000_000_000)]
        public decimal PricePerGuest { get; set; }

        [Range(1, 50)]
        public int IncludedGuestCount { get; set; } = 1;

        [Range(1, 50)]
        public int MaxGroupSize { get; set; }
        
        // Nhận JSON string từ Frontend để deserialize
        public string IncludedServices { get; set; } = "[]"; 
        public string Languages { get; set; } = "[]";
        public string Tags { get; set; } = "[]";
        public string TimelineJson { get; set; } = "[]";
        public string RetainedGalleryImages { get; set; } = "[]";
        
        // Uploaded files
        public IFormFile? CoverImage { get; set; }
        public List<IFormFile>? GalleryImages { get; set; }
    }
}
