using System;
using System.Threading.Tasks;
using TripMate_Webapi.Entities;
using TripMate_Webapi.Repositories;

namespace TripMate_WebAPI.Services
{
    public class TravelerBookingService : ITravelerBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly BookingCreationService _bookingCreation;
        private readonly IPaymentService _payments;

        public TravelerBookingService(
            IBookingRepository bookingRepository,
            BookingCreationService bookingCreation,
            IPaymentService payments)
        {
            _bookingRepository = bookingRepository;
            _bookingCreation = bookingCreation;
            _payments = payments;
        }

        public async Task<(bool Success, string Message, string? BookingId, string? PaymentUrl)> CreateCustomBookingAsync(
            string travelerId, string guideId, DateTime date, int guests, string? notes)
        {
            try
            {
                // Preserve the legacy custom-tour amount while routing it through
                // the same booking/payment lifecycle as package and offer bookings.
                var agreedTotal = 500_000m * Math.Max(1, guests);
                var booking = await _bookingCreation.CreateAgreedBookingAsync(
                    travelerId, guideId, date, date, guests, agreedTotal, notes);
                var payment = await _payments.CreateRequiredPaymentAsync(booking);
                return (true, "Success", booking.Id, payment.CheckoutUrl);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null, null);
            }
        }

        public async Task<(bool Success, string Message, string? BookingId, string? PaymentUrl)> CreateTourBookingAsync(
            string travelerId, string guideId, string packageId, DateTime date, int guests, string? notes = null)
        {
            try
            {
                var booking = await _bookingCreation.CreatePackageBookingAsync(
                    travelerId, packageId, date, guests, notes);
                var payment = await _payments.CreateRequiredPaymentAsync(booking);
                return (true, "Success", booking.Id, payment.CheckoutUrl);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null, null);
            }
        }

        public async Task<(bool Success, string Message, string? BookingId, string? PaymentUrl)> CreateBookingFromOfferAsync(
            string travelerId, TripRequestEntity request, TripOfferEntity offer)
        {
            try
            {
                var guests = Math.Max(1, request.GroupSize);
                var booking = await _bookingCreation.CreateAgreedBookingAsync(
                    travelerId,
                    offer.GuideProfileId,
                    request.StartDate,
                    request.EndDate,
                    guests,
                    offer.ProposedPrice,
                    $"Accepted Offer for Trip Request: {request.Destination}");
                var payment = await _payments.CreateRequiredPaymentAsync(booking);
                return (true, "Success", booking.Id, payment.CheckoutUrl);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null, null);
            }
        }

        public async Task<(bool Success, string Message, int? NewStatus)> GetPaymentReturnStatusAsync(
            string travelerId, string bookingId, string? orderCode)
        {
            var result = await _payments.GetReturnStatusAsync(travelerId, bookingId, orderCode);
            return (result.Found && result.State != "failed", result.Message, result.BookingStatus);
            var tripName = await _notifications.GetTripNameAsync(booking.Id);
            var tripDate = NotificationTextFormatter.FormatTripDate(booking.BookingDate);
            await _notifications.SendAsync(
                booking.TravelerId,
                NotificationTypes.PaymentSucceeded,
                "Payment successful",
                $"We received your payment for \"{tripName}\" on {tripDate}. The guide can now review it.",
                new { bookingId = booking.Id, orderCode, amount = booking.TotalAmount },
                $"/Traveler/BookingDetails/{booking.Id}",
                $"payment-succeeded:{booking.Id}",
                sendEmail: true);

            if (isDeposit)
            {
                var guide = await _guideRepository.GetGuideByProfileIdAsync(booking.GuideProfileId);
                if (!string.IsNullOrWhiteSpace(guide?.UserId))
                {
                    await _notifications.SendAsync(
                        guide.UserId,
                        NotificationTypes.BookingAwaitingGuide,
                        "New paid booking awaiting your response",
                        $"A paid booking for \"{tripName}\" on {tripDate} with {booking.GuestCount} guest(s) is ready for your review.",
                        new { bookingId = booking.Id, booking.BookingDate, booking.GuestCount },
                        "/Guide/Bookings",
                        $"booking-awaiting-guide:{booking.Id}",
                        sendEmail: true);
                }
            }
        }

        public async Task<(bool Success, string Message, string? PaymentUrl)> RetryPaymentAsync(
            string travelerId, string bookingId)
        {
            var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
            if (booking == null || booking.TravelerId != travelerId)
                return (false, "Booking not found", null);

            if (booking.Status != -1 && (booking.Status != 1 || booking.AmountPaid >= booking.TotalAmount))
                return (false, "This booking does not require payment.", null);

            try
            {
                var payment = await _payments.CreateRequiredPaymentAsync(booking);
                return (true, "Success", payment.CheckoutUrl);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

    }
}
