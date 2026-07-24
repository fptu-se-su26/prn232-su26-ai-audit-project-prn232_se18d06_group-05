using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using TripMate_Webapi.Entities;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace TripMate_Webapi.ViewComponents
{
    public class ProfileHeaderViewComponent : ViewComponent
    {
        private readonly Supabase.Client _supabase;
        private readonly IMemoryCache _cache;

        public ProfileHeaderViewComponent(Supabase.Client supabase, IMemoryCache cache)
        {
            _supabase = supabase;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier) 
                         ?? UserClaimsPrincipal?.FindFirstValue("sub");

            if (string.IsNullOrEmpty(userId))
            {
                return View("Default", (ProfileHeaderViewModel?)null);
            }

            var cacheKey = $"HeaderProfile_{userId}";
            if (!_cache.TryGetValue(cacheKey, out ProfileHeaderViewModel? model))
            {
                var response = await _supabase.From<ProfileEntity>().Where(x => x.Id == userId).Get();
                var profile = response.Models.FirstOrDefault();
                
                model = new ProfileHeaderViewModel
                {
                    FullName = profile?.FullName ?? UserClaimsPrincipal?.FindFirstValue(ClaimTypes.Email)?.Split('@')[0] ?? "User",
                    AvatarUrl = profile?.AvatarUrl,
                    Role = profile?.Role ?? "traveler",
                    Email = UserClaimsPrincipal?.FindFirstValue(ClaimTypes.Email) ?? profile?.Email ?? ""
                };

                // Cache for 5 minutes
                _cache.Set(cacheKey, model, TimeSpan.FromMinutes(5));
            }

            return View("Default", model);
        }
    }

    public class ProfileHeaderViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
