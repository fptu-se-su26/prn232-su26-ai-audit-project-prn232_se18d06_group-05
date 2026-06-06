using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TripMate_WebAPI.Extensions;
using TripMate_WebAPI.Models;

namespace TripMate_WebAPI.Services;

/// <summary>
/// Gọi thẳng Supabase GoTrue REST API để xử lý auth
/// (supabase-csharp chưa expose đủ session info cần thiết)
/// </summary>
public class SupabaseAuthService
{
    private readonly HttpClient _http;
    private readonly Supabase.Client _supabase;
    private readonly string _supabaseUrl;
    private readonly string _anonKey;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public SupabaseAuthService(HttpClient http, Supabase.Client supabase, IConfiguration config)
    {
        _http = http;
        _supabase = supabase;
        _supabaseUrl = config["Supabase:Url"]!;
        _anonKey = config["Supabase:AnonKey"]!;
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    public async Task<AuthResponse> LoginAsync(string email, string password)
    {
        var body = JsonSerializer.Serialize(new { email, password });
        var response = await PostGoTrueAsync("/auth/v1/token?grant_type=password", body);

        var session = JsonSerializer.Deserialize<GoTrueSession>(response, _json)
            ?? throw new Exception("Phản hồi không hợp lệ từ Supabase");

        var user = await GetProfileAsync(session.AccessToken, session.User.Id);
        return MapToAuthResponse(session, user);
    }

    // ── Register ──────────────────────────────────────────────────────────────

    public async Task<AuthResponse> RegisterAsync(string email, string password, string fullName, string role = "traveler", 
        string? phoneNumber = null, string? experience = null, string? specialization = null, 
        string? languages = null, string? bio = null, string? certificatePath = null)
    {
        // 1. Tạo tài khoản Supabase Auth
        var body = JsonSerializer.Serialize(new
        {
            email,
            password,
            data = new { full_name = fullName, role = role }
        });
        var response = await PostGoTrueAsync("/auth/v1/signup", body);

        var session = JsonSerializer.Deserialize<GoTrueSession>(response, _json)
            ?? throw new Exception("Phản hồi không hợp lệ từ Supabase");

        if (session.User?.Id == null)
            throw new Exception("Đăng ký thất bại");

        // 2. Upsert profile vào bảng profiles với thông tin mở rộng
        await UpsertProfileAsync(session.AccessToken, session.User.Id, email, fullName, role, 
            phoneNumber, experience, specialization, languages, bio, certificatePath);

        var user = await GetProfileAsync(session.AccessToken, session.User.Id);
        return MapToAuthResponse(session, user);
    }

    // ── Refresh Token ─────────────────────────────────────────────────────────

    public async Task<AuthResponse> RefreshAsync(string refreshToken)
    {
        var body = JsonSerializer.Serialize(new { refresh_token = refreshToken });
        var response = await PostGoTrueAsync("/auth/v1/token?grant_type=refresh_token", body);

        var session = JsonSerializer.Deserialize<GoTrueSession>(response, _json)
            ?? throw new Exception("Không thể làm mới token");

        var user = await GetProfileAsync(session.AccessToken, session.User.Id);
        return MapToAuthResponse(session, user);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string> PostGoTrueAsync(string path, string jsonBody)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _supabaseUrl + path);
        request.Headers.Add("apikey", _anonKey);
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var err = JsonSerializer.Deserialize<GoTrueError>(content, _json);
            throw new Exception(MapGoTrueError(err?.GetMessage() ?? content));
        }

