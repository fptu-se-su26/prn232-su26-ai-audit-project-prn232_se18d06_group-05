namespace TripMate_WebAPI.DTOs;

public sealed class GuideEarningsReportViewModel
{
    public decimal Received { get; set; }
    public decimal Pending { get; set; }
    public int CompletedTours { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewsCount { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
    public List<string> ChartLabels { get; set; } = [];
    public List<decimal> ChartData { get; set; } = [];
    public List<GuideEarningsTransactionViewModel> Transactions { get; set; } = [];
}

public sealed class GuideEarningsTransactionViewModel
{
    public string Id { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public DateTime EventDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string TravelerName { get; set; } = "Unknown traveler";
    public string TourName { get; set; } = "Unknown tour";
    public decimal TotalAmount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal NetEarnings { get; set; }
    public int BookingStatus { get; set; }
    public string Status { get; set; } = "Held";
    public string? PaymentReference { get; set; }
    public string? LedgerReference { get; set; }
}
