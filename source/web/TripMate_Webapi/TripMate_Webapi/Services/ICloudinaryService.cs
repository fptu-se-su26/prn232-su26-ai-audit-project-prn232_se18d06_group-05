using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TripMate_WebAPI.Services
{
    public interface ICloudinaryService
    {
        Task<string?> UploadImageAsync(IFormFile file, string folder = "tripmate_tours");
        Task<string?> UploadFileAsync(IFormFile file, string folder = "tripmate_chat");
        Task<List<string>> UploadImagesAsync(List<IFormFile> files, string folder = "tripmate_tours");
        Task<bool> DeleteImageAsync(string publicId);
    }
}
