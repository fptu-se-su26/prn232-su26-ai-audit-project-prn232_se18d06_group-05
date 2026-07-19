using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TripMate_Webapi.Repositories;
using TripMate_Webapi.Entities;
using TripMate_WebAPI.Services;

namespace TripMate_Webapi.Controllers
{
    /// <summary>
    /// MVC Controller for Personality Survey pages.
    /// Also handles server-side Session storage for quiz state (M2 - Quiz Persistence).
    /// </summary>
    public class SurveyController : Controller
    {
        private readonly ILogger<SurveyController> _logger;
        private readonly IGuideRepository _guideRepository;
        private readonly TourService _tourService;
        private const string PENDING_QUIZ_KEY = "PendingQuiz";
        private const string GHOST_BOOKING_KEY = "GhostBooking";

        public SurveyController(
            ILogger<SurveyController> logger,
            IGuideRepository guideRepository,
            TourService tourService)
        {
            _logger = logger;
            _guideRepository = guideRepository;
            _tourService = tourService;
        }

        // GET: /Survey
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Survey/Personality
        public IActionResult Personality()
        {
            return View();
        }

        // GET: /Survey/Results
        public IActionResult Results()
        {
            return View();
        }

        // GET: /Survey/Matches
        public IActionResult Matches()
        {
            // Đọc quiz từ Session để truyền sang View (nếu có)
            var quizJson = HttpContext.Session.GetString(PENDING_QUIZ_KEY);
            if (!string.IsNullOrEmpty(quizJson))
            {
                ViewBag.PendingQuizJson = quizJson;
            }
            return View();
        }

