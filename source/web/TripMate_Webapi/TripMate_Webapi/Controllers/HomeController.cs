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
        private readonly IReviewRepository _reviewRepository;
        private readonly ILogger<HomeController> _logger;

        // City alias map: canonical DB value (city_area) → fuzzy aliases
        private static readonly Dictionary<string, string[]> CityAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Danang"]       = new[] { "da nang", "đà nẵng", "danang", "da-nang", "dn" },
            ["Hanoi"]        = new[] { "ha noi", "hà nội", "hanoi", "hn" },
            ["Hoi An"]       = new[] { "hoian", "hội an", "hoi-an" },
            ["Ho Chi Minh"]  = new[] { "hcm", "hcmc", "saigon", "sài gòn", "sai gon", "ho chi minh city", "sg" },
            ["Sapa"]         = new[] { "sa pa", "sả pa" },
            ["Nha Trang"]    = new[] { "nhatrang" },
            ["Da Lat"]       = new[] { "dalat", "đà lạt", "da-lat" },
            ["Hue"]          = new[] { "huế", "thua thien", "thừa thiên" },
            ["Phu Quoc"]     = new[] { "phú quốc", "phuquoc", "phu-quoc" },
            ["Ha Long"]      = new[] { "halong", "hạ long", "vịnh hạ long", "ha long bay" },
        };

        private static string? NormalizeCity(string input)
        {
            var norm = RemoveDiacritics(input.Trim().ToLowerInvariant());

            foreach (var (canonical, aliases) in CityAliases)
            {
                // Check canonical itself
                if (RemoveDiacritics(canonical.ToLowerInvariant()) == norm) return canonical;
                // Check aliases
                foreach (var alias in aliases)
                    if (RemoveDiacritics(alias) == norm) return canonical;
            }
            return null; // Return null if not a known city so we know it's a general search term
        }

        private static string RemoveDiacritics(string s) =>
            new string(s.Normalize(System.Text.NormalizationForm.FormD)
                        .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                                    != System.Globalization.UnicodeCategory.NonSpacingMark)
                        .ToArray())
                .Replace("đ", "d").Replace("Đ", "D");

        public HomeController(TourService tourService, IGuideRepository guideRepository, IReviewRepository reviewRepository, ILogger<HomeController> logger)
        {
            _tourService = tourService;
            _guideRepository = guideRepository;
            _reviewRepository = reviewRepository;
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
                var reviews = await _reviewRepository.GetAllReviewsAsync();
                
                // Prepare view model
                var viewModel = new HomeViewModel
                {
                    FeaturedTours = tours.Take(2).ToList(),
                    CuratedStays = tours.Skip(2).Take(4).ToList(),
                    AllTours = tours.ToList(),
                    PopularGuides = guides.Take(4).ToList(),
                    RecentReviews = reviews.Where(r => r.Rating >= 4).OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt).Take(6).ToList() // Only show top 6 positive reviews
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
                // Unified search term — prefer `search`, fall back to `destination`
                var rawQuery = search ?? destination;
                var normalizedCity = string.IsNullOrWhiteSpace(rawQuery) ? null : NormalizeCity(rawQuery);

                var guides = await _guideRepository.GetAllGuidesAsync();
                var tours = await _tourService.GetToursAsync();

                // Filter Guides
                if (!string.IsNullOrWhiteSpace(rawQuery))
                {
                    if (normalizedCity != null)
                    {
                        guides = guides.Where(g => g.CityArea != null && g.CityArea.Contains(normalizedCity, StringComparison.OrdinalIgnoreCase)).ToList();
                    }
                    else
                    {
                        guides = guides.Where(g => 
                            (g.CityArea != null && g.CityArea.Contains(rawQuery, StringComparison.OrdinalIgnoreCase)) ||
                            (g.Profile?.FullName != null && g.Profile.FullName.Contains(rawQuery, StringComparison.OrdinalIgnoreCase)) ||
                            (g.Specialties != null && g.Specialties.Any(s => s.Contains(rawQuery, StringComparison.OrdinalIgnoreCase))) ||
                            (g.Bio != null && g.Bio.Contains(rawQuery, StringComparison.OrdinalIgnoreCase))
                        ).ToList();
                    }
                }

                if (!string.IsNullOrWhiteSpace(specialty))
                {
                    guides = guides.Where(g => g.Specialties != null && g.Specialties.Any(s => s.Contains(specialty, StringComparison.OrdinalIgnoreCase))).ToList();
                }

                // Filter Tours
                if (!string.IsNullOrWhiteSpace(rawQuery))
                {
                    if (normalizedCity != null)
                    {
                        tours = tours.Where(t => t.GuideProfile?.CityArea != null && t.GuideProfile.CityArea.Contains(normalizedCity, StringComparison.OrdinalIgnoreCase)).ToList();
                    }
                    else
                    {
                        tours = tours.Where(t => 
                            (t.Title != null && t.Title.Contains(rawQuery, StringComparison.OrdinalIgnoreCase)) ||
                            (t.Description != null && t.Description.Contains(rawQuery, StringComparison.OrdinalIgnoreCase)) ||
                            (t.Tags != null && t.Tags.Any(tag => tag.Contains(rawQuery, StringComparison.OrdinalIgnoreCase))) ||
                            (t.GuideProfile?.CityArea != null && t.GuideProfile.CityArea.Contains(rawQuery, StringComparison.OrdinalIgnoreCase))
                        ).ToList();
                    }
                }

                ViewBag.Destination = normalizedCity ?? (string.IsNullOrWhiteSpace(rawQuery) ? null : rawQuery);
                ViewBag.Specialty = specialty;
                ViewBag.Search = rawQuery;
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
                var tours = await _tourService.GetToursAsync();
                
                if (!string.IsNullOrWhiteSpace(search))
                {
                    tours = tours.Where(t => 
                        (t.Title != null && t.Title.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (t.Description != null && t.Description.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (t.Tags != null && t.Tags.Any(tag => tag.Contains(search, StringComparison.OrdinalIgnoreCase))) ||
                        (t.GuideProfile?.CityArea != null && t.GuideProfile.CityArea.Contains(search, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                if (!string.IsNullOrWhiteSpace(destination))
                {
                    var normDest = NormalizeCity(destination) ?? destination;
                    tours = tours.Where(t => 
                        t.GuideProfile?.CityArea != null && 
                        t.GuideProfile.CityArea.Contains(normDest, StringComparison.OrdinalIgnoreCase)
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
        public List<ReviewEntity> RecentReviews { get; set; } = new();
    }
}
