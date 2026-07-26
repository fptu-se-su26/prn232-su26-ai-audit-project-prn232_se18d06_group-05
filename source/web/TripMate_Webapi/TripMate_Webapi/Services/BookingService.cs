using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace TripMate_WebAPI.Services;

/// <summary>
/// BookingService — refactored to match database_setup.sql schema.
/// bookings → experience_packages → guide_profiles
/// Status: smallint (0=Pending, 1=Confirmed, 2=Completed, 3=Cancelled)
/// </summary>
public class BookingService
{
    private readonly HttpClient _http;
    private readonly string _supabaseUrl;
    private readonly string _anonKey;
    private readonly INotificationService _notif;
    private readonly TripMate_Webapi.Repositories.IBookingRepository _repo;
    private readonly BookingCreationService _bookingCreation;
    private readonly IPaymentService _payments;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // Status mapping: smallint ↔ string
    private static readonly Dictionary<int, string> StatusMap = new()
    {
        { -1, "AwaitingPayment" }, { 0, "Pending" }, { 1, "Confirmed" }, { 2, "Completed" }, { 3, "Cancelled" }
    };
    private static readonly Dictionary<string, int> StatusReverseMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "awaitingpayment", -1 }, { "pending", 0 }, { "confirmed", 1 }, { "completed", 2 }, { "cancelled", 3 }
    };

    public static string MapStatus(int status) => StatusMap.GetValueOrDefault(status, "pending");
    public static int MapStatus(string status) => StatusReverseMap.GetValueOrDefault(status, 0);

    public BookingService(HttpClient http, IConfiguration config,
        INotificationService notif,
        TripMate_Webapi.Repositories.IBookingRepository repo,
        BookingCreationService bookingCreation,
        IPaymentService payments)
    {
        _http = http;
        _supabaseUrl = config["Supabase:Url"]!;
        _anonKey = config["Supabase:AnonKey"]!;
        _notif = notif;
        _repo = repo;
        _bookingCreation = bookingCreation;
        _payments = payments;
    }

    // ── Create Booking ────────────────────────────────────────────────────────

    public async Task<BookingDto> CreateBookingAsync(
        string travelerId, CreateBookingRequest req, string userToken)
    {
        if (!DateTime.TryParseExact(
                req.BookingDate,
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var bookingDate))
        {
            throw new InvalidOperationException("BookingDate must use yyyy-MM-dd format.");
        }

        var entity = await _bookingCreation.CreatePackageBookingAsync(
            travelerId,
            req.ExperiencePackageId,
            bookingDate,
            req.GuestCount,
            req.TravelerNotes,
            req.StartTime,
            userToken);
        var payment = await _payments.CreateRequiredPaymentAsync(entity);
        var package = await GetExperiencePackageAsync(req.ExperiencePackageId);

        return new BookingDto(
            entity.Id,
            entity.TravelerId,
            entity.GuideProfileId,
            entity.ExperiencePackageId,
            package?.Title,
            entity.BookingDate.ToString("yyyy-MM-dd"),
            entity.StartTime.ToString("HH:mm"),
            entity.GuestCount,
            entity.TotalAmount,
            entity.PlatformFee,
            entity.GuideEarnings,
            MapStatus(entity.Status),
            entity.TravelerNotes,
            entity.CreatedAt,
            entity.UpdatedAt,
            payment.CheckoutUrl);
    }

    // ── Get My Bookings ───────────────────────────────────────────────────────

    public async Task<List<BookingDto>> GetMyBookingsAsync(string travelerId)
    {
        var url = $"{_supabaseUrl}/rest/v1/bookings" +
                  $"?traveler_id=eq.{travelerId}" +
                  $"&order=created_at.desc" +
                  $"&select=*,experience_packages(title)";

        var rows = await GetAsync<List<BookingRowJoined>>(url) ?? [];
        return rows.Select(r => MapJoinedToDto(r)).ToList();
    }

    // ── Get Booking By Id ─────────────────────────────────────────────────────

    public async Task<BookingDto?> GetBookingByIdAsync(string bookingId)
    {
        var url = $"{_supabaseUrl}/rest/v1/bookings" +
                  $"?id=eq.{bookingId}" +
                  $"&select=*,experience_packages(title)";

        var rows = await GetAsync<List<BookingRowJoined>>(url);
        var row = rows?.FirstOrDefault();
        return row == null ? null : MapJoinedToDto(row);
    }

    // ── Cancel Booking ────────────────────────────────────────────────────────

    public async Task CancelBookingAsync(string bookingId, string travelerId)
    {
        var bookingEntity = await _repo.GetBookingByIdAsync(bookingId);
        if (bookingEntity == null)
            throw new Exception("Không tìm thấy booking");

        if (bookingEntity.TravelerId != travelerId)
            throw new UnauthorizedAccessException("Bạn không có quyền hủy booking này");

        if (bookingEntity.Status == 2) // Completed
            throw new Exception("Không thể hủy booking đã hoàn thành");

        if (bookingEntity.Status == -1) // Pending Payment
        {
            await _repo.DeleteBookingAsync(bookingId);
        }
        else
        {
            await _repo.UpdateBookingStatusAsync(bookingId, 3); // Cancelled
        }

        var guideUserId = await GetGuideUserIdAsync(bookingEntity.GuideProfileId);
        var tripName = await _notif.GetTripNameAsync(bookingId);
        var tripDate = NotificationTextFormatter.FormatTripDate(bookingEntity.BookingDate);
        var data = new { bookingId, cancelledBy = "traveler" };
        if (!string.IsNullOrWhiteSpace(guideUserId))
        {
            await _notif.SendAsync(
                guideUserId,
                NotificationTypes.BookingCancelled,
                "Booking cancelled by traveler",
                $"The traveler cancelled \"{tripName}\", scheduled for {tripDate}.",
                data,
                "/Guide/Bookings",
                $"booking-cancelled:{bookingId}:guide",
                sendEmail: true);
        }

        await _notif.SendAsync(
            travelerId,
            NotificationTypes.BookingCancelled,
            "Booking cancelled",
            $"Your cancellation for \"{tripName}\", scheduled for {tripDate}, has been recorded.",
            data,
            $"/Traveler/BookingDetails/{bookingId}",
            $"booking-cancelled:{bookingId}:traveler");

        await _notif.SendToRoleAsync(
            "admin",
            NotificationTypes.CancellationReviewRequired,
            "Cancellation requires review",
            $"\"{tripName}\", scheduled for {tripDate}, was cancelled and may require a refund review.",
            data,
            "/Admin/Moderation",
            $"cancellation-review:{bookingId}");
    }

    // ── Get Guide Bookings ────────────────────────────────────────────────────

    public async Task<List<TripMate_WebAPI.DTOs.Booking.Responses.GuideBookingViewDto>> GetGuideBookingsAsync(string guideProfileId)
    {
        var entities = await _repo.GetBookingsForGuideAsync(guideProfileId);
        var dtos = new List<TripMate_WebAPI.DTOs.Booking.Responses.GuideBookingViewDto>();

        foreach (var b in entities)
        {
            var responseDeadlineUtc = BookingResponsePolicy.GetDeadlineUtc(b.CreatedAt);
            var remaining = responseDeadlineUtc - DateTime.UtcNow;
            var isExpired = b.Status == 0 && remaining <= TimeSpan.Zero;
            var secondsRemaining = b.Status == 0 && !isExpired
                ? (int)Math.Ceiling(remaining.TotalSeconds)
                : 0;
            var effectiveStatus = isExpired ? "Expired" : MapStatus(b.Status);

            dtos.Add(new TripMate_WebAPI.DTOs.Booking.Responses.GuideBookingViewDto(
                Id: b.Id,
                TravelerId: b.TravelerId ?? b.Traveler?.Id ?? "",
                TravelerName: b.Traveler?.FullName ?? "Unknown Traveler",
                TravelerAvatar: b.Traveler?.AvatarUrl ?? "/images/AVATAR.png",
                TravelerRating: b.Traveler?.AverageRating ?? 5.0m,
                TravelerLocation: b.Traveler?.Location ?? "Vietnam",
                TourName: b.ExperiencePackage?.Title ?? "Unknown Tour",
                Date: b.BookingDate.ToString("dd/MM/yyyy"),
                Time: b.StartTime.ToString("HH:mm"),
                Guests: b.GuestCount,
                TotalAmount: b.TotalAmount,
                PlatformFee: b.PlatformFee,
                NetEarnings: b.GuideEarnings,
                Note: b.TravelerNotes,
                Status: effectiveStatus,
                SecondsRemaining: secondsRemaining,
                CreatedAt: b.CreatedAt
            ));
        }

        return dtos;
    }

    // ── Update Guide Booking Status ───────────────────────────────────────────

    public async Task UpdateGuideBookingStatusAsync(string bookingId, string guideProfileId, int newStatus)
    {
        var booking = await _repo.GetBookingByIdAsync(bookingId);
        if (booking == null) throw new Exception("Booking not found.");

        if (booking.GuideProfileId != guideProfileId)
            throw new UnauthorizedAccessException("You are not authorized to update this booking.");

        if (booking.Status != 0)
            throw new Exception("Only pending bookings can be updated.");

        if (newStatus is not 1 and not 3)
            throw new ArgumentOutOfRangeException(nameof(newStatus), "A guide can only accept or reject a pending booking.");

        if (BookingResponsePolicy.IsExpired(booking.Status, booking.CreatedAt, DateTime.UtcNow))
            throw new Exception("The 24-hour response window has expired and this booking can no longer be updated.");

        if (newStatus == 1)
        {
            var requiredDeposit = booking.TotalAmount * 0.30m;
            if (booking.AmountPaid < requiredDeposit)
                throw new Exception("This booking cannot be accepted until the 30% deposit has been received.");
        }

        await _repo.UpdateBookingStatusAsync(bookingId, newStatus);
        var tripName = await _notif.GetTripNameAsync(bookingId);
        var tripDate = NotificationTextFormatter.FormatTripDate(booking.BookingDate);
        
        if (newStatus == 1) // Confirmed
        {
            await _notif.SendAsync(
                booking.TravelerId,
                NotificationTypes.BookingConfirmed,
                "Booking confirmed",
                $"Your guide accepted \"{tripName}\", scheduled for {tripDate}.",
                new { bookingId },
                $"/Traveler/BookingDetails/{bookingId}",
                $"booking-confirmed:{bookingId}",
                sendEmail: true);
        }
        else if (newStatus == 3) // Cancelled
        {
            await _notif.SendAsync(
                booking.TravelerId,
                NotificationTypes.BookingDeclined,
                "Booking declined",
                $"The guide could not accept \"{tripName}\", scheduled for {tripDate}.",
                new { bookingId },
                $"/Traveler/BookingDetails/{bookingId}",
                $"booking-declined:{bookingId}",
                sendEmail: true);
        }
    }

    // ── Get Guide Unavailable Dates ───────────────────────────────────────────

    public async Task<List<GuideAvailabilityDto>> GetGuideAvailabilityAsync(string guideProfileId)
    {
        var url = $"{_supabaseUrl}/rest/v1/guide_availability" +
                  $"?guide_profile_id=eq.{guideProfileId}" +
                  $"&order=unavailable_date.asc";

        var rows = await GetAsync<List<GuideAvailabilityRow>>(url) ?? [];
        return rows.Select(r => new GuideAvailabilityDto(
            Id: r.Id ?? "",
            GuideProfileId: r.GuideProfileId ?? "",
            UnavailableDate: r.UnavailableDate ?? "",
            Reason: r.Reason
        )).ToList();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<ExperiencePackageRow?> GetExperiencePackageAsync(string packageId)
    {
        var url = $"{_supabaseUrl}/rest/v1/experience_packages?id=eq.{packageId}&select=*";
        var rows = await GetAsync<List<ExperiencePackageRow>>(url);
        return rows?.FirstOrDefault();
    }

    private async Task<string?> GetGuideUserIdAsync(string guideProfileId)
    {
        var url = $"{_supabaseUrl}/rest/v1/guide_profiles?id=eq.{guideProfileId}&select=user_id";
        var rows = await GetAsync<List<GuideProfileRow>>(url);
        return rows?.FirstOrDefault()?.UserId;
    }

    private async Task<T?> GetAsync<T>(string url)
    {
        var request = BuildRequest(HttpMethod.Get, url);
        var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, content);
        return JsonSerializer.Deserialize<T>(content, _json);
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url, string? userToken = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("apikey", _anonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", userToken ?? _anonKey);
        req.Headers.Add("Accept", "application/json");
        return req;
    }

    private static void EnsureSuccess(HttpResponseMessage r, string content)
    {
        if (!r.IsSuccessStatusCode)
            throw new Exception($"Supabase {r.StatusCode}: {content}");
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static BookingDto MapToDto(BookingRow row, string? packageTitle)
    {
        return new BookingDto(
            Id: row.Id ?? "",
            TravelerId: row.TravelerId ?? "",
            GuideProfileId: row.GuideProfileId ?? "",
            ExperiencePackageId: row.ExperiencePackageId ?? "",
            PackageTitle: packageTitle,
            BookingDate: row.BookingDate ?? "",
            StartTime: row.StartTime ?? "",
            GuestCount: row.GuestCount,
            TotalAmount: row.TotalAmount,
            PlatformFee: row.PlatformFee,
            GuideEarnings: row.GuideEarnings,
            Status: MapStatus(row.Status),
            TravelerNotes: row.TravelerNotes,
            CreatedAt: row.CreatedAt,
            UpdatedAt: row.UpdatedAt
        );
    }

    private static BookingDto MapJoinedToDto(BookingRowJoined row)
    {
        return MapToDto(row, row.ExperiencePackage?.Title);
    }
}

// ── Row models matching database_setup.sql ────────────────────────────────────

internal class BookingRow
{
    [JsonPropertyName("id")]                    public string? Id { get; set; }
    [JsonPropertyName("traveler_id")]           public string? TravelerId { get; set; }
    [JsonPropertyName("guide_profile_id")]      public string? GuideProfileId { get; set; }
    [JsonPropertyName("experience_package_id")] public string? ExperiencePackageId { get; set; }
    [JsonPropertyName("booking_date")]          public string? BookingDate { get; set; }
    [JsonPropertyName("start_time")]            public string? StartTime { get; set; }
    [JsonPropertyName("guest_count")]           public int GuestCount { get; set; } = 1;
    [JsonPropertyName("total_amount")]          public decimal TotalAmount { get; set; }
    [JsonPropertyName("platform_fee")]          public decimal PlatformFee { get; set; }
    [JsonPropertyName("guide_earnings")]        public decimal GuideEarnings { get; set; }
    [JsonPropertyName("status")]                public int Status { get; set; }
    [JsonPropertyName("traveler_notes")]        public string? TravelerNotes { get; set; }
    [JsonPropertyName("created_at")]            public DateTime CreatedAt { get; set; }
    [JsonPropertyName("updated_at")]            public DateTime UpdatedAt { get; set; }
}

internal class BookingRowJoined : BookingRow
{
    [JsonPropertyName("experience_packages")] public ExperiencePackageBasic? ExperiencePackage { get; set; }
}

internal class ExperiencePackageBasic
{
    [JsonPropertyName("title")] public string? Title { get; set; }
}

internal class GuideAvailabilityRow
{
    [JsonPropertyName("id")]                public string? Id { get; set; }
    [JsonPropertyName("guide_profile_id")]  public string? GuideProfileId { get; set; }
    [JsonPropertyName("unavailable_date")]  public string? UnavailableDate { get; set; }
    [JsonPropertyName("reason")]            public string? Reason { get; set; }
    [JsonPropertyName("created_at")]        public DateTime CreatedAt { get; set; }
}
