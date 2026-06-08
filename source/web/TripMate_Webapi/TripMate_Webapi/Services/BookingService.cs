using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace TripMate_WebAPI.Services;

/// <summary>
/// BookingService — cập nhật theo schema mới:
/// bookings.tour_availability_id → tour_availability → guide_tours → tour_templates
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
        // 1. Lấy thông tin tour_availability để xác minh và tính giá
        var availability = await GetAvailabilityAsync(req.TourAvailabilityId)
            ?? throw new Exception("Không tìm thấy lịch trống cho tour này");

        if (availability.RemainingSlots < req.Guests)
            throw new Exception($"Chỉ còn {availability.RemainingSlots} chỗ trống, không đủ cho {req.Guests} khách");

        if (req.Guests < 1)
            throw new Exception("Số khách phải ít nhất là 1");

        // 2. Lấy thông tin guide_tour để tính giá
        var guideTour = await GetGuideTourAsync(availability.GuideTourId)
            ?? throw new Exception("Không tìm thấy thông tin tour");

        var totalPrice = guideTour.Price * req.Guests;

        // 3. Insert booking
        var body = new
        {
            tour_availability_id = req.TourAvailabilityId,
            traveler_id = travelerId,
            guests = req.Guests,
            total_price = totalPrice,
            note = req.Note,
            status = "pending",
        };

        var request = BuildRequest(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/bookings", userToken);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, content);

        var rows = JsonSerializer.Deserialize<List<BookingRow>>(content, _json);
        var row = rows?.FirstOrDefault() ?? throw new Exception("Tạo booking thất bại");

        // 4. Giảm remaining_slots
        await DecrementSlotsAsync(req.TourAvailabilityId, req.Guests, userToken);

        var dto = MapToDto(row, availability, guideTour);

        // 5. Gửi notification và tạo conversation
        _ = _notif.SendAsync(guideTour.GuideId ?? "", "booking_created",
            "Có người đặt tour của bạn!",
            $"{dto.TourTitle} — {dto.Guests} khách ngày {dto.TourDate:dd/MM/yyyy}",
            new { bookingId = dto.Id, travelerId });

        _ = _notif.SendAsync(travelerId, "booking_confirmed",
            "Đặt tour thành công!",
            $"Bạn đã đặt {dto.TourTitle} ngày {dto.TourDate:dd/MM/yyyy}",
            new { bookingId = dto.Id });

        _ = _chat.GetOrCreateConversationAsync(travelerId, guideTour.GuideId ?? "", dto.Id, userToken);

        return dto;
    }

    // ── Get My Bookings ───────────────────────────────────────────────────────

    public async Task<List<BookingDto>> GetMyBookingsAsync(string travelerId)
    {
        // Join bookings → tour_availability → guide_tours → tour_templates
        var url = $"{_supabaseUrl}/rest/v1/bookings" +
                  $"?traveler_id=eq.{travelerId}" +
                  $"&order=created_at.desc" +
                  $"&select=*,tour_availability(id,date,remaining_slots,guide_tour_id," +
                  $"guide_tours(id,guide_id,price,duration_hours,max_participants,status,rating," +
                  $"tour_templates(title,location,images)))";

        var rows = await GetAsync<List<BookingRowJoined>>(url) ?? [];

        return rows.Select(MapJoinedToDto).ToList();
    }

    // ── Get Booking By Id ─────────────────────────────────────────────────────

    public async Task<BookingDto?> GetBookingByIdAsync(string bookingId)
    {
        var url = $"{_supabaseUrl}/rest/v1/bookings" +
                  $"?id=eq.{bookingId}" +
                  $"&select=*,tour_availability(id,date,remaining_slots,guide_tour_id," +
                  $"guide_tours(id,guide_id,price,duration_hours,max_participants,status,rating," +
                  $"tour_templates(title,location,images)))";

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

        // Update status
        var updates = new { status = "cancelled" };
        var request = BuildRequest(HttpMethod.Patch,
            $"{_supabaseUrl}/rest/v1/bookings?id=eq.{bookingId}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        EnsureSuccess(response, await response.Content.ReadAsStringAsync());

        // Hoàn trả remaining_slots
        await IncrementSlotsAsync(booking.TourAvailabilityId, booking.Guests);
    }

    // ── Get Tour Availability ─────────────────────────────────────────────────

    public async Task<List<TourAvailabilityDto>> GetAvailabilityByGuideTourAsync(string guideTourId)
    {
        var url = $"{_supabaseUrl}/rest/v1/tour_availability" +
                  $"?guide_tour_id=eq.{guideTourId}" +
                  $"&remaining_slots=gt.0" +
                  $"&order=date.asc";

        var rows = await GetAsync<List<AvailabilityRow>>(url) ?? [];

        return rows.Select(r => new TourAvailabilityDto(
            Id: r.Id ?? "",
            GuideTourId: r.GuideTourId ?? "",
            Date: DateOnly.Parse(r.Date ?? DateTime.UtcNow.ToString("yyyy-MM-dd")),
            RemainingSlots: r.RemainingSlots
        )).ToList();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<AvailabilityRow?> GetAvailabilityAsync(string availabilityId)
    {
        var url = $"{_supabaseUrl}/rest/v1/tour_availability?id=eq.{availabilityId}&select=*";
        var rows = await GetAsync<List<AvailabilityRow>>(url);
        return rows?.FirstOrDefault();
    }

    private async Task<GuideTourDetailRow?> GetGuideTourAsync(string guideTourId)
    {
        var url = $"{_supabaseUrl}/rest/v1/guide_tours" +
                  $"?id=eq.{guideTourId}" +
                  $"&select=*,tour_templates(title,description,location,images)";
        var rows = await GetAsync<List<GuideTourDetailRow>>(url);
        return rows?.FirstOrDefault();
    }

    private async Task DecrementSlotsAsync(string availabilityId, int guests, string userToken)
    {
        // RPC call or raw update
        var url = $"{_supabaseUrl}/rest/v1/tour_availability?id=eq.{availabilityId}";
        var request = BuildRequest(HttpMethod.Patch, url, userToken);
        // Use Postgres expression via RPC
        request.Content = new StringContent(
            $"{{\"remaining_slots\":\"remaining_slots - {guests}\"}}",
            Encoding.UTF8, "application/json");
        await _http.SendAsync(request);
    }

    private async Task IncrementSlotsAsync(string availabilityId, int guests)
    {
        var url = $"{_supabaseUrl}/rest/v1/tour_availability?id=eq.{availabilityId}";
        var request = BuildRequest(HttpMethod.Patch, url);
        request.Content = new StringContent(
            $"{{\"remaining_slots\":\"remaining_slots + {guests}\"}}",
            Encoding.UTF8, "application/json");
        await _http.SendAsync(request);
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

    private static BookingDto MapToDto(BookingRow row, AvailabilityRow availability, GuideTourDetailRow guideTour)
    {
        return new BookingDto(
            Id: row.Id ?? "",
            TourAvailabilityId: row.TourAvailabilityId ?? "",
            GuideTourId: availability.GuideTourId ?? "",
            TourTitle: guideTour.TourTemplate?.Title ?? "",
            TourLocation: guideTour.TourTemplate?.Location ?? "",
            TravelerId: row.TravelerId ?? "",
            TourDate: DateOnly.Parse(availability.Date ?? DateTime.UtcNow.ToString("yyyy-MM-dd")),
            Guests: row.Guests,
            TotalPrice: (double)row.TotalPrice,
            Note: row.Note,
            Status: row.Status ?? "pending",
            CreatedAt: row.CreatedAt,
            RemainingSlots: availability.RemainingSlots - row.Guests
        );
    }

    private static BookingDto MapJoinedToDto(BookingRowJoined row)
    {
        var av = row.TourAvailability;
        var gt = av?.GuideTour;
        var tmpl = gt?.TourTemplate;

        return new BookingDto(
            Id: row.Id ?? "",
            TourAvailabilityId: row.TourAvailabilityId ?? "",
            GuideTourId: av?.GuideTourId ?? "",
            TourTitle: tmpl?.Title ?? "",
            TourLocation: tmpl?.Location ?? "",
            TravelerId: row.TravelerId ?? "",
            TourDate: DateOnly.Parse(av?.Date ?? DateTime.UtcNow.ToString("yyyy-MM-dd")),
            Guests: row.Guests,
            TotalPrice: (double)row.TotalPrice,
            Note: row.Note,
            Status: row.Status ?? "pending",
            CreatedAt: row.CreatedAt,
            RemainingSlots: av?.RemainingSlots
        );
    }
}

// ── Row models ────────────────────────────────────────────────────────────────

internal class BookingRow
{
    [JsonPropertyName("id")]                    public string? Id { get; set; }
    [JsonPropertyName("tour_availability_id")]  public string? TourAvailabilityId { get; set; }
    [JsonPropertyName("traveler_id")]           public string? TravelerId { get; set; }
    [JsonPropertyName("guests")]                public int Guests { get; set; }
    [JsonPropertyName("total_price")]           public decimal TotalPrice { get; set; }
    [JsonPropertyName("note")]                  public string? Note { get; set; }
    [JsonPropertyName("status")]                public string? Status { get; set; }
    [JsonPropertyName("created_at")]            public DateTime CreatedAt { get; set; }
}

internal class BookingRowJoined : BookingRow
{
    [JsonPropertyName("tour_availability")] public AvailabilityRowJoined? TourAvailability { get; set; }
}

internal class AvailabilityRow
{
    [JsonPropertyName("id")]                public string? Id { get; set; }
    [JsonPropertyName("guide_tour_id")]     public string? GuideTourId { get; set; }
    [JsonPropertyName("date")]              public string? Date { get; set; }
    [JsonPropertyName("remaining_slots")]   public int RemainingSlots { get; set; }
}

internal class AvailabilityRowJoined : AvailabilityRow
{
    [JsonPropertyName("guide_tours")] public GuideTourDetailRow? GuideTour { get; set; }
}

internal class GuideTourDetailRow
{
    [JsonPropertyName("id")]                public string? Id { get; set; }
    [JsonPropertyName("guide_id")]          public string? GuideId { get; set; }
    [JsonPropertyName("price")]             public decimal Price { get; set; }
    [JsonPropertyName("duration_hours")]    public int DurationHours { get; set; }
    [JsonPropertyName("max_participants")]  public int MaxParticipants { get; set; }
    [JsonPropertyName("status")]            public string? Status { get; set; }
    [JsonPropertyName("rating")]            public double Rating { get; set; }
    [JsonPropertyName("tour_templates")]    public TourTemplateRow? TourTemplate { get; set; }
}
