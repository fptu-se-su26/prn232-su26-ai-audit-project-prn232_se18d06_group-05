using Microsoft.AspNetCore.Mvc;

namespace TripMate_Webapi.Controllers
{
    /// <summary>
    /// MVC Controller for Authentication Views (Login, Register)
    /// Note: Actual authentication logic is handled by API endpoints in /api/auth
    /// </summary>
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;
        }

        // GET: /Auth/Login
        public IActionResult Login()
        {
            // Check if user is already logged in
            // If so, redirect to home
            // (This is optional - can be done client-side too)
            return View();
        }

        // GET: /Auth/Register
        public IActionResult Register()
        {
            return View();
        }

        // GET: /Auth/Logout
        public IActionResult Logout()
        {
            // Clear any server-side session if needed
            // Client-side will clear localStorage
            return RedirectToAction("Index", "Home");
        }
    }
}
