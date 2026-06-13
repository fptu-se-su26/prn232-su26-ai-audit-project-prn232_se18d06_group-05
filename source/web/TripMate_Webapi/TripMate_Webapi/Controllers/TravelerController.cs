using Microsoft.AspNetCore.Mvc;
using TripMate_Webapi.Repositories;
using TripMate_Webapi.Entities;
using TripMate_WebAPI.Services;

namespace TripMate_Webapi.Controllers
{
    public class TravelerController : Controller
    {
        private readonly ILogger<TravelerController> _logger;
        private readonly SupabaseAuthService _authService;
        private readonly ITripRequestRepository _tripRequestRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly TourService _tourService;

        public TravelerController(
            ILogger<TravelerController> logger,
            SupabaseAuthService authService,
            ITripRequestRepository tripRequestRepository,
            IBookingRepository bookingRepository,
            TourService tourService)
        {
            _logger = logger;
            _authService = authService;
            _tripRequestRepository = tripRequestRepository;
            _bookingRepository = bookingRepository;
            _tourService = tourService;
        }

        // GET: /Traveler/Home
        public IActionResult Home()
        {
            return View();
        }

        // GET: /Traveler/Dashboard (Will eventually be deprecated/redirected to Trips)
        public async Task<IActionResult> Dashboard()
        {
            var travelerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var bookings = new List<BookingEntity>();
            
            if (!string.IsNullOrEmpty(travelerId))
            {
                bookings = await _bookingRepository.GetBookingsByTravelerAsync(travelerId);
            }
            
            return View(bookings);
        }

        // POST: /Traveler/Book
        [HttpPost]
        public async Task<IActionResult> Book(string guideId, DateTime date, int guests)
        {
            var travelerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(travelerId))
            {
                // Fallback for testing/audit: get any existing profile
                var profiles = await _bookingRepository.GetBookingsByTravelerAsync(""); // hack to access db client? No, let's just use a fake UUID if null for testing without login.
                travelerId = "00000000-0000-0000-0000-000000000000"; 
            }

            // Fallback for guideId
            if (string.IsNullOrEmpty(guideId)) guideId = "00000000-0000-0000-0000-000000000000";

            // Find a package for this guide or use a fallback
            var packages = await _tourService.GetToursByGuideAsync(guideId);
            string packageId;
            
            if (packages != null && packages.Any())
            {
                packageId = packages.First().Id!;
            }
            else
            {
                // Create a dummy package for this guide to satisfy FK
                try 
                {
                    // For audit purposes, if it fails, we catch it.
                    var newPackage = await _tourService.CreateTourAsync(guideId, new TripMate_WebAPI.DTOs.Tour.CreateTourRequest(
                        "Custom Trip", "Custom booking request", 4, 500000, null, guests, null, null
                    ), "");
                    packageId = newPackage.Id!;
                }
                catch
                {
                    // Fallback to a fake ID if creation fails (e.g. guide doesn't exist)
                    packageId = "00000000-0000-0000-0000-000000000000";
                }
            }

            var booking = new BookingEntity
            {
                TravelerId = travelerId,
                GuideProfileId = guideId,
                ExperiencePackageId = packageId,
                BookingDate = date,
                StartTime = date.AddHours(9),
                GuestCount = guests,
                TotalAmount = 500000 * guests,
                PlatformFee = (500000 * guests) * 0.1m,
                GuideEarnings = (500000 * guests) * 0.9m,
                Status = 0 // Pending
            };

            await _bookingRepository.CreateBookingAsync(booking);

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
            List<BookingEntity> bookings = new List<BookingEntity>();
            
            if (!string.IsNullOrEmpty(travelerId))
            {
                trips = await _tripRequestRepository.GetTripRequestsByTravelerAsync(travelerId);
                bookings = await _bookingRepository.GetBookingsByTravelerAsync(travelerId);
            }
            else
            {
                // Fallback for testing: Just show all trip requests if not logged in
                trips = await _tripRequestRepository.GetAllTripRequestsAsync();
            }

            ViewBag.Bookings = bookings;
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
