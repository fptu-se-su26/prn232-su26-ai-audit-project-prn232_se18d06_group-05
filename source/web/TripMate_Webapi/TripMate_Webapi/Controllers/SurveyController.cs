using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace TripMate_Webapi.Controllers
{
    /// <summary>
    /// MVC Controller for Personality Survey pages.
    /// Also handles server-side Session storage for quiz state (M2 - Quiz Persistence).
    /// </summary>
    public class SurveyController : Controller
    {
        private readonly ILogger<SurveyController> _logger;
        private const string PENDING_QUIZ_KEY = "PendingQuiz";
        private const string GHOST_BOOKING_KEY = "GhostBooking";

        public SurveyController(ILogger<SurveyController> logger)
        {
            _logger = logger;
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
}
