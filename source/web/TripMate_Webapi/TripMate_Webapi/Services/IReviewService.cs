using System.Threading.Tasks;
using TripMate_Webapi.Entities;

namespace TripMate_WebAPI.Services
{
    public interface IReviewService
    {
        Task<(bool Success, string Message)> SubmitReviewAsync(string travelerId, string bookingId, int rating, string comment);
    }
}
