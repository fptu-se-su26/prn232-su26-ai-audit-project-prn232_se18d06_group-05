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

        // GET: /Traveler/Review/{id}
        public IActionResult Review(string id = "1")
        {
            return View();
        }

        // GET: /Traveler/Trips
        public IActionResult Trips()
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
