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
    private readonly ChatService _chat;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // Status mapping: smallint ↔ string
    private static readonly Dictionary<int, string> StatusMap = new()
    {
        { 0, "pending" }, { 1, "confirmed" }, { 2, "completed" }, { 3, "cancelled" }
    };
    private static readonly Dictionary<string, int> StatusReverseMap = new()
    {
        { "pending", 0 }, { "confirmed", 1 }, { "completed", 2 }, { "cancelled", 3 }
    };

    public static string MapStatus(int status) => StatusMap.GetValueOrDefault(status, "pending");
    public static int MapStatus(string status) => StatusReverseMap.GetValueOrDefault(status, 0);

    // Platform fee rate (e.g. 15%)
    private const decimal PlatformFeeRate = 0.15m;

    public BookingService(HttpClient http, IConfiguration config,
        INotificationService notif, ChatService chat)
    {
        _http = http;
        _supabaseUrl = config["Supabase:Url"]!;
        _anonKey = config["Supabase:AnonKey"]!;
        _notif = notif;
        _chat = chat;
    }

    // ── Create Booking ────────────────────────────────────────────────────────

    public async Task<BookingDto> CreateBookingAsync(
        string travelerId, CreateBookingRequest req, string userToken)
    {
        // 1. Get experience package to calculate price
        var package = await GetExperiencePackageAsync(req.ExperiencePackageId)
            ?? throw new Exception("Không tìm thấy gói trải nghiệm");

        if (!package.IsActive)
            throw new Exception("Gói trải nghiệm này không còn hoạt động");

        if (req.GuestCount < 1)
            throw new Exception("Số khách phải ít nhất là 1");

        if (req.GuestCount > package.MaxGroupSize)
            throw new Exception($"Số khách tối đa cho gói này là {package.MaxGroupSize}");

        // 2. Check guide availability (blacklist check)
        var isUnavailable = await IsGuideUnavailableAsync(
            package.GuideProfileId!, req.BookingDate);
        if (isUnavailable)
            throw new Exception("Hướng dẫn viên không khả dụng vào ngày này");

        // 3. Calculate pricing
        decimal totalAmount;
        if (package.PricePerPerson.HasValue && package.PricePerPerson > 0)
            totalAmount = package.PricePerPerson.Value * req.GuestCount;
        else
            totalAmount = package.PricePerSession;

        var platformFee = Math.Round(totalAmount * PlatformFeeRate, 2);
        var guideEarnings = totalAmount - platformFee;

        // 4. Insert booking
        var body = new
        {
            traveler_id = travelerId,
            guide_profile_id = package.GuideProfileId,
            experience_package_id = req.ExperiencePackageId,
            booking_date = req.BookingDate,
            start_time = req.StartTime,
            guest_count = req.GuestCount,
            total_amount = totalAmount,
            platform_fee = platformFee,
            guide_earnings = guideEarnings,
            status = 0, // Pending
            traveler_notes = req.TravelerNotes,
        };

        var request = BuildRequest(HttpMethod.Post,
            $"{_supabaseUrl}/rest/v1/bookings", userToken);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, content);

        var rows = JsonSerializer.Deserialize<List<BookingRow>>(content, _json);
        var row = rows?.FirstOrDefault() ?? throw new Exception("Tạo booking thất bại");

        var dto = MapToDto(row, package.Title);

        // 5. Send notifications
        var guideUserId = await GetGuideUserIdAsync(package.GuideProfileId!);
        if (guideUserId != null)
        {
            _ = _notif.SendAsync(guideUserId, "booking_created",
                "Có người đặt gói trải nghiệm!",
                $"{package.Title} — {req.GuestCount} khách ngày {req.BookingDate}",
                new { bookingId = dto.Id, travelerId });

            _ = _chat.GetOrCreateConversationAsync(
                travelerId, guideUserId, dto.Id, userToken);
        }

        _ = _notif.SendAsync(travelerId, "booking_confirmed",
            "Đặt tour thành công!",
            $"Bạn đã đặt {package.Title} ngày {req.BookingDate}",
            new { bookingId = dto.Id });

        return dto;
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
        var booking = await GetBookingByIdAsync(bookingId)
            ?? throw new Exception("Không tìm thấy booking");

        if (booking.TravelerId != travelerId)
            throw new UnauthorizedAccessException("Bạn không có quyền hủy booking này");

        if (booking.Status == "completed")
            throw new Exception("Không thể hủy booking đã hoàn thành");

        // Update status to 3 (Cancelled)
        var updates = new { status = 3 };
        var request = BuildRequest(HttpMethod.Patch,
            $"{_supabaseUrl}/rest/v1/bookings?id=eq.{bookingId}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        EnsureSuccess(response, await response.Content.ReadAsStringAsync());
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

    private async Task<bool> IsGuideUnavailableAsync(string guideProfileId, string date)
    {
        var url = $"{_supabaseUrl}/rest/v1/guide_availability" +
                  $"?guide_profile_id=eq.{guideProfileId}" +
                  $"&unavailable_date=eq.{date}";
        var rows = await GetAsync<List<GuideAvailabilityRow>>(url);
        return rows?.Count > 0;
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
            PaymentReference: row.PaymentReference,
            PaymentMethod: row.PaymentMethod,
            EscrowReleased: row.EscrowReleased,
            TravelerNotes: row.TravelerNotes,
            CancelReason: row.CancelReason,
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
    [JsonPropertyName("payment_reference")]     public string? PaymentReference { get; set; }
    [JsonPropertyName("payment_method")]        public string? PaymentMethod { get; set; }
    [JsonPropertyName("escrow_released")]       public bool EscrowReleased { get; set; }
    [JsonPropertyName("traveler_notes")]        public string? TravelerNotes { get; set; }
    [JsonPropertyName("guide_response_at")]     public DateTime? GuideResponseAt { get; set; }
    [JsonPropertyName("cancel_reason")]         public string? CancelReason { get; set; }
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
