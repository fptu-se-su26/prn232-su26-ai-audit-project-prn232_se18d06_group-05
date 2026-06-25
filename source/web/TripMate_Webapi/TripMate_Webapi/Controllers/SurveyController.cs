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

        /// <summary>
        /// M3 — Real Matching API: Tính điểm match giữa quiz preferences và guides trong DB.
        /// GET /Survey/CalculateMatches?destination=...&vibe=...&budget=...
        /// AllowAnonymous: user chưa login vẫn có thể xem kết quả matching.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CalculateMatches(
            string? destination = null,
            string? vibe = null,
            string? budget = null,
            int limit = 6)
        {
            try
            {
                // 1. Query guides từ DB theo destination
                List<GuideProfileEntity> guides;
                if (!string.IsNullOrWhiteSpace(destination) && destination != "Other" && destination != "Vietnam")
                {
                    guides = await _guideRepository.GetGuidesByDestinationAsync(destination);
                    // Nếu không có kết quả theo destination → fallback lấy tất cả
                    if (!guides.Any())
                        guides = await _guideRepository.GetAllGuidesAsync();
                }
                else
                {
                    guides = await _guideRepository.GetAllGuidesAsync();
                }

                if (!guides.Any())
                {
                    return Ok(new { success = true, guides = new List<object>(), total = 0 });
                }

                // 2. Tính match score cho từng guide
                var scoredGuides = new List<GuideMatchResult>();
                foreach (var guide in guides)
                {
                    var score = CalculateMatchScore(guide, destination, vibe, budget);

                    // Lấy packages của guide để hiển thị giá
                    List<ExperiencePackageRow>? packages = null;
                    if (!string.IsNullOrEmpty(guide.Id))
                    {
                        try { packages = await _tourService.GetToursByGuideAsync(guide.Id); }
                        catch { /* ignore nếu lỗi package */ }
                    }

                    var firstPackage = packages?.FirstOrDefault();
                    scoredGuides.Add(new GuideMatchResult
                    {
                        GuideId = guide.Id,
                        UserId = guide.UserId,
                        Name = guide.Profile?.FullName ?? "Local Guide",
                        Bio = guide.Bio,
                        CityArea = guide.CityArea,
                        Specialties = guide.Specialties ?? new List<string>(),
                        Languages = guide.Languages ?? new List<string>(),
                        CoverPhotoUrl = guide.CoverPhotoUrl,
                        AverageRating = guide.AverageRating ?? 0,
                        TotalReviews = guide.TotalReviews ?? 0,
                        MatchScore = score,
                        PackageTitle = firstPackage?.Title,
                        PricePerSession = firstPackage?.PricePerSession ?? 0,
                        PricePerPerson = firstPackage?.PricePerPerson,
                        PackageId = firstPackage?.Id
                    });
                }

                // 3. Sort theo score giảm dần, lấy top `limit`
                var topMatches = scoredGuides
                    .OrderByDescending(g => g.MatchScore)
                    .Take(limit)
                    .ToList();

                _logger.LogInformation("[Matching] Returning {Count} matches for destination={Dest}, vibe={Vibe}",
                    topMatches.Count, destination, vibe);

                return Ok(new { success = true, guides = topMatches, total = topMatches.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Matching] Error calculating matches");
                return StatusCode(500, new { success = false, message = "Không thể tính kết quả matching. Vui lòng thử lại." });
            }
        }

        /// <summary>
        /// Thuật toán tính match score (0-100) giữa quiz preferences và guide profile.
        /// Trọng số: Destination (40%) + Specialties/Vibe (35%) + Budget (25%)
        /// </summary>
        private static int CalculateMatchScore(GuideProfileEntity guide, string? destination, string? vibe, string? budget)
        {
            int score = 0;

            // ── Destination (40 điểm) ──────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(destination) && !string.IsNullOrWhiteSpace(guide.CityArea))
            {
                var destLower = destination.ToLower();
                var cityLower = guide.CityArea.ToLower();

                // Exact/contains match → điểm cao
                if (cityLower.Contains(destLower) || destLower.Contains(cityLower))
                    score += 40;
                else
                {
                    // Fuzzy: kiểm tra từng word trong destination
                    var destWords = destLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var matchedWords = destWords.Count(w => cityLower.Contains(w) && w.Length > 2);
                    score += Math.Min(30, matchedWords * 15); // tối đa 30 điểm
                }
            }
            else
            {
                score += 20; // Không có destination → điểm trung bình
            }

            // ── Specialties / Vibe matching (35 điểm) ─────────────────────────
            if (!string.IsNullOrWhiteSpace(vibe) && guide.Specialties != null && guide.Specialties.Any())
            {
                // Map vibe → keywords tìm trong specialties
                var vibeKeywords = vibe.ToLower() switch
                {
                    var v when v.Contains("chill") || v.Contains("relax") =>
                        new[] { "cafe", "coffee", "chill", "relax", "slow", "nature", "lake", "sunset" },
                    var v when v.Contains("adventure") || v.Contains("active") =>
                        new[] { "adventure", "hiking", "trekking", "outdoor", "sport", "extreme", "climbing" },
                    var v when v.Contains("culture") || v.Contains("history") =>
                        new[] { "history", "culture", "heritage", "museum", "temple", "art", "tradition" },
                    var v when v.Contains("food") || v.Contains("foodie") =>
                        new[] { "food", "street food", "cooking", "cuisine", "restaurant", "market", "local food" },
                    _ => new[] { "local", "guide", "experience" }
                };

                var specialtiesLower = guide.Specialties.Select(s => s.ToLower()).ToList();
                var matchCount = vibeKeywords.Count(kw => specialtiesLower.Any(s => s.Contains(kw)));
                score += Math.Min(35, matchCount * 12); // tối đa 35 điểm
            }
            else
            {
                score += 15; // Không có vibe → điểm trung bình
            }

            // ── Budget compatibility (25 điểm) ────────────────────────────────
            if (!string.IsNullOrWhiteSpace(budget))
            {
                var pricePerHour = guide.PricePerHour ?? 0;
                var budgetLower = budget.ToLower();

                bool budgetMatch = budgetLower switch
                {
                    var b when b.Contains("economy") || b.Contains("budget") => pricePerHour <= 300_000m,
                    var b when b.Contains("standard") => pricePerHour is > 200_000m and <= 600_000m,
                    var b when b.Contains("premium") => pricePerHour is > 400_000m and <= 1_000_000m,
                    var b when b.Contains("luxury") => pricePerHour >= 700_000m,
                    _ => true // Unknown budget → pass
                };

                score += budgetMatch ? 25 : 5; // Match → 25đ, không match → 5đ (vẫn hiện)
            }
            else
            {
                score += 15; // Không có budget → điểm trung bình
            }

            // ── Bonus: Guide đã verified và có reviews ──────────────────────────
            if (guide.IsVerified == true) score += 3;
            if ((guide.TotalReviews ?? 0) > 10) score += 2;
            if ((guide.AverageRating ?? 0) >= 4.5m) score += 3;

            return Math.Min(100, score); // Cap tối đa 100
        }

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
    /// DTO trả về từ Matching API — dùng trong JSON response cho Matches.cshtml
    /// </summary>
    public class GuideMatchResult
    {
        public string? GuideId { get; set; }
        public string? UserId { get; set; }
        public string Name { get; set; } = "Local Guide";
        public string? Bio { get; set; }
        public string? CityArea { get; set; }
        public List<string> Specialties { get; set; } = new();
        public List<string> Languages { get; set; } = new();
        public string? CoverPhotoUrl { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int MatchScore { get; set; }
        public string? PackageTitle { get; set; }
        public decimal PricePerSession { get; set; }
        public decimal? PricePerPerson { get; set; }
        public string? PackageId { get; set; }
    }
}
