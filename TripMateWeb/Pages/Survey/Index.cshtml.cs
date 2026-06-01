using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TripMateWeb.Data;
using System.ComponentModel.DataAnnotations;

namespace TripMateWeb.Pages.Survey
{
    public class IndexModel : PageModel
    {
        private readonly TripMateDbContext _context;

        public IndexModel(TripMateDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public SurveyInputModel Input { get; set; } = new();

        public class SurveyInputModel
        {
            [Required(ErrorMessage = "Họ và tên là bắt buộc")]
            [Display(Name = "Họ và tên")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            [Display(Name = "Số điện thoại")]
            public string Phone { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = HttpContext.Session.GetString("CurrentUserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToPage("/Auth/Login");
            }

            if (Guid.TryParse(userIdStr, out var userId))
            {
                var user = await _context.Profiles.FindAsync(userId);
                if (user != null)
                {
                    Input.FullName = user.FullName == "undefined" ? "" : (user.FullName ?? "");
                    Input.Phone = user.Phone ?? "";
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userIdStr = HttpContext.Session.GetString("CurrentUserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToPage("/Auth/Login");
            }

            if (Guid.TryParse(userIdStr, out var userId))
            {
                var user = await _context.Profiles.FindAsync(userId);
                if (user != null)
                {
                    user.FullName = Input.FullName;
                    user.Phone = Input.Phone;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    
                    // Update session
                    HttpContext.Session.SetString("CurrentUser", user.FullName);

                    return RedirectToPage("/Index");
                }
            }

            return RedirectToPage("/Auth/Login");
        }
    }
}
