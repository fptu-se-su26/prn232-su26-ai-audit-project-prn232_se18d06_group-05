using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TripMate_Webapi.Entities;

namespace TripMate_WebAPI.Services
{
    public class ProfileService : IProfileService
    {
        private readonly Supabase.Client _supabase;
        private readonly ICloudinaryService _cloudinary;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(
            Supabase.Client supabase,
            ICloudinaryService cloudinary,
            IMemoryCache cache,
            ILogger<ProfileService> logger)
        {
            _supabase = supabase;
            _cloudinary = cloudinary;
            _cache = cache;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, string? AvatarUrl)> UpdateTravelerProfileAsync(
            string userId, 
            string? fullName, 
            string? phone, 
            string? location, 
            string? email, 
            IFormFile? avatarFile,
            string? avatarUrlString = null)
        {
            try
            {
                var profileResponse = await _supabase.From<ProfileEntity>().Where(x => x.Id == userId).Get();
                var profile = profileResponse.Models.FirstOrDefault();

                if (profile == null)
                {
                    return (false, "Profile not found", null);
                }

                if (fullName != null) profile.FullName = fullName;
                if (phone != null) profile.Phone = phone;
                if (location != null) profile.Location = location;
                if (email != null) profile.Email = email;

                if (avatarUrlString != null)
                {
                    profile.AvatarUrl = avatarUrlString;
                }
                
                if (avatarFile != null)
                {
                    var avatarUrl = await _cloudinary.UploadImageAsync(avatarFile, "tripmate_avatars");
                    if (!string.IsNullOrEmpty(avatarUrl))
                    {
                        profile.AvatarUrl = avatarUrl;
                    }
                }

                await _supabase.From<ProfileEntity>().Update(profile);
                
                // Invalidate header component cache
                _cache.Remove($"HeaderProfile_{userId}");

                return (true, "Profile updated successfully", profile.AvatarUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return (false, "Internal server error", null);
            }
        }

        public async Task<ProfileEntity?> GetProfileAsync(string userId)
        {
            try
            {
                var profileResponse = await _supabase.From<ProfileEntity>().Where(x => x.Id == userId).Get();
                return profileResponse.Models.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile for user {UserId}", userId);
                return null;
            }
        }
    }
}
