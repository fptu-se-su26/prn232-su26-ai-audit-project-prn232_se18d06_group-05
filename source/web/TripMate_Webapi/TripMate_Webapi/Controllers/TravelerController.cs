using Microsoft.AspNetCore.Mvc;
using TripMate_Webapi.Repositories;
using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Controllers
{
    public class TravelerController : Controller
    {
        private readonly ILogger<TravelerController> _logger;
        private readonly ITripRequestRepository _tripRequestRepository;

        public TravelerController(ILogger<TravelerController> logger, ITripRequestRepository tripRequestRepository)
        {
            _logger = logger;
            _tripRequestRepository = tripRequestRepository;
        }

        // GET: /Traveler/Home
        public IActionResult Home()
        {
            return View();
        }

        // GET: /Traveler/Dashboard (Will eventually be deprecated/redirected to Trips)
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

        // GET: /Traveler/Saved
        public IActionResult Saved()
        {
            return View();
        }

        // GET: /Traveler/Settings
        public IActionResult Settings()
        {
            return View();
        }

        // GET: /Traveler/Review/{id}
        public IActionResult Review(string id = "1")
        {
            return View();
        }

        // GET: /Traveler/Trips
        public async Task<IActionResult> Trips()
        {
            var travelerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            List<TripRequestEntity> trips;
            if (!string.IsNullOrEmpty(travelerId))
            {
                trips = await _tripRequestRepository.GetTripRequestsByTravelerAsync(travelerId);
            }
            else
            {
                // Fallback for testing: Just show all trip requests if not logged in
                trips = await _tripRequestRepository.GetAllTripRequestsAsync();
            }

            return View(trips);
        }

        // GET: /Traveler/CreateTripRequest
        public IActionResult CreateTripRequest()
        {
            return View();
        }

        // POST: /Traveler/CreateTripRequest
        [HttpPost]
        public async Task<IActionResult> CreateTripRequest(
            [FromServices] Supabase.Client supabase, 
            string destination, string dates, string budget, string notes)
        {
            // Parse dates from "YYYY-MM-DD to YYYY-MM-DD"
            DateTime startDate = DateTime.UtcNow;
            DateTime endDate = DateTime.UtcNow.AddDays(1);
            if (!string.IsNullOrEmpty(dates) && dates.Contains(" to "))
            {
                var parts = dates.Split(" to ");
                if (DateTime.TryParse(parts[0], out var start)) startDate = start;
                if (DateTime.TryParse(parts[1], out var end)) endDate = end;
            }
            else if (DateTime.TryParse(dates, out var singleDate))
            {
                startDate = singleDate;
                endDate = singleDate;
            }

            var travelerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            // Fallback for testing: get any existing traveler profile from DB
            if (string.IsNullOrEmpty(travelerId))
            {
                var profiles = await supabase.From<ProfileEntity>().Select("id").Limit(1).Get();
                travelerId = profiles.Models.FirstOrDefault()?.Id ?? Guid.NewGuid().ToString();
            }

            var tripRequest = new TripRequestEntity
            {
                Id = Guid.NewGuid().ToString(),
                TravelerId = travelerId,
                Destination = destination,
                StartDate = startDate.ToUniversalTime(),
                EndDate = endDate.ToUniversalTime(),
                GroupSize = 1, // Add to form later
                Budget = budget ?? "",
                Notes = notes ?? "",
                Status = "open",
                CreatedAt = DateTime.UtcNow
            };

            await _tripRequestRepository.CreateTripRequestAsync(tripRequest);

            TempData["SuccessMessage"] = "Your trip request has been posted! Local guides will contact you soon.";
            return RedirectToAction("Trips");
        }

        // POST: /Traveler/DeleteTrip/{id}
        [HttpPost]
        public async Task<IActionResult> DeleteTrip(string id)
        {
            await _tripRequestRepository.DeleteTripRequestAsync(id);
            TempData["SuccessMessage"] = "Trip request deleted successfully.";
            return RedirectToAction("Trips");
        }

        // POST: /Traveler/ToggleTripStatus/{id}
        [HttpPost]
        public async Task<IActionResult> ToggleTripStatus(string id)
        {
            await _tripRequestRepository.ToggleTripRequestStatusAsync(id);
            TempData["SuccessMessage"] = "Trip status updated successfully.";
            return RedirectToAction("Trips");
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
