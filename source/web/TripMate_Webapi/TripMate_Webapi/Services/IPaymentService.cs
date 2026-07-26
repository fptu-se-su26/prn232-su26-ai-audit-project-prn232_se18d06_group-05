using PayOS.Models.Webhooks;
using TripMate_Webapi.Entities;
using TripMate_Webapi.Repositories;

namespace TripMate_WebAPI.Services;

public interface IPaymentService
{
    Task<PaymentLinkResult> CreateRequiredPaymentAsync(BookingEntity booking);
    Task<PaymentReturnStatus> GetReturnStatusAsync(string travelerId, string bookingId, string? orderCode);
    Task<PaymentWebhookResult> HandlePayOSWebhookAsync(Webhook webhook);
}

public sealed record PaymentLinkResult(string PaymentId, string OrderCode, string CheckoutUrl, string InstallmentType, decimal Amount);

public sealed record PaymentReturnStatus(bool Found, string State, string Message, int BookingStatus);