        return content;
    }

    private async Task<ProfileRow> GetProfileAsync(string accessToken, string userId)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_supabaseUrl}/rest/v1/profiles?id=eq.{userId}&select=*");

        request.Headers.Add("apikey", _anonKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Accept", "application/json");

        var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Debug logging
        Console.WriteLine($"Profile API Response Status: {response.StatusCode}");
        Console.WriteLine($"Profile API Response Content: {content}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Profile API Error: {response.StatusCode} - {content}");
            return CreateDefaultProfile(userId);
        }

        // Handle empty response
        if (string.IsNullOrWhiteSpace(content) || content.Trim() == "[]")
        {
            Console.WriteLine("Empty profile response, creating default profile");
            return CreateDefaultProfile(userId);
        }

        try
        {
            // Validate JSON format first
            if (!content.Trim().StartsWith("[") && !content.Trim().StartsWith("{"))
            {
                Console.WriteLine($"Invalid JSON format: {content}");
                return CreateDefaultProfile(userId);
            }

            // Try to deserialize as array first
            if (content.Trim().StartsWith("["))
            {
                var rows = JsonSerializer.Deserialize<List<ProfileRow>>(content, _json);
                return rows?.FirstOrDefault() ?? CreateDefaultProfile(userId);
            }
            else
            {
                // Try as single object
                var singleRow = JsonSerializer.Deserialize<ProfileRow>(content, _json);
                return singleRow ?? CreateDefaultProfile(userId);
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON Deserialization Error: {ex.Message}");
            Console.WriteLine($"Content that failed: {content}");
            return CreateDefaultProfile(userId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error in GetProfileAsync: {ex.Message}");
            return CreateDefaultProfile(userId);
        }
    }

    private static ProfileRow CreateDefaultProfile(string userId)
    {
        return new ProfileRow
        {
            Id = userId,
            Role = "traveler",
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task UpsertProfileAsync(string accessToken, string userId, string email, string fullName, string role = "traveler",
        string? phoneNumber = null, string? experience = null, string? specialization = null, 
        string? languages = null, string? bio = null, string? certificatePath = null)
    {
        var profile = new
        {
            id = userId,
            email,
            full_name = fullName,
            phone = phoneNumber,         // schema mới dùng 'phone'
            role = role,
            experience = experience,
            specialization = specialization,
            languages = languages,
            bio = bio,
            status = role == "guide" ? "active" : "active",
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow,
        };

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_supabaseUrl}/rest/v1/profiles");

        request.Headers.Add("apikey", _anonKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Prefer", "resolution=merge-duplicates");
        request.Content = new StringContent(
            JsonSerializer.Serialize(profile), Encoding.UTF8, "application/json");

        await _http.SendAsync(request);
    }

    private static AuthResponse MapToAuthResponse(GoTrueSession session, ProfileRow profile)
    {
        return new AuthResponse(
            AccessToken: session.AccessToken,
            RefreshToken: session.RefreshToken,
            ExpiresAt: session.ExpiresAt,
            User: new UserDto(
                Id: profile.Id ?? session.User.Id,
                Email: profile.Email ?? session.User.Email ?? "",
                FullName: profile.FullName,
                Phone: profile.Phone,
                AvatarUrl: profile.AvatarUrl,
                Role: profile.Role ?? "traveler",
                CreatedAt: profile.CreatedAt
            )
        );
    }

    private static string MapGoTrueError(string msg) => msg switch
    {
        "Invalid login credentials" => "Email hoặc mật khẩu không đúng",
        "User already registered"   => "Email đã được đăng ký",
        "Email not confirmed"       => "Vui lòng xác nhận email trước khi đăng nhập",
        "invalid_credentials"       => "Email hoặc mật khẩu không đúng",
        "email_not_confirmed"       => "Vui lòng xác nhận email trước khi đăng nhập",
        "user_already_exists"       => "Email đã được đăng ký",
        _                           => msg
    };
}

// ── Internal GoTrue models ────────────────────────────────────────────────────

internal class GoTrueSession
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = "";

    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }

    [JsonPropertyName("user")]
    public GoTrueUser User { get; set; } = new();
}

internal class GoTrueUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

internal class GoTrueError
{
    // Supabase GoTrue v2 format: {"code":400,"error_code":"invalid_credentials","msg":"..."}
    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("code")]
    public int? Code { get; set; }

    // Legacy format: {"error":"...", "error_description":"..."}
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    // Helper to get most meaningful message
    public string GetMessage() =>
        ErrorDescription ?? Msg ?? Error ?? "Lỗi xác thực";
}

public class ProfileRow
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("phone")]          // schema mới: phone (không phải phone_number)
    public string? Phone { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}
