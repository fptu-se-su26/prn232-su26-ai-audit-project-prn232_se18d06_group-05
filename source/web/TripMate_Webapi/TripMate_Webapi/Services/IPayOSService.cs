namespace TripMate_WebAPI.Services
{
    using System.Threading.Tasks;
    using TripMate_Webapi.Entities;

    public interface IPayOSService
    {
        Task<string> CreatePaymentLink(BookingEntity booking, long orderCode, int amount);
    }
}
