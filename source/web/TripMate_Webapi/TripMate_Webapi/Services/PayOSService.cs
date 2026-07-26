using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
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

        public async Task<string> CreatePaymentLink(
            BookingEntity booking,
            PaymentEntity payment,
            long orderCode,
            int amount)
        {
            EnsureConfigured();

            var returnUrl = AddQueryValues(
                _config["PayOS:ReturnUrl"] ?? throw new InvalidOperationException("PayOS return URL is not configured."),
                booking.Id,
                payment.Id);
            var cancelUrl = AddQueryValues(
                _config["PayOS:CancelUrl"] ?? throw new InvalidOperationException("PayOS cancel URL is not configured."),
                booking.Id,
                payment.Id);

            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");

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

        public async Task<WebhookData> VerifyWebhookAsync(Webhook webhook)
        {
            EnsureConfigured();
            return await _payOS.Webhooks.VerifyAsync(webhook);
        }

        public async Task<PaymentLink> GetPaymentLinkAsync(string orderCode)
        {
            EnsureConfigured();
            if (string.IsNullOrWhiteSpace(orderCode))
                throw new ArgumentException("PayOS order code is required.", nameof(orderCode));

            return await _payOS.PaymentRequests.GetAsync(orderCode);
        }

        private void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_config["PayOS:ClientId"]) ||
                string.IsNullOrWhiteSpace(_config["PayOS:ApiKey"]) ||
                string.IsNullOrWhiteSpace(_config["PayOS:ChecksumKey"]))
            {
                throw new InvalidOperationException("PayOS credentials are not configured.");
            }
        }

        private static string AddQueryValues(string baseUrl, string bookingId, string paymentId)
        {
            var separator = baseUrl.Contains('?') ? "&" : "?";
            return $"{baseUrl}{separator}bookingId={Uri.EscapeDataString(bookingId)}&paymentId={Uri.EscapeDataString(paymentId)}";
        }
    }
}
