using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TripMate_Webapi.Entities;
using TripMate_Webapi.Repositories;

namespace TripMate_WebAPI.Services
{
    public class TravelerBookingService : ITravelerBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IGuideRepository _guideRepository;
        private readonly TourService _tourService;
        private readonly IPayOSService _payOSService;
        private readonly INotificationService _notifications;
        private readonly ILogger<TravelerBookingService> _logger;

        public TravelerBookingService(
            IBookingRepository bookingRepository,
            IGuideRepository guideRepository,
            TourService tourService,
            IPayOSService payOSService,
            INotificationService notifications,
            ILogger<TravelerBookingService> logger)
        {
            _bookingRepository = bookingRepository;
            _guideRepository = guideRepository;
            _tourService = tourService;
            _payOSService = payOSService;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, string? BookingId, string? PaymentUrl)> CreateCustomBookingAsync(
            string travelerId, string guideId, DateTime date, int guests, string? notes)
        {
            string packageId = "00000000-0000-0000-0000-000000000000";
            
            var existingBookings = await _bookingRepository.GetBookingsByTravelerAsync(travelerId);
            bool hasDuplicate = existingBookings.Any(b => 
                b.Status >= 0 && b.Status < 2 && 
                b.ExperiencePackageId == packageId && 
                b.GuideProfileId == guideId && 
                b.BookingDate.Date == date.Date);
                
            if (hasDuplicate)
            {
                return (false, "You already have an active booking for this custom tour on this date.", null, null);
            }

            decimal basePrice = 500_000m * guests;
            var platformFee = Math.Round(basePrice * 0.15m, 0);
            var guideEarnings = basePrice - platformFee;
            long orderCode = long.Parse(DateTimeOffset.UtcNow.ToString("yyMMddHHmmssfff"));

            var booking = new BookingEntity
            {
                TravelerId = travelerId,
                GuideProfileId = guideId,
                ExperiencePackageId = packageId,
                BookingDate = date,
                StartTime = date.Date.AddHours(9),
                GuestCount = guests,
                TotalAmount = basePrice,
                PlatformFee = platformFee,
                GuideEarnings = guideEarnings,
                TravelerNotes = notes,
                PaymentReference = orderCode.ToString(),
                Status = -1
            };

            var createdBooking = await _bookingRepository.CreateBookingAsync(booking);
            int depositAmount = (int)Math.Round(createdBooking.TotalAmount * 0.3m);
            string paymentUrl = await _payOSService.CreatePaymentLink(createdBooking, orderCode, depositAmount);

            return (true, "Success", createdBooking.Id, paymentUrl);
        }

        public async Task<(bool Success, string Message, string? BookingId, string? PaymentUrl)> CreateTourBookingAsync(
            string travelerId, string guideId, string packageId, DateTime date, int guests)
        {
            var selectedPackage = await _tourService.GetTourByIdAsync(packageId);
            decimal basePrice;
            
            if (selectedPackage != null)
            {
                guideId = selectedPackage.GuideProfileId ?? guideId;
                if (selectedPackage.PricePerSession > 0)
                    basePrice = selectedPackage.PricePerSession;
                else if (selectedPackage.PricePerPerson.HasValue && selectedPackage.PricePerPerson > 0)
                    basePrice = selectedPackage.PricePerPerson.Value * guests;
                else
                    basePrice = 500_000m * guests;
            }
            else
            {
                basePrice = 500_000m * guests;
                packageId = "00000000-0000-0000-0000-000000000000";
            }

            var platformFee = Math.Round(basePrice * 0.15m, 0);
            var guideEarnings = basePrice - platformFee;
            var targetDate = date == default ? DateTime.UtcNow.AddDays(7).Date : date.Date;

            var existingBookings = await _bookingRepository.GetBookingsByTravelerAsync(travelerId);
            bool hasDuplicate = existingBookings.Any(b => 
                b.Status >= -1 && b.Status < 2 && 
                b.ExperiencePackageId == packageId && 
                b.GuideProfileId == guideId && 
                b.BookingDate.Date == targetDate);
                
            if (hasDuplicate)
            {
                return (false, "You already have an active booking for this tour on this date.", null, null);
            }

            long orderCode = long.Parse(DateTimeOffset.UtcNow.ToString("yyMMddHHmmssfff"));

            var booking = new BookingEntity
            {
                TravelerId = travelerId,
                GuideProfileId = guideId,
                ExperiencePackageId = packageId,
                BookingDate = targetDate,
                StartTime = targetDate.AddHours(9),
                GuestCount = guests,
                TotalAmount = basePrice,
                PlatformFee = platformFee,
                GuideEarnings = guideEarnings,
                PaymentReference = orderCode.ToString(),
                Status = -1
            };

            var createdBooking = await _bookingRepository.CreateBookingAsync(booking);
            int depositAmount = Convert.ToInt32(basePrice * 0.3m);
            string paymentUrl = await _payOSService.CreatePaymentLink(createdBooking, orderCode, depositAmount);

            return (true, "Success", createdBooking.Id, paymentUrl);
        }

