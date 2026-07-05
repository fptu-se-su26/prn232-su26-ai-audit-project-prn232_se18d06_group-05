using System.Collections.Generic;

namespace TripMate_WebAPI.DTOs.Tour.Responses
{
    public class MyTourDashboardDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Duration { get; set; }
        public int MaxGuests { get; set; }
        public decimal Price { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public int Bookings { get; set; }
        public decimal Rating { get; set; }
        public bool IsActive { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
