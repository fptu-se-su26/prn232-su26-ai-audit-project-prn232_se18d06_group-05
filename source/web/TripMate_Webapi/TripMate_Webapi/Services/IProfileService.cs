using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TripMate_Webapi.Entities;

namespace TripMate_WebAPI.Services
{
    public interface IProfileService
    {
        Task<(bool Success, string Message, string? AvatarUrl)> UpdateTravelerProfileAsync(
            string userId, 
            string? fullName, 
            string? phone, 
            string? location, 
            string? email, 
            IFormFile? avatarFile,
            string? avatarUrlString = null);

        Task<ProfileEntity?> GetProfileAsync(string userId);
    }
}
