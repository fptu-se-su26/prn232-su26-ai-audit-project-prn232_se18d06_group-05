using Microsoft.AspNetCore.Mvc;

namespace TripMate_Webapi.Controllers
{
    public class TravelerController : Controller
    {
        private readonly ILogger<TravelerController> _logger;

        public TravelerController(ILogger<TravelerController> logger)
        {
            _logger = logger;
        }

        // GET: /Traveler/Dashboard
        public IActionResult Dashboard()
        {
            return View();
        }

        // GET: /Traveler/Book
        // Placeholder for booking logic
        public IActionResult Book(string guideId, string date, int guests)
        {
            // Simulate saving booking and redirecting to dashboard
            TempData["SuccessMessage"] = "Your booking request has been sent to the Guide successfully!";
            return RedirectToAction("Dashboard");
        }

        // GET: /Traveler/BookingDetails/{id}
        public IActionResult BookingDetails(string id = "1")
        {
            return View();
        }

        // GET: /Traveler/Checkout/{id}
        public IActionResult Checkout(string id = "1")
        {
            return View();
        }

        // GET: /Traveler/Messages
        public IActionResult Messages()
        {
            return View();
        }

        // GET: /Traveler/SavedGuides
        public IActionResult SavedGuides()
        {
            return View();
        }

        // GET: /Traveler/Settings
        public IActionResult Settings()
        {
            return View();
        }

        // ponytail ultra: minimal inline update
        public class UpdateTravelerProfileDto
        {
            public string? FullName { get; set; }
            public string? Phone { get; set; }
            public string? Location { get; set; }
            public string? AvatarUrl { get; set; }
            public string? Email { get; set; }
        }

        [HttpPost("Traveler/UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateTravelerProfileDto dto, [FromServices] Supabase.Client supabase)
        {
            // Simplified auth check based on current project pattern
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var profileResponse = await supabase.From<Entities.ProfileEntity>().Where(x => x.Id == userId).Get();
            var profile = profileResponse.Models.FirstOrDefault();
            if (profile != null)
            {
                if (dto.FullName != null) profile.FullName = dto.FullName;
                if (dto.Phone != null) profile.Phone = dto.Phone;
                if (dto.Location != null) profile.Location = dto.Location;
                if (dto.AvatarUrl != null) profile.AvatarUrl = dto.AvatarUrl;
                if (dto.Email != null) profile.Email = dto.Email;
                await supabase.From<Entities.ProfileEntity>().Update(profile);
            }

            return Ok(new { success = true });
        }

        // GET: /Traveler/Review/{id}
        public IActionResult Review(string id = "1")
        {
            return View();
        }

        // POST: /Traveler/SubmitReview
        [HttpPost]
        public IActionResult SubmitReview(string id, int rating, string comment)
        {
            TempData["SuccessMessage"] = "Your review has been submitted successfully!";
            return RedirectToAction("Dashboard");
        }
    }
}