        public async Task<(bool Success, string Message, string? BookingId, string? PaymentUrl)> CreateBookingFromOfferAsync(
            string travelerId, TripRequestEntity request, TripOfferEntity offer)
        {
            string packageId = "00000000-0000-0000-0000-000000000000"; // Custom Tour
            
            // ProposedPrice is the total price for the trip (agreed by Guide)
            decimal basePrice = offer.ProposedPrice;
            var platformFee = Math.Round(basePrice * 0.15m, 0);
            var guideEarnings = basePrice - platformFee;
            long orderCode = long.Parse(DateTimeOffset.UtcNow.ToString("yyMMddHHmmssfff"));

            // Calculate guest count based on TripRequest
            int guests = request.GroupSize > 0 ? request.GroupSize : 1;
            
            // Use StartDate as BookingDate
            var date = request.StartDate;

            var booking = new BookingEntity
            {
                TravelerId = travelerId,
                GuideProfileId = offer.GuideProfileId,
                ExperiencePackageId = packageId,
                BookingDate = date,
                StartTime = date.Date.AddHours(9), // default start time
                GuestCount = guests,
                TotalAmount = basePrice,
                PlatformFee = platformFee,
                GuideEarnings = guideEarnings,
                TravelerNotes = $"Accepted Offer for Trip Request: {request.Destination}",
                PaymentReference = orderCode.ToString(),
                Status = -1
            };

            var createdBooking = await _bookingRepository.CreateBookingAsync(booking);
            
            // Generate payOS link for 30% deposit
            int depositAmount = (int)Math.Round(createdBooking.TotalAmount * 0.3m);
            string paymentUrl = await _payOSService.CreatePaymentLink(createdBooking, orderCode, depositAmount);

            return (true, "Success", createdBooking.Id, paymentUrl);
        }

        public async Task<(bool Success, string Message, int? NewStatus)> ProcessPaymentCallbackAsync(
            string bookingId, string status, string cancel, string orderCode)
        {
            var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
            if (booking == null) return (false, "Booking not found", null);

            if (cancel == "true" || status != "PAID")
            {
                await _bookingRepository.UpdateBookingStatusAsync(booking.Id, 3);
                return (false, "Payment was cancelled.", 3);
            }
            
            if (status == "PAID")
            {
                if (booking.Status == -1) // 30% Deposit paid
                {
                    booking.AmountPaid = booking.TotalAmount * 0.3m;
                    booking.Status = 0; // Pending Guide Approval
                    await _bookingRepository.UpdateBookingAsync(booking);
                    
                    await SendPaymentNotificationsAsync(booking, orderCode, true);
                    return (true, "Deposit (30%) paid successfully! Your booking is now pending guide approval.", 0);
                }
                else if (booking.Status == 1 && booking.AmountPaid < booking.TotalAmount) // 70% Final payment
                {
                    booking.AmountPaid = booking.TotalAmount;
                    await _bookingRepository.UpdateBookingAsync(booking);
                    await _bookingRepository.UpdateBookingStatusAsync(booking.Id, 0); // Not sure why it was set to 0 in original code for 70%, but keeping logic consistent
                    
                    await SendPaymentNotificationsAsync(booking, orderCode, false);
                    return (true, "Final payment (70%) successful! You are all set for the tour.", 0);
                }
            }
            
            return (false, "Unknown payment state.", booking.Status);
        }

        private async Task SendPaymentNotificationsAsync(BookingEntity booking, string orderCode, bool isDeposit)
        {
            await _notifications.SendAsync(
                booking.TravelerId,
                NotificationTypes.PaymentSucceeded,
                "Payment successful",
                $"Payment for booking {booking.Id} was received. The guide can now review it.",
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
                        $"Booking {booking.Id} is ready for your review.",
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

            long orderCode = long.Parse(DateTimeOffset.UtcNow.ToString("yyMMddHHmmssfff"));
            booking.PaymentReference = orderCode.ToString();
            await _bookingRepository.UpdateBookingAsync(booking);

            int amountToPay = booking.Status == -1 
                ? Convert.ToInt32(booking.TotalAmount * 0.3m) 
                : Convert.ToInt32(booking.TotalAmount * 0.7m);

            string paymentUrl = await _payOSService.CreatePaymentLink(booking, orderCode, amountToPay);
            return (true, "Success", paymentUrl);
        }

        public async Task TryAutoCompleteBookingsAsync(IEnumerable<BookingEntity> bookings)
        {
            var today = DateTime.UtcNow.Date;
            foreach (var b in bookings)
            {
                if (b.Status == 1 && b.AmountPaid >= b.TotalAmount && b.BookingDate.Date < today)
                {
                    try
                    {
                        await _bookingRepository.UpdateBookingStatusAsync(b.Id, 2);
                        b.Status = 2;
                        _logger.LogInformation("[AutoComplete] Booking {BookingId} → Completed on read", b.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[AutoComplete] Failed for booking {BookingId}", b.Id);
                    }
                }
            }
        }
    }
}
