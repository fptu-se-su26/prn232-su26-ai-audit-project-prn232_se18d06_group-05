using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TripMate_Webapi.Entities;

namespace TripMate_WebAPI.Services
{
    public interface ITravelerBookingService
    {
        Task<(bool Success, string Message, string? BookingId, string? PaymentUrl)> CreateCustomBookingAsync(
            string travelerId, string guideId, DateTime date, int guests, string? notes);

        Task<(bool Success, string Message, string? BookingId, string? PaymentUrl)> CreateTourBookingAsync(
            string travelerId, string guideId, string packageId, DateTime date, int guests);

        Task<(bool Success, string Message, int? NewStatus)> ProcessPaymentCallbackAsync(
            string bookingId, string status, string cancel, string orderCode);

        Task<(bool Success, string Message, string? PaymentUrl)> RetryPaymentAsync(
            string travelerId, string bookingId);

        Task TryAutoCompleteBookingsAsync(IEnumerable<BookingEntity> bookings);
    }
}
