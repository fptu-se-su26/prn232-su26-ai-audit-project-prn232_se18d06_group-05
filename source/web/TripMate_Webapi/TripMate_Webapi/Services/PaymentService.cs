using System.Globalization;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using TripMate_Webapi.Entities;
using TripMate_Webapi.Repositories;

namespace TripMate_WebAPI.Services;

public sealed class PaymentService : IPaymentService
{
    private static long _lastOrderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private readonly IPaymentRepository _payments;
    private readonly IBookingRepository _bookings;
    private readonly IGuideRepository _guides;
    private readonly IPayOSService _payOS;
    private readonly INotificationService _notifications;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository payments,
        IBookingRepository bookings,
        IGuideRepository guides,
        IPayOSService payOS,
        INotificationService notifications,
        ILogger<PaymentService> logger)
    {
        _payments = payments;
        _bookings = bookings;
        _guides = guides;
        _payOS = payOS;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<PaymentLinkResult> CreateRequiredPaymentAsync(BookingEntity booking)
    {
        var (installmentType, amount) = GetRequiredInstallment(booking);
        if (amount > int.MaxValue)
            throw new InvalidOperationException("The payment amount exceeds the supported PayOS limit.");

        // Do not create multiple PayOS links while the latest attempt is still
        // fresh. This protects travelers from paying the same installment twice
        // when a local/private webhook has not reached the application yet.
        var existing = await _payments.GetLatestForBookingAsync(booking.Id, booking.TravelerId);
        if (CanReusePendingCheckout(existing, installmentType, amount))
        {
            return new PaymentLinkResult(
                existing!.Id,
                existing.ProviderOrderCode!,
                existing.CheckoutUrl!,
                existing.InstallmentType,
                existing.Amount);
        }

        var orderCode = NextOrderCode();
        var payment = new PaymentEntity
        {
            BookingId = booking.Id,
            PayerId = booking.TravelerId,
            Amount = amount,
            Currency = "VND",
            PaymentMethod = "payos",
            InstallmentType = installmentType,
            Status = "pending",
            ProviderOrderCode = orderCode.ToString(CultureInfo.InvariantCulture),
            IdempotencyKey = $"payos:{booking.Id}:{installmentType}:{orderCode}",
            Metadata = new Dictionary<string, object?>
            {
                ["bookingId"] = booking.Id,
                ["installmentType"] = installmentType,
                ["orderCode"] = orderCode
            }
        };

        payment = await _payments.CreateAsync(payment);
        await _bookings.UpdatePaymentReferenceAsync(booking.Id, payment.ProviderOrderCode!);

        try
        {
            var checkoutUrl = await _payOS.CreatePaymentLink(
                booking,
                payment,
                orderCode,
                checked((int)amount));
            await _payments.UpdateCheckoutAsync(payment.Id, checkoutUrl);
            return new PaymentLinkResult(
                payment.Id,
                payment.ProviderOrderCode!,
                checkoutUrl,
                installmentType,
                amount);
        }
        catch (Exception ex)
        {
            await _payments.MarkFailedAsync(payment.Id, ex.Message);
            throw;
        }
    }

    public async Task<PaymentReturnStatus> GetReturnStatusAsync(
        string travelerId,
        string bookingId,
        string? orderCode)
    {
        var booking = await _bookings.GetBookingByIdAsync(bookingId);
        if (booking == null || booking.TravelerId != travelerId)
            return new PaymentReturnStatus(false, "not_found", "Booking not found.", -1);

        var payment = !string.IsNullOrWhiteSpace(orderCode)
            ? await _payments.GetByOrderCodeAsync(orderCode)
            : await _payments.GetLatestForBookingAsync(bookingId, travelerId);

        if (payment == null || payment.BookingId != bookingId || payment.PayerId != travelerId)
        {
            return new PaymentReturnStatus(
                false,
                "pending",
                "We are waiting for payment confirmation. Please refresh your bookings shortly.",
                booking.Status);
        }

        if (string.Equals(payment.Status, "pending", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var reconciliation = await TryReconcilePaidPaymentAsync(payment);
                if (reconciliation is { Processed: true } or { AlreadyProcessed: true })
                {
                    payment = await _payments.GetByOrderCodeAsync(payment.ProviderOrderCode!) ?? payment;
                    booking = await _bookings.GetBookingByIdAsync(bookingId) ?? booking;
                }
            }
            catch (Exception ex)
            {
                // A provider lookup is a resilient fallback for delayed/unreachable
                // webhooks. The callback must still remain usable when PayOS is
                // temporarily unavailable.
                _logger.LogWarning(
                    ex,
                    "Could not reconcile PayOS order {OrderCode} during payment return.",
                    payment.ProviderOrderCode);
            }
        }

        return payment.Status switch
        {
            "succeeded" => new PaymentReturnStatus(true, "succeeded", "Payment confirmed successfully.", booking.Status),
            "failed" => new PaymentReturnStatus(true, "failed", "The payment could not be completed. You can retry from your booking.", booking.Status),
            "cancelled" => new PaymentReturnStatus(true, "cancelled", "Payment was cancelled. Your booking remains available for retry.", booking.Status),
            "expired" => new PaymentReturnStatus(true, "expired", "The payment link expired. You can create a new payment link.", booking.Status),
            _ => new PaymentReturnStatus(true, "pending", "Payment is being verified. Your booking will update automatically.", booking.Status)
        };
    }

    public async Task<PaymentWebhookResult> HandlePayOSWebhookAsync(Webhook webhook)
    {
        var verified = await _payOS.VerifyWebhookAsync(webhook);
        if (!string.Equals(verified.Code, "00", StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentWebhookResult(
                KnownPayment: false,
                Processed: false,
                AlreadyProcessed: false,
                Reason: "non_success_event",
                BookingId: null,
                PayerId: null,
                GuideProfileId: null,
                InstallmentType: null,
                Amount: 0,
                BookingStatus: null);
        }

        if (verified.Amount <= 0 || string.IsNullOrWhiteSpace(verified.Reference))
            throw new InvalidOperationException("The verified PayOS event is missing transaction details.");

        var result = await _payments.ProcessVerifiedPayOSWebhookAsync(
            verified.OrderCode.ToString(CultureInfo.InvariantCulture),
            verified.Amount,
            string.IsNullOrWhiteSpace(verified.Currency) ? "VND" : verified.Currency,
            verified.Reference,
            ParsePayOSDateTime(verified.TransactionDateTime),
            verified);

        if (!result.KnownPayment)
            _logger.LogInformation("Acknowledged a signed PayOS event for an unknown order code {OrderCode}.", verified.OrderCode);
        else if (!result.Processed && !result.AlreadyProcessed)
            _logger.LogWarning("PayOS payment {OrderCode} was not applied: {Reason}.", verified.OrderCode, result.Reason);

        // Notification writes are independently idempotent. Retrying them for
        // an already-processed webhook prevents a transient notification error
        // from permanently losing the Guide alert.
        if (result.Processed || result.AlreadyProcessed)
            await SendSuccessNotificationsAsync(result);

        return result;
    }

    private async Task<PaymentWebhookResult?> TryReconcilePaidPaymentAsync(PaymentEntity payment)
    {
        if (string.IsNullOrWhiteSpace(payment.ProviderOrderCode)) return null;

        var providerPayment = await _payOS.GetPaymentLinkAsync(payment.ProviderOrderCode);
        if (providerPayment.Status != PaymentLinkStatus.Paid) return null;

        if (providerPayment.Amount != decimal.ToInt64(payment.Amount) ||
            providerPayment.AmountPaid < providerPayment.Amount)
        {
            _logger.LogWarning(
                "PayOS order {OrderCode} reported an unexpected amount. Expected {ExpectedAmount}, provider amount {ProviderAmount}, paid {PaidAmount}.",
                payment.ProviderOrderCode,
                payment.Amount,
                providerPayment.Amount,
                providerPayment.AmountPaid);
            return null;
        }

        var transaction = (providerPayment.Transactions ?? [])
            .LastOrDefault(item => !string.IsNullOrWhiteSpace(item.Reference));
        if (transaction == null)
        {
            _logger.LogWarning(
                "Paid PayOS order {OrderCode} did not include a transaction reference.",
                payment.ProviderOrderCode);
            return null;
        }

        // This uses data fetched directly through the authenticated PayOS API.
        // It intentionally shares the same atomic/idempotent database transition
        // as the signed webhook and never trusts the browser callback query.
        var result = await _payments.ProcessVerifiedPayOSWebhookAsync(
            payment.ProviderOrderCode,
            payment.Amount,
            payment.Currency,
            transaction.Reference,
            ParsePayOSDateTime(transaction.TransactionDateTime),
            providerPayment);

        if (result.Processed || result.AlreadyProcessed)
            await SendSuccessNotificationsAsync(result);

        return result;
    }

    private async Task SendSuccessNotificationsAsync(PaymentWebhookResult result)
    {
        if (string.IsNullOrWhiteSpace(result.BookingId) || string.IsNullOrWhiteSpace(result.PayerId))
            return;

        var isDeposit = string.Equals(result.InstallmentType, "deposit", StringComparison.OrdinalIgnoreCase);
        var title = isDeposit ? "Deposit payment successful" : "Final payment successful";
        var message = isDeposit
            ? $"Your deposit for booking {result.BookingId} was received. The guide can now review it."
            : $"Your final payment for booking {result.BookingId} was received.";

        await _notifications.SendAsync(
            result.PayerId,
            NotificationTypes.PaymentSucceeded,
            title,
            message,
            new { bookingId = result.BookingId, amount = result.Amount, installmentType = result.InstallmentType },
            $"/Traveler/BookingDetails/{result.BookingId}",
            $"payment-succeeded:{result.BookingId}:{result.InstallmentType}",
            sendEmail: true);

        if (!isDeposit || string.IsNullOrWhiteSpace(result.GuideProfileId)) return;

        var guide = await _guides.GetGuideByProfileIdAsync(result.GuideProfileId);
        if (string.IsNullOrWhiteSpace(guide?.UserId)) return;

        await _notifications.SendAsync(
            guide.UserId,
            NotificationTypes.BookingAwaitingGuide,
            "New paid booking awaiting your response",
            $"Booking {result.BookingId} is ready for your review.",
            new { bookingId = result.BookingId },
            "/Guide/Bookings",
            $"booking-awaiting-guide:{result.BookingId}",
            sendEmail: true);
    }

    private static (string InstallmentType, decimal Amount) GetRequiredInstallment(BookingEntity booking)
    {
        if (booking.Status == -1 && booking.AmountPaid <= 0)
        {
            var deposit = TourPricingCalculator.FromAgreedTotal(booking.TotalAmount).DepositAmount;
            return ("deposit", deposit);
        }

        if (booking.Status == 1 && booking.AmountPaid < booking.TotalAmount)
            return ("balance", Math.Round(booking.TotalAmount - booking.AmountPaid, 0, MidpointRounding.AwayFromZero));

        throw new InvalidOperationException("This booking does not require payment.");
    }

    private static bool CanReusePendingCheckout(
        PaymentEntity? payment,
        string installmentType,
        decimal amount)
    {
        if (payment == null ||
            !string.Equals(payment.Status, "pending", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(payment.InstallmentType, installmentType, StringComparison.OrdinalIgnoreCase) ||
            payment.Amount != amount ||
            string.IsNullOrWhiteSpace(payment.ProviderOrderCode) ||
            string.IsNullOrWhiteSpace(payment.CheckoutUrl))
        {
            return false;
        }

        // PayOS links are short-lived. Reusing only a recent attempt prevents a
        // duplicate charge without trapping the traveler on an old link.
        return payment.CreatedAt >= DateTime.UtcNow.AddMinutes(-15);
    }

    private static long NextOrderCode()
    {
        while (true)
        {
            var current = Interlocked.Read(ref _lastOrderCode);
            var next = Math.Max(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), current + 1);
            if (Interlocked.CompareExchange(ref _lastOrderCode, next, current) == current)
                return next;
        }
    }

    private static DateTime ParsePayOSDateTime(string? value)
    {
        if (!DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var localTime))
        {
            return DateTime.UtcNow;
        }

        if (localTime.Kind != DateTimeKind.Unspecified)
            return localTime.ToUniversalTime();

        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");
            return TimeZoneInfo.ConvertTimeToUtc(localTime, zone);
        }
        catch (TimeZoneNotFoundException)
        {
            _ = localTime;
            return DateTime.SpecifyKind(localTime, DateTimeKind.Utc);
        }
    }
}
