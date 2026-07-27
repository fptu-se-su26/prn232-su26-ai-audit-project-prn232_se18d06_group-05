using System;
using System.Threading.Tasks;
using TripMate_Webapi.Entities;

namespace TripMate_WebAPI.Services
{
    public interface ITravelerBookingService
    {
        Task<(bool Success, string Message, string? BookingId, string? PaymentUrl)> CreateCustomBookingAsync(
            string travelerId, string guideId, DateTime date, int guests, string? notes);

        Task<(bool Success, string Message, string? BookingId, string? PaymentUrl)> CreateTourBookingAsync(
            string travelerId, string guideId, string packageId, DateTime date, int guests, string? notes = null);

        Task<(bool Success, string Message, string? BookingId, string? PaymentUrl)> CreateBookingFromOfferAsync(
            string travelerId, TripRequestEntity request, TripOfferEntity offer);

        Task<(bool Success, string Message, int? NewStatus)> GetPaymentReturnStatusAsync(
            string travelerId, string bookingId, string? orderCode, string? cancel = null);

        Task<(bool Success, string Message, string? PaymentUrl)> RetryPaymentAsync(
            string travelerId, string bookingId);
    }
}
