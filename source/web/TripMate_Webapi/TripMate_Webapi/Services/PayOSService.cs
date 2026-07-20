using PayOS;
using PayOS.Models.V2.PaymentRequests;
using System.Threading.Tasks;
using TripMate_Webapi.Entities;
using Microsoft.Extensions.Configuration;
using System;

namespace TripMate_WebAPI.Services
{
    public class PayOSService : IPayOSService
    {
        private readonly PayOSClient _payOS;
        private readonly IConfiguration _config;

        public PayOSService(IConfiguration config)
        {
            _config = config;
            _payOS = new PayOSClient(
                _config["PayOS:ClientId"] ?? "",
                _config["PayOS:ApiKey"] ?? "",
                _config["PayOS:ChecksumKey"] ?? ""
            );
        }

        public async Task<string> CreatePaymentLink(BookingEntity booking, long orderCode, int amount)
        {
            var returnUrl = _config["PayOS:ReturnUrl"] + "?bookingId=" + booking.Id;
            var cancelUrl = _config["PayOS:CancelUrl"] + "&bookingId=" + booking.Id;

            if (amount <= 0) amount = 10000; // Fallback minimum for testing

            var paymentData = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = amount,
                Description = "TripMate Booking",
                CancelUrl = cancelUrl,
                ReturnUrl = returnUrl
            };

            var createPayment = await _payOS.PaymentRequests.CreateAsync(paymentData);
            return createPayment.CheckoutUrl;
        }
    }
}
