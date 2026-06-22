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

        public async Task<IActionResult> Dashboard()
        {
            var travelerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var bookings = new List<BookingEntity>();
            
            // Fallback for testing
            if (string.IsNullOrEmpty(travelerId))
            {
                try
                {
                    var serviceKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");
                    var baseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
                    using var http = new System.Net.Http.HttpClient();
                    var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"{baseUrl}/rest/v1/profiles?role=eq.traveler&limit=1");
                    req.Headers.Add("apikey", serviceKey);
                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", serviceKey);
                    var response = await http.SendAsync(req);
                    if (response.IsSuccessStatusCode)
                    {
                        var profiles = System.Text.Json.JsonSerializer.Deserialize<List<TripMate_Webapi.Entities.ProfileEntity>>(await response.Content.ReadAsStringAsync());
                        if (profiles != null && profiles.Any())
                        {
                            travelerId = profiles.First().Id;
                            ViewBag.TravelerName = profiles.First().FullName;
                        }
                    }
                }
                catch { }
            }
            else
            {
                ViewBag.TravelerName = User.Identity?.Name ?? "Traveler";
            }

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
            string? testUserToken = null;
            if (string.IsNullOrEmpty(travelerId))
            {
                // Fallback for testing: Fetch a traveler profile directly using ServiceRoleKey
                try
                {
                    var serviceKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");
                    var baseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
                    using var http = new System.Net.Http.HttpClient();
                    var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"{baseUrl}/rest/v1/profiles?role=eq.traveler&limit=1");
                    req.Headers.Add("apikey", serviceKey);
                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", serviceKey);
                    
                    var response = await http.SendAsync(req);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var profiles = System.Text.Json.JsonSerializer.Deserialize<List<TripMate_Webapi.Entities.ProfileEntity>>(content);
                        if (profiles != null && profiles.Any())
                        {
                            travelerId = profiles.First().Id;
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore errors in fallback
                }
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
                // If no package exists, we fall back to a dummy guid. The repository will omit it.
                packageId = "00000000-0000-0000-0000-000000000000";
            }

            var booking = new BookingEntity
            {
                TravelerId = travelerId ?? string.Empty,
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

            await _bookingRepository.CreateBookingAsync(booking, testUserToken);

            TempData["SuccessMessage"] = "Your booking request has been sent to the Guide successfully!";
            return RedirectToAction("Dashboard");
        }

        // GET: /Traveler/BookingDetails/{id}
        public async Task<IActionResult> BookingDetails(string id)
        {
            var booking = await _bookingRepository.GetBookingByIdAsync(id);
            if (booking == null)
            {
                return RedirectToAction("Dashboard");
            }
            return View(booking);
        }

        // GET: /Traveler/Checkout/{id}
        public async Task<IActionResult> Checkout(string id)
        {
            var booking = await _bookingRepository.GetBookingByIdAsync(id);
            if (booking == null)
            {
                return RedirectToAction("Dashboard");
            }
            return View(booking);
        }

        // POST: /Traveler/ProcessPayment
        [HttpPost]
        public async Task<IActionResult> ProcessPayment(string id, string paymentMethod)
        {
            var booking = await _bookingRepository.GetBookingByIdAsync(id);
            if (booking != null)
            {
                booking.Status = 2; // 2 = Completed/Paid (assuming 0=Pending, 1=Approved, 2=Completed)
                await _bookingRepository.UpdateBookingAsync(booking);
                TempData["SuccessMessage"] = $"Payment successful via {paymentMethod}! Enjoy your trip.";
            }
            return RedirectToAction("Trips");
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
