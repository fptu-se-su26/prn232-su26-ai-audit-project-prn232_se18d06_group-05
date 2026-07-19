using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TripMate_WebAPI.Services;

public sealed class BookingReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingReminderWorker> _logger;

    public BookingReminderWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BookingReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("Notifications:RemindersEnabled", true)) return;

        // Let startup complete before the first external query.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<BookingReminderService>()
                    .SendDueRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Booking reminder scan failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

public sealed class BookingReminderService
{
    private readonly HttpClient _http;
    private readonly INotificationService _notifications;
    private readonly string _supabaseUrl;
    private readonly string _serviceRoleKey;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public BookingReminderService(HttpClient http, INotificationService notifications, IConfiguration configuration)
    {
        _http = http;
        _notifications = notifications;
        _supabaseUrl = configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase URL not configured");
        _serviceRoleKey = configuration["Supabase:ServiceRoleKey"] ?? throw new InvalidOperationException("Supabase service key not configured");
    }

    public async Task SendDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        await NotifyExpiredPendingPaymentsAsync(cancellationToken);

        var now = DateTimeOffset.Now;
        var firstDate = now.Date.ToString("yyyy-MM-dd");
        var lastDate = now.AddHours(25).Date.ToString("yyyy-MM-dd");
        var url = $"{_supabaseUrl}/rest/v1/bookings?status=eq.1&booking_date=gte.{firstDate}&booking_date=lte.{lastDate}" +
                  "&select=id,traveler_id,guide_profile_id,booking_date,start_time,experience_packages(title)";
        using var request = BuildRequest(HttpMethod.Get, url);
        using var response = await _http.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        var bookings = JsonSerializer.Deserialize<List<ReminderBooking>>(content, JsonOptions) ?? [];

        foreach (var booking in bookings)
        {
            if (!TryGetStart(booking, out var startsAt)) continue;
            var hours = (startsAt - now).TotalHours;
            var window = hours is >= 23.5 and <= 24.5 ? "24h"
                : hours is >= 1.5 and <= 2.5 ? "2h"
                : null;
            if (window is null || string.IsNullOrWhiteSpace(booking.Id)) continue;

            var guideUserId = await GetGuideUserIdAsync(booking.GuideProfileId, cancellationToken);
            var title = window == "24h" ? "Your trip is tomorrow" : "Your trip starts in about 2 hours";
            var tour = booking.Package?.Title ?? "your TripMate tour";
            var data = new { bookingId = booking.Id, startsAt, reminder = window };

            await _notifications.SendAsync(
                booking.TravelerId ?? string.Empty,
                NotificationTypes.BookingReminder,
                title,
                $"{tour} starts at {startsAt:g}.",
                data,
                $"/Traveler/BookingDetails/{booking.Id}",
                $"booking-reminder:{booking.Id}:{window}");

            if (!string.IsNullOrWhiteSpace(guideUserId))
            {
                await _notifications.SendAsync(
                    guideUserId,
                    NotificationTypes.BookingReminder,
                    title,
                    $"{tour} starts at {startsAt:g}.",
                    data,
                    "/Guide/Bookings",
                    $"booking-reminder:{booking.Id}:{window}:guide");
            }
        }
    }

    private static bool TryGetStart(ReminderBooking booking, out DateTimeOffset startsAt)
    {
        if (DateTimeOffset.TryParse(booking.StartTime, out startsAt) && booking.StartTime?.Contains('T') == true)
            return true;
        if (!DateOnly.TryParse(booking.BookingDate, out var date) ||
            !TimeOnly.TryParse(booking.StartTime, out var time))
        {
            startsAt = default;
            return false;
        }

        startsAt = new DateTimeOffset(date.ToDateTime(time), TimeZoneInfo.Local.GetUtcOffset(date.ToDateTime(time)));
        return true;
    }

    private async Task NotifyExpiredPendingPaymentsAsync(CancellationToken cancellationToken)
    {
        var cutoff = Uri.EscapeDataString(DateTime.UtcNow.AddMinutes(-30).ToString("O"));
        var url = $"{_supabaseUrl}/rest/v1/bookings?status=eq.-1&created_at=lt.{cutoff}&select=id,traveler_id&limit=1000";
        using var request = BuildRequest(HttpMethod.Get, url);
        using var response = await _http.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        var expired = JsonSerializer.Deserialize<List<ReminderBooking>>(content, JsonOptions) ?? [];

        foreach (var booking in expired.Where(x => !string.IsNullOrWhiteSpace(x.Id)))
        {
            await _notifications.SendAsync(
                booking.TravelerId ?? string.Empty,
                NotificationTypes.PaymentFailed,
                "Payment expired",
                $"Payment for booking {booking.Id} was not completed within 30 minutes.",
                new { bookingId = booking.Id, reason = "expired" },
                $"/Traveler/BookingDetails/{booking.Id}",
                $"payment-failed:{booking.Id}",
                sendEmail: true);
        }
    }

    private async Task<string?> GetGuideUserIdAsync(string? guideProfileId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(guideProfileId)) return null;
        var url = $"{_supabaseUrl}/rest/v1/guide_profiles?id=eq.{Uri.EscapeDataString(guideProfileId)}&select=user_id&limit=1";
        using var request = BuildRequest(HttpMethod.Get, url);
        using var response = await _http.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<List<GuideUser>>(content, JsonOptions)?.FirstOrDefault()?.UserId;
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("apikey", _serviceRoleKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
        return request;
    }

    private sealed class ReminderBooking
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("traveler_id")] public string? TravelerId { get; set; }
        [JsonPropertyName("guide_profile_id")] public string? GuideProfileId { get; set; }
        [JsonPropertyName("booking_date")] public string? BookingDate { get; set; }
        [JsonPropertyName("start_time")] public string? StartTime { get; set; }
        [JsonPropertyName("experience_packages")] public ReminderPackage? Package { get; set; }
    }

    private sealed class ReminderPackage
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
    }

    private sealed class GuideUser
    {
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
    }
}
