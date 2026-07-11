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

        // GET: /Home/Explore — Unified search results: guides + tours
        public async Task<IActionResult> Explore(string? destination = null, string? specialty = null, string? search = null)
        {
            try
            {
                var guides = await _guideRepository.GetGuidesFilteredAsync(destination ?? search, specialty);
                var tours = await _tourService.GetToursAsync(search ?? destination);
                
                // If searching by destination, also filter tours by guide's city_area
                if (!string.IsNullOrEmpty(destination))
                {
                    tours = tours.Where(t => 
                        t.GuideProfile?.CityArea != null && 
                        t.GuideProfile.CityArea.Contains(destination, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                ViewBag.Destination = destination;
                ViewBag.Specialty = specialty;
                ViewBag.Search = search ?? destination;
                ViewBag.Tours = tours;
                return View(guides);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching data for explore page");
                ViewBag.Tours = new List<ExperiencePackageRow>();
                return View(new List<GuideProfileEntity>());
            }
        }

        // GET: /Home/Tours — Browse all tours page
        public async Task<IActionResult> Tours(string? search = null, string? destination = null)
        {
            try
            {
                var tours = await _tourService.GetToursAsync(search);
                
                if (!string.IsNullOrEmpty(destination))
                {
                    tours = tours.Where(t => 
                        t.GuideProfile?.CityArea != null && 
                        t.GuideProfile.CityArea.Contains(destination, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                ViewBag.Search = search;
                ViewBag.Destination = destination;
                return View(tours);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tours page");
                return View(new List<ExperiencePackageRow>());
            }
        }

        // GET: /Home/Destinations — View all destinations page
        public async Task<IActionResult> Destinations()
        {
            try
            {
                var guides = await _guideRepository.GetAllGuidesAsync();
                var tours = await _tourService.GetToursAsync();
                
                ViewBag.Tours = tours;
                return View(guides);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading destinations page");
                ViewBag.Tours = new List<ExperiencePackageRow>();
                return View(new List<GuideProfileEntity>());
            }
        }

        // GET: /Home/SearchApi?q=...  — JSON API for AJAX search
        [HttpGet]
        public async Task<IActionResult> SearchApi(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(new { guides = new List<object>(), tours = new List<object>() });

            try
            {
                var guides = await _guideRepository.GetGuidesFilteredAsync(q, null);
                var tours = await _tourService.GetToursAsync(q);

                var guideResults = guides.Select(g => new {
                    id = g.Id,
                    name = g.Profile?.FullName ?? "Unknown",
                    avatar = g.Profile?.AvatarUrl ?? "",
                    coverPhoto = g.CoverPhotoUrl ?? "",
                    cityArea = g.CityArea ?? "",
                    rating = g.AverageRating ?? 0,
                    reviewCount = g.TotalReviews ?? 0,
                    pricePerHour = g.PricePerHour ?? 0,
                    specialties = g.Specialties ?? new List<string>()
                });

                var tourResults = tours.Select(t => new {
                    id = t.Id,
                    title = t.Title,
                    description = t.Description?.Length > 120 ? t.Description[..120] + "..." : t.Description,
                    duration = t.DurationHours,
                    pricePerSession = t.PricePerSession,
                    guideName = t.GuideProfile?.Profile?.FullName ?? "Unknown",
                    guideCityArea = t.GuideProfile?.CityArea ?? "",
                    guideProfileId = t.GuideProfileId
                });

                return Json(new { guides = guideResults, tours = tourResults });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchApi error");
                return Json(new { guides = new List<object>(), tours = new List<object>() });
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

        // GET: /Home/Support
        public IActionResult Support()
        {
            return View();
        }

        // GET: /Home/TermsOfService
        public IActionResult TermsOfService()
        {
            return View();
        }

        // GET: /Home/PrivacyPolicy
        public IActionResult PrivacyPolicy()
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
