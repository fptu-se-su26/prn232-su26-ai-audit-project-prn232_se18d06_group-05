namespace TripMate_WebAPI.DTOs.Notification;

/// <summary>Canonical, stable notification type values persisted in the database.</summary>
public static class NotificationTypes
{
    public const string BookingAwaitingGuide = "booking.awaiting_guide";
    public const string BookingConfirmed = "booking.confirmed";
    public const string BookingDeclined = "booking.declined";
    public const string BookingCancelled = "booking.cancelled";
    public const string BookingReminder = "booking.reminder";
    public const string BookingCompleted = "booking.completed";
    public const string PaymentSucceeded = "payment.succeeded";
    public const string RefundProcessed = "refund.processed";
    public const string PayoutReleased = "payout.released";
    public const string PayoutFailed = "payout.failed";
    public const string ReviewReceived = "review.received";
    public const string ReviewRequested = "review.requested";
    public const string GuideApplicationSubmitted = "guide.application_submitted";
    public const string GuideApplicationApproved = "guide.application_approved";
    public const string GuideApplicationRejected = "guide.application_rejected";
    public const string CancellationReviewRequired = "cancellation.review_required";
    public const string SupportTicketUpdated = "support.ticket_updated";
    public const string VoucherIssued = "voucher.issued";
    public const string SystemAnnouncement = "system.announcement";

    public static readonly ISet<string> AdminSendable = new HashSet<string>(StringComparer.Ordinal)
    {
        SupportTicketUpdated,
        SystemAnnouncement
    };
}
