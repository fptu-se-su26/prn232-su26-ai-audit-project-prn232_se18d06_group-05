using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;

namespace TripMate_Webapi.Controllers
{
    public class LandingPageController : Controller
    {
        private readonly TourService _tourService;
        private readonly ILogger<LandingPageController> _logger;

        public LandingPageController(TourService tourService, ILogger<LandingPageController> logger)
        {
            _tourService = tourService;
            _logger = logger;
        }

        // GET: /LandingPage/LandingPage or /
        public async Task<IActionResult> LandingPage()
        {
            try
            {
                // Load tours from service
                var tours = await _tourService.GetToursAsync();
                
                // Prepare view model
                var viewModel = new HomeViewModel
                {
                    FeaturedTours = tours.Take(2).ToList(),
                    CuratedStays = tours.Skip(2).Take(4).ToList(),
                    AllTours = tours.ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page");
                return View(new HomeViewModel());
            }
        }

        // GET: /Home/About
        public IActionResult About()
        {
            return View();
        }

        // GET: /Home/Contact
        public IActionResult Contact()
        {
            return View();
        }

        // GET: /LandingPage/Explore
        public IActionResult Explore()
        {
            return View();
        }

        // GET: /LandingPage/HowItWorks
        public IActionResult HowItWorks()
        {
            return View();
        }

        // GET: /LandingPage/BecomeAGuide
        public IActionResult BecomeAGuide()
        {
            return View();
        }

        // GET: /LandingPage/Support
        public IActionResult Support()
        {
            return View();
        }
    }

    // View Model
    public class HomeViewModel
    {
        public List<TourRow> FeaturedTours { get; set; } = new();
        public List<TourRow> CuratedStays { get; set; } = new();
        public List<TourRow> AllTours { get; set; } = new();
    }
}
