using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TripMate_WebAPI.Models;

namespace TripMate_WebAPI.Services;

public class BookingService
{
    private readonly HttpClient _http;
    private readonly string _supabaseUrl;
    private readonly string _anonKey;
    private readonly NotificationService _notif;
    private readonly ChatService _chat;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public BookingService(HttpClient http, IConfiguration config,
        NotificationService notif, ChatService chat)
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
        // 1. Lấy thông tin tour để tính giá
        var tour = await GetTourAsync(req.TourId)
            ?? throw new Exception("Không tìm thấy tour");

        if (req.Guests < 1 || req.Guests > tour.MaxParticipants)
            throw new Exception($"Số khách phải từ 1 đến {tour.MaxParticipants}");

        if (req.TourDate < DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            throw new Exception("Ngày tour phải từ ngày mai trở đi");

        var unitPrice  = tour.Price;
        var totalPrice = unitPrice * req.Guests;

        // 2. Insert booking
        var body = new
        {
            guide_tour_id = req.TourId,  // Updated to use guide_tour_id
            traveler_id = travelerId,
            tour_date   = req.TourDate.ToString("yyyy-MM-dd"),
            guests      = req.Guests,
            unit_price  = unitPrice,
            total_price = totalPrice,
            note        = req.Note,
            status      = "pending",
        };

        var request = BuildRequest(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/bookings", userToken);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        var content  = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, content);

        var rows = JsonSerializer.Deserialize<List<BookingRow>>(content, _json);
        var row  = rows?.FirstOrDefault() ?? throw new Exception("Tạo booking thất bại");

        var dto = MapToDto(row, tour);

        // Gửi notification cho guide
        _ = _notif.SendAsync(tour.GuideId ?? "", "booking_created",
            "Có người đặt tour của bạn!",
            $"{dto.TourTitle} — {dto.Guests} khách ngày {dto.TourDate:dd/MM/yyyy}",
            new { bookingId = dto.Id, travelerId });

        // Gửi notification cho traveler
        _ = _notif.SendAsync(travelerId, "booking_confirmed",
            "Đặt tour thành công!",
            $"Bạn đã đặt {dto.TourTitle} ngày {dto.TourDate:dd/MM/yyyy}",
            new { bookingId = dto.Id });

        // Tạo conversation giữa traveler và guide
        _ = _chat.GetOrCreateConversationAsync(travelerId, tour.GuideId ?? "", dto.Id, userToken);

        return dto;
    }

    // ── Get My Bookings ───────────────────────────────────────────────────────

    public async Task<List<BookingDto>> GetMyBookingsAsync(string travelerId)
    {
        var url = $"{_supabaseUrl}/rest/v1/bookings" +
                  $"?traveler_id=eq.{travelerId}&order=created_at.desc&select=*";

        var rows = await GetAsync<List<BookingRow>>(url) ?? [];

        // Lấy tour info cho từng booking
        var result = new List<BookingDto>();
        foreach (var row in rows)
        {
            var tour = await GetTourAsync(row.TourId ?? "");
            result.Add(MapToDto(row, tour));
        }
        return result;
    }

    // ── Get Booking By Id ─────────────────────────────────────────────────────

    public async Task<BookingDto?> GetBookingByIdAsync(string bookingId)
    {
        var url = $"{_supabaseUrl}/rest/v1/bookings?id=eq.{bookingId}&select=*";
        var rows = await GetAsync<List<BookingRow>>(url);
        var row  = rows?.FirstOrDefault();
        if (row == null) return null;

        var tour = await GetTourAsync(row.TourId ?? "");
        return MapToDto(row, tour);
    }

    // ── Cancel Booking ────────────────────────────────────────────────────────

    public async Task CancelBookingAsync(string bookingId, string travelerId)
    {
        // Verify ownership
        var booking = await GetBookingByIdAsync(bookingId)
            ?? throw new Exception("Không tìm thấy booking");

        if (booking.TravelerId != travelerId)
            throw new UnauthorizedAccessException("Bạn không có quyền hủy booking này");

        if (booking.Status == "completed")
            throw new Exception("Không thể hủy booking đã hoàn thành");

        var updates = new { status = "cancelled", updated_at = DateTime.UtcNow };
        var request = BuildRequest(HttpMethod.Patch,
            $"{_supabaseUrl}/rest/v1/bookings?id=eq.{bookingId}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        EnsureSuccess(response, await response.Content.ReadAsStringAsync());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<TourRow?> GetTourAsync(string tourId)
    {
        // Updated to use guide_tours joined with tour_templates
        var url = $"{_supabaseUrl}/rest/v1/guide_tours?id=eq.{tourId}&select=*,tour_templates(title,description,location,images,created_at),profiles(full_name)";
        var rows = await GetAsync<List<GuideTourRow>>(url);
        var guideTour = rows?.FirstOrDefault();
        return guideTour != null ? MapGuideTourToTourRow(guideTour) : null;
    }

    private async Task<T?> GetAsync<T>(string url)
    {
        var request = BuildRequest(HttpMethod.Get, url);
        var response = await _http.SendAsync(request);
        var content  = await response.Content.ReadAsStringAsync();
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

    // ── Mapping methods ───────────────────────────────────────────────────────

    private static TourRow MapGuideTourToTourRow(GuideTourRow guideTour)
    {
        return new TourRow
        {
            Id = guideTour.Id,
            GuideId = guideTour.GuideId,
            Title = guideTour.TourTemplate?.Title,
            Description = guideTour.TourTemplate?.Description,
            Location = guideTour.TourTemplate?.Location,
            Price = guideTour.Price,
            DurationHours = guideTour.DurationHours,
            MaxParticipants = guideTour.MaxParticipants,
            Images = guideTour.TourTemplate?.Images,
            Rating = guideTour.Rating,
            TotalReviews = guideTour.TotalReviews,
            Status = guideTour.Status,
            CreatedAt = guideTour.TourTemplate?.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = guideTour.TourTemplate?.CreatedAt ?? DateTime.UtcNow
        };
    }

    private static BookingDto MapToDto(BookingRow row, TourRow? tour) => new(
        Id:           row.Id ?? "",
        TourId:       row.TourId ?? "",
        TourTitle:    tour?.Title ?? "",
        TourLocation: tour?.Location ?? "",
        TravelerId:   row.TravelerId ?? "",
        TourDate:     DateOnly.Parse(row.TourDate ?? DateTime.UtcNow.ToString("yyyy-MM-dd")),
        Guests:       row.Guests,
        UnitPrice:    row.UnitPrice,
        TotalPrice:   row.TotalPrice,
        Note:         row.Note,
        Status:       row.Status ?? "pending",
        CreatedAt:    row.CreatedAt
    );
}

// ── Row models ────────────────────────────────────────────────────────────────

internal class BookingRow
{
    [JsonPropertyName("id")]              public string? Id { get; set; }
    [JsonPropertyName("guide_tour_id")]   public string? TourId { get; set; }  // Updated to guide_tour_id
    [JsonPropertyName("traveler_id")]     public string? TravelerId { get; set; }
    [JsonPropertyName("tour_date")]       public string? TourDate { get; set; }
    [JsonPropertyName("guests")]          public int Guests { get; set; }
    [JsonPropertyName("unit_price")]      public double UnitPrice { get; set; }
    [JsonPropertyName("total_price")]     public double TotalPrice { get; set; }
    [JsonPropertyName("note")]            public string? Note { get; set; }
    [JsonPropertyName("status")]          public string? Status { get; set; }
    [JsonPropertyName("created_at")]      public DateTime CreatedAt { get; set; }
}
