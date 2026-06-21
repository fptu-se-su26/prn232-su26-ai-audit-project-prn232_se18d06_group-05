using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;
using TripMate_Webapi.Repositories;
using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Controllers
{
    public class HomeController : Controller
    {
        private readonly TourService _tourService;
        private readonly IGuideRepository _guideRepository;
        private readonly ILogger<HomeController> _logger;

        public HomeController(TourService tourService, IGuideRepository guideRepository, ILogger<HomeController> logger)
        {
            _tourService = tourService;
            _guideRepository = guideRepository;
            _logger = logger;
        }

        // GET: /Home/Index or /
        public async Task<IActionResult> Index()
        {
            try
            {
                // Load tours and guides
                var tours = await _tourService.GetToursAsync();
                var guides = await _guideRepository.GetAllGuidesAsync();
                
                // Prepare view model
                var viewModel = new HomeViewModel
                {
                    FeaturedTours = tours.Take(2).ToList(),
                    CuratedStays = tours.Skip(2).Take(4).ToList(),
                    AllTours = tours.ToList(),
                    PopularGuides = guides.Take(4).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("error_log.txt", ex.ToString());
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

        // GET: /Home/Explore
        public async Task<IActionResult> Explore(string? destination = null, string? specialty = null)
        {
            try
            {
                var guides = await _guideRepository.GetGuidesFilteredAsync(destination, specialty);
                
                ViewBag.Destination = destination;
                ViewBag.Specialty = specialty;
                return View(guides);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching guides for explore page");
                return View(new List<GuideProfileEntity>());
            }
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
        public List<ExperiencePackageRow> FeaturedTours { get; set; } = new();
        public List<ExperiencePackageRow> CuratedStays { get; set; } = new();
        public List<ExperiencePackageRow> AllTours { get; set; } = new();
        public List<GuideProfileEntity> PopularGuides { get; set; } = new();
    }
}
