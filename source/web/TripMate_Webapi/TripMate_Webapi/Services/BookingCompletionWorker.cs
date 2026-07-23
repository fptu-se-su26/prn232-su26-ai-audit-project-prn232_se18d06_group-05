using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TripMate_WebAPI.Services;

/// <summary>
/// Background service that periodically scans for fully-paid bookings whose
/// booking date has passed and automatically transitions them to Completed (status=2).
/// 
/// Transition rules:
///   Status 1 (Confirmed) + AmountPaid >= TotalAmount + BookingDate < today → Status 2 (Completed)
/// 
/// Runs every 15 minutes. Also sends a notification to the traveler inviting them to review.
/// </summary>
public sealed class BookingCompletionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingCompletionWorker> _logger;

    public BookingCompletionWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingCompletionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let startup complete before the first query.
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<BookingCompletionService>()
                    .CompleteEligibleBookingsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BookingCompletion] Scan failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

/// <summary>
/// Service that queries Supabase for bookings eligible for auto-completion
/// and updates their status from 1 (Confirmed) to 2 (Completed).
/// </summary>
public sealed class BookingCompletionService
{
    private readonly HttpClient _http;
    private readonly INotificationService _notifications;
    private readonly ILogger<BookingCompletionService> _logger;
    private readonly string _supabaseUrl;
    private readonly string _serviceRoleKey;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public BookingCompletionService(
        HttpClient http,
        INotificationService notifications,
        IConfiguration configuration,
        ILogger<BookingCompletionService> logger)
    {
        _http = http;
        _notifications = notifications;
        _logger = logger;
        _supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Supabase URL not configured");
        _serviceRoleKey = configuration["Supabase:ServiceRoleKey"]
            ?? throw new InvalidOperationException("Supabase service key not configured");
    }

    public async Task CompleteEligibleBookingsAsync(CancellationToken cancellationToken = default)
    {
        // Query all bookings that are Confirmed (status=1) with booking_date before today
        var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        var url = $"{_supabaseUrl}/rest/v1/bookings" +
                  $"?status=eq.1&booking_date=lt.{today}" +
                  "&select=id,traveler_id,guide_profile_id,booking_date,total_amount,amount_paid," +
                  "experience_packages(title)";

        using var request = BuildRequest(HttpMethod.Get, url);
        using var response = await _http.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[BookingCompletion] Failed to fetch bookings: {Error}", content);
            return;
        }

        var bookings = JsonSerializer.Deserialize<List<CompletionBooking>>(content, JsonOptions) ?? [];
        
        // Filter only fully-paid bookings
        var eligibleBookings = bookings
            .Where(b => b.AmountPaid >= b.TotalAmount && !string.IsNullOrWhiteSpace(b.Id))
            .ToList();

        if (eligibleBookings.Count == 0) return;

        _logger.LogInformation("[BookingCompletion] Found {Count} bookings eligible for auto-completion",
            eligibleBookings.Count);

        foreach (var booking in eligibleBookings)
        {
            try
            {
                await UpdateBookingStatusAsync(booking.Id!, 2, cancellationToken);
                _logger.LogInformation("[BookingCompletion] Booking {BookingId} → Completed", booking.Id);

                // Notify traveler that the trip is completed and they can leave a review
                var tourTitle = booking.Package?.Title ?? "your TripMate tour";
                await _notifications.SendAsync(
                    booking.TravelerId ?? string.Empty,
                    NotificationTypes.BookingCompleted,
                    "Trip completed! How was it?",
                    $"Your trip \"{tourTitle}\" is now marked as completed. Leave a review to help future travelers!",
                    new { bookingId = booking.Id },
                    $"/Traveler/Review/{booking.Id}",
                    $"booking-completed:{booking.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BookingCompletion] Failed to complete booking {BookingId}", booking.Id);
            }
        }
    }

    private async Task UpdateBookingStatusAsync(string bookingId, int status, CancellationToken cancellationToken)
    {
        var url = $"{_supabaseUrl}/rest/v1/bookings?id=eq.{Uri.EscapeDataString(bookingId)}";
        var body = JsonSerializer.Serialize(new
        {
            status,
            updated_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        });

        using var request = BuildRequest(HttpMethod.Patch, url);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation("Prefer", "return=representation");

        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to update booking {bookingId}: {error}");
        }
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("apikey", _serviceRoleKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
        return request;
    }

    private sealed class CompletionBooking
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("traveler_id")] public string? TravelerId { get; set; }
        [JsonPropertyName("guide_profile_id")] public string? GuideProfileId { get; set; }
        [JsonPropertyName("booking_date")] public string? BookingDate { get; set; }
        [JsonPropertyName("total_amount")] public decimal TotalAmount { get; set; }
        [JsonPropertyName("amount_paid")] public decimal AmountPaid { get; set; }
        [JsonPropertyName("experience_packages")] public CompletionPackage? Package { get; set; }
    }

    private sealed class CompletionPackage
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
    }
}
