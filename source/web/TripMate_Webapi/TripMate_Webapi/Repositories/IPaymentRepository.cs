using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories;

public interface IPaymentRepository
{
    Task<PaymentEntity> CreateAsync(PaymentEntity payment);
    Task UpdateCheckoutAsync(string paymentId, string checkoutUrl);
    Task MarkFailedAsync(string paymentId, string reason);
    Task MarkCancelledAsync(string paymentId);
    Task<PaymentEntity?> GetByOrderCodeAsync(string orderCode);
    Task<PaymentEntity?> GetLatestForBookingAsync(string bookingId, string payerId);
    Task<PaymentWebhookResult> ProcessVerifiedPayOSWebhookAsync(
        string orderCode,
        decimal amount,
        string currency,
        string providerTransactionId,
        DateTime paidAtUtc,
        object payload);
}

public sealed record PaymentWebhookResult(
    bool KnownPayment,
    bool Processed,
    bool AlreadyProcessed,
    string? Reason,
    string? BookingId,
    string? PayerId,
    string? GuideProfileId,
    string? InstallmentType,
    decimal Amount,
    int? BookingStatus);
