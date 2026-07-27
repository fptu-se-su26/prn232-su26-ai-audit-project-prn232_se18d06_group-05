using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using TripMate_WebAPI.DTOs;
using TripMate_Webapi.Repositories;

namespace TripMate_Webapi.Services;

public interface IGuideEarningsService
{
    Task<GuideEarningsReportViewModel> GetReportAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

public sealed class GuideEarningsService : IGuideEarningsService
{
    private readonly HttpClient _http;
    private readonly IGuideRepository _guideRepository;
    private readonly string _supabaseUrl;
    private readonly string _serviceRoleKey;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public GuideEarningsService(
        HttpClient http,
        IGuideRepository guideRepository,
        IConfiguration configuration)
    {
        _http = http;
        _guideRepository = guideRepository;
        _supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Supabase URL is not configured.");
        _serviceRoleKey = configuration["Supabase:ServiceRoleKey"]
            ?? throw new InvalidOperationException("Supabase service key is not configured.");
    }

    public async Task<GuideEarningsReportViewModel> GetReportAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var guide = await _guideRepository.GetGuideProfileByUserIdAsync(userId);
        if (guide == null)
        {
            return new GuideEarningsReportViewModel
            {
                ErrorMessage = "Không tìm thấy hồ sơ hướng dẫn viên cho tài khoản này."
            };
        }

        var bookingsTask = GetReportBookingsAsync(guide.Id, cancellationToken);
        var ledgerTask = GetEarningLedgerEntriesAsync(userId, cancellationToken);
        await Task.WhenAll(bookingsTask, ledgerTask);

        var bookings = await bookingsTask;
        var ledgerEntries = await ledgerTask;
        var reportBookingIds = bookings
            .Select(booking => booking.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var relevantLedgerEntries = ledgerEntries
            .Where(entry => entry.BookingId != null && reportBookingIds.Contains(entry.BookingId))
            .ToList();

        var latestLedgerByBooking = relevantLedgerEntries
            .GroupBy(entry => entry.BookingId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(entry => entry.CreatedAt).First(),
                StringComparer.OrdinalIgnoreCase);
        var payoutEntries = latestLedgerByBooking.Values.ToList();

        var transactions = bookings.Select(booking =>
        {
            latestLedgerByBooking.TryGetValue(booking.Id, out var ledgerEntry);

            var status = ledgerEntry != null
                ? "Received"
                : booking.Status == 2
                    ? "AwaitingPayout"
                    : "Held";

            var completedAt = booking.Status == 2 ? (DateTime?)booking.UpdatedAt : null;
            var eventDate = ledgerEntry?.CreatedAt
                ?? completedAt
                ?? booking.BookingDate;

            return new GuideEarningsTransactionViewModel
            {
                Id = booking.Id,
                BookingDate = booking.BookingDate,
                EventDate = eventDate,
                CompletedAt = completedAt,
                ReleaseDate = ledgerEntry?.CreatedAt,
                TravelerName = booking.Traveler?.FullName ?? "Unknown traveler",
                TourName = booking.ExperiencePackage?.Title ?? "Unknown tour",
                TotalAmount = booking.TotalAmount,
                PlatformFee = booking.PlatformFee,
                NetEarnings = ledgerEntry?.Amount ?? booking.GuideEarnings,
                BookingStatus = booking.Status,
                Status = status,
                PaymentReference = booking.PaymentReference,
                LedgerReference = ledgerEntry?.Id
            };
        })
        .OrderByDescending(transaction => transaction.EventDate)
        .ToList();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStart = monthStart.AddMonths(1);
        var model = new GuideEarningsReportViewModel
        {
            AverageRating = guide.AverageRating ?? 0,
            ReviewsCount = guide.TotalReviews ?? 0,
            GeneratedAtUtc = now,
            Transactions = transactions,
            Received = transactions
                .Where(transaction =>
                    transaction.Status == "Received" &&
                    IsWithin(transaction.ReleaseDate, monthStart, nextMonthStart))
                .Sum(transaction => transaction.NetEarnings),
            Pending = transactions
                .Where(transaction =>
                    transaction.Status != "Received" &&
                    IsWithin(transaction.EventDate, monthStart, nextMonthStart))
                .Sum(transaction => transaction.NetEarnings),
            CompletedTours = transactions.Count(transaction =>
                transaction.BookingStatus == 2 &&
                IsWithin(transaction.BookingDate, monthStart, nextMonthStart))
        };

        for (var offset = -11; offset <= 0; offset++)
        {
            var chartMonth = monthStart.AddMonths(offset);
            var chartMonthEnd = chartMonth.AddMonths(1);
            model.ChartLabels.Add($"T{chartMonth.Month}/{chartMonth:yy}");
            model.ChartData.Add(payoutEntries
                .Where(entry => IsWithin(entry.CreatedAt, chartMonth, chartMonthEnd))
                .Sum(entry => entry.Amount));
        }

        return model;
    }

