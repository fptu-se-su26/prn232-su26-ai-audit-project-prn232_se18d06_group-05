using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TripMateWeb.Data;
using TripMateWeb.Models;

namespace TripMateWeb.Pages.Tours
{
    public class IndexModel : PageModel
    {
        private readonly TripMateDbContext _context;

        public IndexModel(TripMateDbContext context)
        {
            _context = context;
        }

        public List<GuideTour> Tours { get; set; } = new();
        public List<string> Locations { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedLocation { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MaxPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? DurationFilter { get; set; }

        public async Task OnGetAsync()
        {
            // Get all locations for filter
            Locations = await _context.TourTemplates
                .Select(t => t.Location)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();

            // Build query
            var query = _context.GuideTours
                .Include(t => t.TourTemplate)
                .Include(t => t.Guide)
                .Where(t => t.Status == "active");

            // Apply search filter
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(t => 
                    t.TourTemplate.Title.Contains(SearchTerm) ||
                    t.TourTemplate.Location.Contains(SearchTerm) ||
                    (t.TourTemplate.Description != null && t.TourTemplate.Description.Contains(SearchTerm)));
            }

            // Apply location filter
            if (!string.IsNullOrEmpty(SelectedLocation))
            {
                query = query.Where(t => t.TourTemplate.Location == SelectedLocation);
            }

            // Apply price filter
            if (MaxPrice.HasValue)
            {
                query = query.Where(t => t.Price <= MaxPrice.Value);
            }

            // Apply duration filter
            if (!string.IsNullOrEmpty(DurationFilter))
            {
                switch (DurationFilter)
                {
                    case "1-4":
                        query = query.Where(t => t.DurationHours >= 1 && t.DurationHours <= 4);
                        break;
                    case "5-8":
                        query = query.Where(t => t.DurationHours >= 5 && t.DurationHours <= 8);
                        break;
                    case "9+":
                        query = query.Where(t => t.DurationHours >= 9);
                        break;
                }
            }

            // Execute query with ordering
            Tours = await query
                .OrderByDescending(t => t.Rating)
                .ThenByDescending(t => t.TotalReviews)
                .ThenBy(t => t.Price)
                .ToListAsync();
        }
    }
}