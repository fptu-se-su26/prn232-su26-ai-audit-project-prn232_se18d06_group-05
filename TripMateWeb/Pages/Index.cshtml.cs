using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TripMateWeb.Data;
using TripMateWeb.Models;

namespace TripMateWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly TripMateDbContext _context;

        public IndexModel(TripMateDbContext context)
        {
            _context = context;
        }

        public int TotalGuides { get; set; }
        public int TotalTours { get; set; }
        public int TotalBookings { get; set; }
        public double AverageRating { get; set; }
        public List<GuideTour> FeaturedTours { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Get statistics
            TotalGuides = await _context.Profiles.CountAsync(p => p.Role == "guide");
            TotalTours = await _context.GuideTours.CountAsync(t => t.Status == "active");
            TotalBookings = await _context.Bookings.CountAsync(b => b.Status == "completed");
            
            var avgRating = await _context.GuideTours
                .Where(t => t.TotalReviews > 0)
                .AverageAsync(t => (double?)t.Rating);
            AverageRating = avgRating ?? 0;

            // Get featured tours (top rated, active tours)
            FeaturedTours = await _context.GuideTours
                .Include(t => t.TourTemplate)
                .Include(t => t.Guide)
                .Where(t => t.Status == "active")
                .OrderByDescending(t => t.Rating)
                .ThenByDescending(t => t.TotalReviews)
                .Take(6)
                .ToListAsync();
        }
    }
}