    private async Task<List<EarningsBookingRow>> GetReportBookingsAsync(
        string guideProfileId,
        CancellationToken cancellationToken)
    {
        var select = string.Join(',', new[]
        {
            "id",
            "booking_date",
            "total_amount",
            "platform_fee",
            "guide_earnings",
            "status",
            "payment_reference",
            "created_at",
            "updated_at",
            "traveler:traveler_id(full_name)",
            "experience_package:experience_package_id(title)"
        });

        var url = $"{_supabaseUrl}/rest/v1/bookings" +
                  $"?guide_profile_id=eq.{Uri.EscapeDataString(guideProfileId)}" +
                  "&status=in.(1,2)" +
                  "&order=booking_date.desc" +
                  $"&select={select}";

        return await GetAsync<EarningsBookingRow>(url, cancellationToken);
    }

    private async Task<List<EarningLedgerRow>> GetEarningLedgerEntriesAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var url = $"{_supabaseUrl}/rest/v1/ledger_entries" +
                  $"?user_id=eq.{Uri.EscapeDataString(userId)}" +
                  "&type=eq.EARNING" +
                  "&select=id,booking_id,amount,created_at" +
                  "&order=created_at.desc";

        return await GetAsync<EarningLedgerRow>(url, cancellationToken);
    }

    private async Task<List<T>> GetAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("apikey", _serviceRoleKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _http.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to load guide earnings data ({(int)response.StatusCode}).");
        }

        return JsonSerializer.Deserialize<List<T>>(content, JsonOptions) ?? [];
    }

    private static bool IsWithin(DateTime value, DateTime start, DateTime end) =>
        value >= start && value < end;

    private static bool IsWithin(DateTime? value, DateTime start, DateTime end) =>
        value.HasValue && IsWithin(value.Value, start, end);

    private sealed class EarningsBookingRow
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("booking_date")] public DateTime BookingDate { get; set; }
        [JsonPropertyName("total_amount")] public decimal TotalAmount { get; set; }
        [JsonPropertyName("platform_fee")] public decimal PlatformFee { get; set; }
        [JsonPropertyName("guide_earnings")] public decimal GuideEarnings { get; set; }
        [JsonPropertyName("status")] public int Status { get; set; }
        [JsonPropertyName("payment_reference")] public string? PaymentReference { get; set; }
        [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime UpdatedAt { get; set; }
        [JsonPropertyName("traveler")] public EarningsTravelerRow? Traveler { get; set; }
        [JsonPropertyName("experience_package")] public EarningsPackageRow? ExperiencePackage { get; set; }
    }

    private sealed class EarningsTravelerRow
    {
        [JsonPropertyName("full_name")] public string? FullName { get; set; }
    }

    private sealed class EarningsPackageRow
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
    }

    private sealed class EarningLedgerRow
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("booking_id")] public string? BookingId { get; set; }
        [JsonPropertyName("amount")] public decimal Amount { get; set; }
        [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
    }
}
