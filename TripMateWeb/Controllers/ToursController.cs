using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TripMateWeb.Data;
using TripMateWeb.Models;

namespace TripMateWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToursController : ControllerBase
    {
        private readonly TripMateDbContext _context;

        public ToursController(TripMateDbContext context)
        {
            _context = context;
        }

        // GET: api/tours
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTours(
            [FromQuery] string? search,
            [FromQuery] string? location,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? duration)
        {
            var query = _context.GuideTours
                .Include(t => t.TourTemplate)
                .Include(t => t.Guide)
                .Where(t => t.Status == "active");

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => 
                    t.TourTemplate.Title.Contains(search) ||
                    t.TourTemplate.Location.Contains(search));
            }

            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(t => t.TourTemplate.Location == location);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(t => t.Price <= maxPrice.Value);
            }

            if (!string.IsNullOrEmpty(duration))
            {
                switch (duration)
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

            var tours = await query
                .OrderByDescending(t => t.Rating)
                .Select(t => new
                {
                    t.Id,
                    t.TourTemplateId,
                    t.GuideId,
                    t.Price,
                    t.DurationHours,
                    t.MaxParticipants,
                    t.Status,
                    t.Rating,
                    t.TotalReviews,
                    Title = t.TourTemplate.Title,
                    Description = t.TourTemplate.Description,
                    Location = t.TourTemplate.Location,
                    Images = t.TourTemplate.Images,
                    GuideName = t.Guide.FullName,
                    CreatedAt = t.TourTemplate.CreatedAt
                })
                .ToListAsync();

            return Ok(new { tours });
        }