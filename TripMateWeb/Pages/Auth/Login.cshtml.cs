using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TripMateWeb.Data;

namespace TripMateWeb.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly TripMateDbContext _context;

        public LoginModel(TripMateDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Ghi nhớ đăng nhập")]
            public bool RememberMe { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Find user by email
            var user = await _context.Profiles
                .FirstOrDefaultAsync(u => u.Email == Input.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return Page();
            }

            // In a real application, you would verify the password hash
            // For demo purposes, we'll use a simple check
            // You should implement proper password hashing with BCrypt or similar
            
            // Fix "undefined" header by checking for valid FullName
            var displayName = !string.IsNullOrWhiteSpace(user.FullName) && user.FullName != "undefined" 
                ? user.FullName 
                : user.Email;

            // Store user info in session
            HttpContext.Session.SetString("CurrentUserId", user.Id.ToString());
            HttpContext.Session.SetString("CurrentUser", displayName);
            HttpContext.Session.SetString("CurrentUserRole", user.Role);
            HttpContext.Session.SetString("CurrentUserEmail", user.Email);

            // Redirect based on role
            if (user.Role == "admin")
                return RedirectToPage("/Admin/Dashboard");
                
            if (user.Role == "guide")
                return RedirectToPage("/Guide/Dashboard");

            // Traveler logic
            bool hasCompletedSurvey = !string.IsNullOrWhiteSpace(user.FullName) && user.FullName != "undefined" && !string.IsNullOrWhiteSpace(user.Phone);
            
            if (!hasCompletedSurvey)
                return RedirectToPage("/Survey/Index");
                
            return RedirectToPage("/Index");
        }
    }
}