        /// <summary>
        /// M2 — Lưu quiz answers vào server-side Session.
        /// POST /Survey/SavePreferences
        /// AllowAnonymous: user chưa đăng nhập vẫn có thể lưu để khôi phục sau khi login.
        /// </summary>
        [HttpPost]
        public IActionResult SavePreferences([FromBody] JsonElement payload)
        {
            try
            {
                var json = payload.GetRawText();
                HttpContext.Session.SetString(PENDING_QUIZ_KEY, json);
                _logger.LogInformation("[Quiz Session] Saved quiz preferences to session.");
                return Ok(new { success = true, message = "Quiz preferences saved to session." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Quiz Session] Failed to save quiz preferences.");
                return StatusCode(500, new { success = false, message = "Could not save preferences." });
            }
        }

        /// <summary>
        /// M2 — Khôi phục quiz state từ Session sau khi user đăng nhập.
        /// GET /Survey/GetPendingQuiz
        /// </summary>
        [HttpGet]
        public IActionResult GetPendingQuiz()
        {
            var quizJson = HttpContext.Session.GetString(PENDING_QUIZ_KEY);
            if (string.IsNullOrEmpty(quizJson))
            {
                return Ok(new { found = false });
            }
            return Ok(new { found = true, data = JsonSerializer.Deserialize<JsonElement>(quizJson) });
        }

        /// <summary>
        /// M1 — Ghost Booking: Lưu booking intent vào Session (không cần đăng nhập).
        /// POST /Survey/HoldBooking
        /// </summary>
        [HttpPost]
        public IActionResult HoldBooking([FromBody] JsonElement payload)
        {
            try
            {
                var json = payload.GetRawText();
                HttpContext.Session.SetString(GHOST_BOOKING_KEY, json);
                _logger.LogInformation("[Ghost Booking] Saved booking intent to session.");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Ghost Booking] Failed to save booking intent.");
                return StatusCode(500, new { success = false });
            }
        }

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
                if (RemoveDiacritics(canonical.ToLowerInvariant()) == norm) return canonical;
                foreach (var alias in aliases)
                    if (RemoveDiacritics(alias) == norm) return canonical;
            }
            return null;
        }

        private static string RemoveDiacritics(string s) =>
            new string(s.Normalize(System.Text.NormalizationForm.FormD)
                        .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                                    != System.Globalization.UnicodeCategory.NonSpacingMark)
                        .ToArray())
                .Replace("đ", "d").Replace("Đ", "D");

        /// <summary>
        /// M3 — Hybrid AI Matching API
        /// Cấp độ 1: Hard Filters (Location)
        /// Cấp độ 2: Weighted Scoring (Jaccard Similarity + Vibe + Budget + Demographics)
        /// GET /Survey/CalculateMatches?destination=...&vibe=...&budget=...&interests=...
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CalculateMatches(
            string? destination = null,
            string? vibe = null,
            string? budget = null,
            string? interests = null,
            int limit = 6)
        {
            try
            {
                // ═══ LEVEL 1: HARD FILTERS ═══════════════════════════════════════
                var allGuides = await _guideRepository.GetAllGuidesAsync();

                List<GuideProfileEntity> filteredGuides;
                bool hasDestinationFilter = !string.IsNullOrWhiteSpace(destination)
                    && destination != "Other" && destination != "Vietnam";

                if (hasDestinationFilter)
                {
                    var normalizedTarget = NormalizeCity(destination!) ?? destination!;
                    filteredGuides = allGuides.Where(g =>
                    {
                        if (string.IsNullOrWhiteSpace(g.CityArea)) return false;
                        var normalizedGuideCity = NormalizeCity(g.CityArea) ?? g.CityArea;
                        return normalizedGuideCity.Contains(normalizedTarget, StringComparison.OrdinalIgnoreCase) ||
                               normalizedTarget.Contains(normalizedGuideCity, StringComparison.OrdinalIgnoreCase);
                    }).ToList();

                    // Do not fallback to all guides. We strictly want to filter out other cities.
                }
                else
                {
                    filteredGuides = allGuides;
                }

                if (!filteredGuides.Any())
                    return Ok(new { success = true, guides = new List<object>(), total = 0 });

                // ═══ LEVEL 2: WEIGHTED SCORING ═══════════════════════════════════
                var travelerInterests = string.IsNullOrWhiteSpace(interests)
                    ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(
                        interests.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                        StringComparer.OrdinalIgnoreCase);

                // Map vibe → interest keywords (bổ sung vào travelerInterests)
                if (!string.IsNullOrWhiteSpace(vibe))
                {
                    var vibeKeywords = MapVibeToKeywords(vibe);
                    foreach (var kw in vibeKeywords)
                        travelerInterests.Add(kw);
                }

                var scoredGuides = new List<GuideMatchResult>();
                foreach (var guide in filteredGuides)
                {
                    var (score, reasons) = CalculateHybridMatchScore(
                        guide, destination, vibe, budget, travelerInterests, hasDestinationFilter);

                    // Lấy packages của guide để hiển thị giá
                    List<ExperiencePackageRow>? packages = null;
                    if (!string.IsNullOrEmpty(guide.Id))
                    {
                        try { packages = await _tourService.GetToursByGuideAsync(guide.Id); }
                        catch { /* ignore */ }
                    }

                    var firstPackage = packages?.FirstOrDefault();
                    scoredGuides.Add(new GuideMatchResult
                    {
                        GuideId = guide.Id,
                        UserId = guide.UserId,
                        Name = guide.Profile?.FullName ?? "Local Guide",
                        AvatarUrl = guide.Profile?.AvatarUrl,
                        Bio = guide.Bio,
                        CityArea = guide.CityArea,
                        Specialties = guide.Specialties ?? new List<string>(),
                        Languages = guide.Languages ?? new List<string>(),
                        CoverPhotoUrl = guide.CoverPhotoUrl,
                        AverageRating = guide.AverageRating ?? 0,
                        TotalReviews = guide.TotalReviews ?? 0,
                        MatchScore = score,
                        MatchReasons = reasons,
                        PackageTitle = firstPackage?.Title,
                        PricePerSession = firstPackage?.PricePerSession ?? 0,
                        PricePerPerson = firstPackage?.PricePerPerson,
                        PackageId = firstPackage?.Id
                    });
                }

                // Sort theo score giảm dần
                var topMatches = scoredGuides
                    .OrderByDescending(g => g.MatchScore)
                    .Take(limit)
                    .ToList();

                _logger.LogInformation(
                    "[AI Matching] {Count} matches for dest={Dest}, vibe={Vibe}, budget={Budget}. Top score: {Top}%",
                    topMatches.Count, destination, vibe, budget, topMatches.FirstOrDefault()?.MatchScore ?? 0);

                return Ok(new { success = true, guides = topMatches, total = topMatches.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI Matching] Error calculating matches");
                return StatusCode(500, new { success = false, message = "Could not calculate matches. Please try again." });
            }
        }

        // ═══ HYBRID SCORING ENGINE ═══════════════════════════════════════════════

        /// <summary>
        /// Tính điểm Hybrid Match (0-100) với 4 trọng số + Explainable AI reasons.
        /// W1: Interests (Jaccard Similarity) = 40%
        /// W2: Travel Vibe = 30%
        /// W3: Budget Compatibility = 20%
        /// W4: Demographics / Bonus = 10%
        /// </summary>
        private static (int Score, List<string> Reasons) CalculateHybridMatchScore(
            GuideProfileEntity guide,
            string? destination,
            string? vibe,
            string? budget,
            HashSet<string> travelerInterests,
            bool passedHardFilter)
        {
            double totalScore = 0;
            var reasons = new List<string>();

            // ── W1: Interests / Specialties — Jaccard Similarity (40%) ──────────
            var guideSpecialties = new HashSet<string>(
                (guide.Specialties ?? new List<string>()).Select(s => s.ToLower()),
                StringComparer.OrdinalIgnoreCase);

            if (travelerInterests.Any() && guideSpecialties.Any())
            {
                var intersection = travelerInterests.Intersect(guideSpecialties, StringComparer.OrdinalIgnoreCase).ToList();
                var union = travelerInterests.Union(guideSpecialties, StringComparer.OrdinalIgnoreCase);
                double jaccard = (double)intersection.Count / union.Count();

                // Cũng tính partial matching (từ trong specialty chứa keyword)
                var partialMatches = new List<string>();
                foreach (var interest in travelerInterests)
                {
                    foreach (var spec in guideSpecialties)
                    {
                        if (spec.Contains(interest, StringComparison.OrdinalIgnoreCase) ||
                            interest.Contains(spec, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!intersection.Contains(spec, StringComparer.OrdinalIgnoreCase) &&
                                !partialMatches.Contains(spec, StringComparer.OrdinalIgnoreCase))
                                partialMatches.Add(spec);
                        }
                    }
                }

                double partialBonus = partialMatches.Count * 0.05; // 5% mỗi partial match
                double interestScore = Math.Min(1.0, jaccard + partialBonus);
                totalScore += interestScore * 40;

                // Explainable AI: Lý do
                var matchedLabels = intersection.Concat(partialMatches).Distinct(StringComparer.OrdinalIgnoreCase).Take(3);
                if (matchedLabels.Any())
                {
                    var joined = string.Join(", ", matchedLabels.Select(m =>
                        System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(m)));
                    reasons.Add($"Shared interests: {joined}");
                }
            }
            else if (!travelerInterests.Any())
            {
                totalScore += 20; // No interests provided → neutral score
            }

            // ── W2: Travel Vibe Match (30%) ─────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(vibe) && guideSpecialties.Any())
            {
                var vibeKeywords = MapVibeToKeywords(vibe);
                var vibeMatched = vibeKeywords.Where(kw =>
                    guideSpecialties.Any(s => s.Contains(kw, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                double vibeRatio = vibeKeywords.Length > 0
                    ? (double)vibeMatched.Count / vibeKeywords.Length
                    : 0;
                totalScore += vibeRatio * 30;

                if (vibeMatched.Any())
                {
                    var vibeName = vibe.Contains("Chill") ? "relaxed exploration" :
                                   vibe.Contains("Adventure") ? "adventure activities" :
                                   vibe.Contains("Culture") ? "cultural experiences" :
                                   vibe.Contains("Food") ? "food exploration" :
                                   "your travel style";
                    reasons.Add($"Great match for {vibeName}");
                }
            }
            else if (string.IsNullOrWhiteSpace(vibe))
            {
                totalScore += 15; // No vibe → neutral
            }

            // ── W3: Budget Compatibility (20%) ──────────────────────────────────
            if (!string.IsNullOrWhiteSpace(budget))
            {
                var pricePerHour = guide.PricePerHour ?? 0;
                var budgetLower = budget.ToLower();

                var (budgetMatch, budgetLabel) = budgetLower switch
                {
                    var b when b.Contains("budget") => (pricePerHour <= 400_000m, "budget-friendly"),
                    var b when b.Contains("standard") => (pricePerHour is > 200_000m and <= 800_000m, "standard"),
                    var b when b.Contains("premium") => (pricePerHour > 500_000m, "premium"),
                    _ => (true, "flexible")
                };

                totalScore += budgetMatch ? 20 : 5;
                if (budgetMatch)
                    reasons.Add($"Fits your {budgetLabel} budget");
            }
            else
            {
                totalScore += 10; // No budget → neutral
            }

            // ── W4: Demographics & Trust Bonus (10%) ────────────────────────────
            double demoScore = 0;

            if (guide.IsVerified == true)
            {
                demoScore += 4;
                reasons.Add("✓ Verified guide");
            }

            if ((guide.AverageRating ?? 0) >= 4.5m)
            {
                demoScore += 3;
                reasons.Add($"Highly rated ({guide.AverageRating:0.0}★)");
            }
            else if ((guide.AverageRating ?? 0) >= 4.0m)
            {
                demoScore += 2;
            }

            if ((guide.TotalReviews ?? 0) > 5)
                demoScore += 2;
            if ((guide.TotalReviews ?? 0) > 20)
                demoScore += 1;

            totalScore += Math.Min(10, demoScore);

            // ── Location bonus (nếu đã vượt qua Hard Filter) ───────────────────
            if (passedHardFilter && !string.IsNullOrWhiteSpace(destination) &&
                !string.IsNullOrWhiteSpace(guide.CityArea) &&
                guide.CityArea.Contains(destination, StringComparison.OrdinalIgnoreCase))
            {
                reasons.Insert(0, $"Located in {guide.CityArea}");
            }

            int finalScore = (int)Math.Round(Math.Min(99, Math.Max(15, totalScore)));
            return (finalScore, reasons);
        }

        /// <summary>
        /// Map vibe string → related keywords for matching against guide specialties.
        /// </summary>
        private static string[] MapVibeToKeywords(string vibe) => vibe.ToLower() switch
        {
            var v when v.Contains("chill") || v.Contains("relax") =>
                new[] { "cafe", "coffee", "chill", "relax", "nature", "lake", "sunset", "spa", "beach" },
            var v when v.Contains("adventure") || v.Contains("active") =>
                new[] { "adventure", "hiking", "trekking", "outdoor", "sport", "climbing", "motorbike", "kayak" },
            var v when v.Contains("culture") || v.Contains("history") =>
                new[] { "history", "culture", "heritage", "museum", "temple", "art", "tradition", "architecture" },
            var v when v.Contains("food") || v.Contains("foodie") =>
                new[] { "food", "street food", "cooking", "cuisine", "restaurant", "market", "local food", "culinary" },
            _ => new[] { "local", "guide", "experience", "sightseeing" }
        };

        /// <summary>
        /// Xóa Session sau khi booking hoàn tất.
        /// POST /Survey/ClearSession
        /// </summary>
        [HttpPost]
        public IActionResult ClearSession()
        {
            HttpContext.Session.Remove(PENDING_QUIZ_KEY);
            HttpContext.Session.Remove(GHOST_BOOKING_KEY);
            return Ok(new { success = true });
        }
    }

    /// <summary>
    /// DTO trả về từ Matching API — dùng trong JSON response cho Matches.cshtml.
    /// Includes Explainable AI reasons for match transparency.
    /// </summary>
    public class GuideMatchResult
    {
        public string? GuideId { get; set; }
        public string? UserId { get; set; }
        public string Name { get; set; } = "Local Guide";
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? CityArea { get; set; }
        public List<string> Specialties { get; set; } = new();
        public List<string> Languages { get; set; } = new();
        public string? CoverPhotoUrl { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int MatchScore { get; set; }
        public List<string> MatchReasons { get; set; } = new();
        public string? PackageTitle { get; set; }
        public decimal PricePerSession { get; set; }
        public decimal? PricePerPerson { get; set; }
        public string? PackageId { get; set; }
    }
}
