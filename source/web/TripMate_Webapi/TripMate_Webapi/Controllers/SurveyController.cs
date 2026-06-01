using Microsoft.AspNetCore.Mvc;

namespace TripMate_Webapi.Controllers
{
    /// <summary>
    /// MVC Controller for Personality Survey
    /// </summary>
    public class SurveyController : Controller
    {
        private readonly ILogger<SurveyController> _logger;

        public SurveyController(ILogger<SurveyController> logger)
        {
            _logger = logger;
        }

        // GET: /Survey/Personality
        public IActionResult Personality()
        {
            // Check if user is logged in
            // This is optional - can be done client-side too
            return View();
        }

        // GET: /Survey/Results
        public IActionResult Results()
        {
            return View();
        }
    }
}
