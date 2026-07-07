using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TripMate_WebAPI.Extensions;


namespace TripMate_WebAPI.Services;

/// <summary>
/// Gọi thẳng Supabase GoTrue REST API để xử lý auth
/// (supabase-csharp chưa expose đủ session info cần thiết)
/// </summary>
public class SupabaseAuthService
{
    private readonly HttpClient _http;
    private readonly Supabase.Client _supabase;
    private readonly INotificationService _notificationService;
    private readonly string _supabaseUrl;
    private readonly string _anonKey;
    private readonly string _serviceRoleKey;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public SupabaseAuthService(HttpClient http, Supabase.Client supabase, INotificationService notificationService, IConfiguration config)
    {
        _http = http;
        _supabase = supabase;
        _notificationService = notificationService;
        _supabaseUrl = config["Supabase:Url"]!;
        _anonKey = config["Supabase:AnonKey"]!;
        _serviceRoleKey = config["Supabase:ServiceRoleKey"]!;
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
        string? languages = null, string? bio = null, string? certificatePath = null, string? avatarUrl = null)
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

        // If Email Confirmations are on, the response might be a GoTrueUser instead of a Session
        if (string.IsNullOrEmpty(session.AccessToken) && !response.Contains("\"access_token\""))
        {
            var userOnly = JsonSerializer.Deserialize<GoTrueUser>(response, _json);
            if (userOnly != null && !string.IsNullOrEmpty(userOnly.Id))
            {
                session.User = userOnly;
            }
        }

        if (string.IsNullOrEmpty(session.User?.Id))
            throw new Exception("Đăng ký thất bại");

        // 2. Upsert profile vào bảng profiles với thông tin mở rộng
        if (string.IsNullOrEmpty(session.AccessToken))
        {
            Console.WriteLine($"[WARNING] No access token returned for {email}. Using ServiceRoleKey to upsert profile.");
        }
        await UpsertProfileAsync(session.AccessToken, session.User.Id, email, fullName, role, 
            phoneNumber, experience, specialization, languages, bio, certificatePath, avatarUrl);

        // 3. Notify admin if it's a guide registration
        if (role == "guide")
        {
            await _notificationService.NotifyAdminNewGuideApplicationAsync(session.User.Id, fullName, email);
        }

        if (string.IsNullOrEmpty(session.AccessToken))
            throw new Exception("Đăng ký thành công nhưng yêu cầu xác thực email. Vui lòng kiểm tra hộp thư của bạn.");

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

    public async Task<ProfileRow> GetProfileAsync(string accessToken, string userId)
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
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task UpsertProfileAsync(string? accessToken, string userId, string email, string fullName, string role = "traveler",
        string? phoneNumber = null, string? experience = null, string? specialization = null, 
        string? languages = null, string? bio = null, string? certificatePath = null, string? avatarUrl = null)
    {
        try
        {
            var profile = new
            {
                id = userId,
                email,
                full_name = fullName,
                phone_number = phoneNumber,
                role = role,
                experience = experience,
                specialization = specialization,
                languages = languages,
                bio = bio,
                avatar_url = avatarUrl,
                certificate_url = certificatePath,
                is_active = role == "guide" ? false : true,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,
            };

            var token = string.IsNullOrEmpty(accessToken) ? _serviceRoleKey : accessToken;
            bool success = false;

            // Try INSERT first (for new users)
            var insertRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_supabaseUrl}/rest/v1/profiles");

            insertRequest.Headers.Add("apikey", _anonKey);
            insertRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            insertRequest.Headers.Add("Prefer", "resolution=merge-duplicates");
            insertRequest.Content = new StringContent(
                JsonSerializer.Serialize(profile), Encoding.UTF8, "application/json");

            var insertResponse = await _http.SendAsync(insertRequest);
            
            if (insertResponse.IsSuccessStatusCode)
            {
                success = true;
            }
            else
            {
                // If INSERT fails (user exists), try UPDATE
                var updateRequest = new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"{_supabaseUrl}/rest/v1/profiles?id=eq.{userId}");

                updateRequest.Headers.Add("apikey", _anonKey);
                updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                updateRequest.Content = new StringContent(
                    JsonSerializer.Serialize(new {
                        full_name = fullName,
                        phone_number = phoneNumber,
                        role = role,
                        experience = experience,
                        specialization = specialization,
                        languages = languages,
                        bio = bio,
                        avatar_url = avatarUrl,
                        certificate_url = certificatePath,
                        is_active = role == "guide" ? false : true,
                        updated_at = DateTime.UtcNow,
                    }), Encoding.UTF8, "application/json");

                var updateResponse = await _http.SendAsync(updateRequest);
                
                if (updateResponse.IsSuccessStatusCode)
                {
                    success = true;
                }
                else
                {
                    var err = await updateResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ERROR] UpsertProfileAsync failed: {updateResponse.StatusCode} - {err}");
                    throw new Exception($"Không thể lưu thông tin hồ sơ: {err}");
                }
            }

            // Create/Update guide_profiles if role is guide
            if (success && role == "guide")
            {
                // 1. Check if guide_profiles already exists
                var checkReq = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{_supabaseUrl}/rest/v1/guide_profiles?user_id=eq.{userId}&select=id");
                checkReq.Headers.Add("apikey", _anonKey);
                checkReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var checkRes = await _http.SendAsync(checkReq);
                var checkContent = await checkRes.Content.ReadAsStringAsync();

                string? guideProfileId = null;
                bool exists = false;

                if (checkRes.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(checkContent) && checkContent.Trim() != "[]")
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(checkContent);
                        if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                        {
                            var first = doc.RootElement[0];
                            if (first.TryGetProperty("id", out var idProp))
                            {
                                guideProfileId = idProp.GetString();
                                exists = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WARNING] Error parsing existing guide_profile id: {ex.Message}");
                    }
                }

                if (string.IsNullOrEmpty(guideProfileId))
                {
                    guideProfileId = Guid.NewGuid().ToString();
                }

                // Parse array fields
                var langList = string.IsNullOrEmpty(languages) 
                    ? new List<string>() 
                    : languages.Split(',').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();

                var specList = string.IsNullOrEmpty(specialization) 
                    ? new List<string>() 
                    : specialization.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

                var guideProfileData = new
                {
                    id = guideProfileId,
                    user_id = userId,
                    bio = bio ?? "",
                    languages = langList,
                    specialties = specList,
                    city_area = "Hội An",
                    price_per_hour = 0,
                    is_verified = false,
                    average_rating = 0.00,
                    total_reviews = 0,
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                };

                if (!exists)
                {
                    var createReq = new HttpRequestMessage(
                        HttpMethod.Post,
                        $"{_supabaseUrl}/rest/v1/guide_profiles");
                    createReq.Headers.Add("apikey", _anonKey);
                    createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    createReq.Content = new StringContent(JsonSerializer.Serialize(guideProfileData), Encoding.UTF8, "application/json");
                    var createRes = await _http.SendAsync(createReq);
                    if (!createRes.IsSuccessStatusCode)
                    {
                        var err = await createRes.Content.ReadAsStringAsync();
                        Console.WriteLine($"[ERROR] Failed to create guide_profile: {createRes.StatusCode} - {err}");
                    }
                }
                else
                {
                    var updateReq = new HttpRequestMessage(
                        HttpMethod.Patch,
                        $"{_supabaseUrl}/rest/v1/guide_profiles?id=eq.{guideProfileId}");
                    updateReq.Headers.Add("apikey", _anonKey);
                    updateReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    updateReq.Content = new StringContent(JsonSerializer.Serialize(new {
                        bio = bio ?? "",
                        languages = langList,
                        specialties = specList,
                        updated_at = DateTime.UtcNow
                    }), Encoding.UTF8, "application/json");
                    var updateRes = await _http.SendAsync(updateReq);
                    if (!updateRes.IsSuccessStatusCode)
                    {
                        var err = await updateRes.Content.ReadAsStringAsync();
                        Console.WriteLine($"[ERROR] Failed to update guide_profile: {updateRes.StatusCode} - {err}");
                    }
                }

                // 2. If certificatePath is provided, upsert certificate
                if (!string.IsNullOrEmpty(certificatePath))
                {
                    var certCheckReq = new HttpRequestMessage(
                        HttpMethod.Get,
                        $"{_supabaseUrl}/rest/v1/guide_certificates?guide_profile_id=eq.{guideProfileId}&select=id");
                    certCheckReq.Headers.Add("apikey", _anonKey);
                    certCheckReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var certCheckRes = await _http.SendAsync(certCheckReq);
                    var certCheckContent = await certCheckRes.Content.ReadAsStringAsync();

                    bool certExists = certCheckRes.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(certCheckContent) && certCheckContent.Trim() != "[]";

                    var certData = new
                    {
                        guide_profile_id = guideProfileId,
                        certificate_name = "Chứng chỉ hướng dẫn viên",
                        file_url = certificatePath,
                        status = "pending",
                        created_at = DateTime.UtcNow
                    };

                    if (!certExists)
                    {
                        var certCreateReq = new HttpRequestMessage(
                            HttpMethod.Post,
                            $"{_supabaseUrl}/rest/v1/guide_certificates");
                        certCreateReq.Headers.Add("apikey", _anonKey);
                        certCreateReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        certCreateReq.Content = new StringContent(JsonSerializer.Serialize(certData), Encoding.UTF8, "application/json");
                        await _http.SendAsync(certCreateReq);
                    }
                    else
                    {
                        var certUpdateReq = new HttpRequestMessage(
                            HttpMethod.Patch,
                            $"{_supabaseUrl}/rest/v1/guide_certificates?guide_profile_id=eq.{guideProfileId}");
                        certUpdateReq.Headers.Add("apikey", _anonKey);
                        certUpdateReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        certUpdateReq.Content = new StringContent(JsonSerializer.Serialize(new {
                            file_url = certificatePath,
                            status = "pending"
                        }), Encoding.UTF8, "application/json");
                        await _http.SendAsync(certUpdateReq);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] UpsertProfileAsync exception: {ex.Message}");
            throw new Exception($"Không thể lưu thông tin hồ sơ: {ex.Message}");
        }
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
                PhoneNumber: profile.PhoneNumber,
                AvatarUrl: profile.AvatarUrl,
                Role: profile.Role ?? "traveler",
                IsActive: profile.IsActive,
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


