using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;


namespace TripMate_WebAPI.Services
{
    public interface IGoogleAuthService
    {
        Task<AuthResponse> LoginWithGoogleAsync(string idToken, string? accessToken = null);
    }

    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly SupabaseAuthService _authService;
        private readonly ILogger<GoogleAuthService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _googleClientId;
        private readonly string _supabaseUrl;
        private readonly string _anonKey;
        
        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public GoogleAuthService(
            HttpClient httpClient,
            SupabaseAuthService authService,
            ILogger<GoogleAuthService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _authService = authService;
            _logger = logger;
            _configuration = configuration;
            _googleClientId = _configuration["GoogleOAuth:ClientId"] 
                ?? throw new InvalidOperationException("Google Client ID not configured");
            _supabaseUrl = _configuration["Supabase:Url"] 
                ?? throw new InvalidOperationException("Supabase URL not configured");
            _anonKey = _configuration["Supabase:AnonKey"] 
                ?? throw new InvalidOperationException("Supabase Anon Key not configured");
        }

        public async Task<AuthResponse> LoginWithGoogleAsync(string idToken, string? accessToken = null)
        {
            try
            {
                _logger.LogInformation("Attempting Google OAuth login with Supabase");

                // Call Supabase Auth OAuth endpoint with Google ID token
                var body = JsonSerializer.Serialize(new
                {
                    provider = "google",
                    id_token = idToken,
                    access_token = accessToken
                });

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/auth/v1/token?grant_type=id_token")
                {
                    Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                };
                
                request.Headers.Add("apikey", _anonKey);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Supabase Google OAuth failed: {StatusCode} - {Content}", response.StatusCode, content);
                    
                    // If Supabase OAuth fails, fallback to manual Google token validation and user creation
                    return await HandleGoogleLoginFallback(idToken);
                }

                var session = JsonSerializer.Deserialize<SupabaseOAuthSession>(content, _json);
                
                if (session?.AccessToken == null || session.User?.Email == null)
                {
                    _logger.LogWarning("Invalid Supabase OAuth response");
                    return await HandleGoogleLoginFallback(idToken);
                }

                // Get user profile from profiles table
                var user = await _authService.GetProfileAsync(session.AccessToken, session.User.Id);
                
                return new AuthResponse(
                    session.AccessToken,
                    session.RefreshToken ?? "",
                    DateTimeOffset.UtcNow.AddSeconds(session.ExpiresIn ?? 3600).ToUnixTimeSeconds(),
                    new UserDto(
                        session.User.Id,
                        session.User.Email,
                        user?.FullName ?? session.User.UserMetadata?.Name ?? session.User.Email.Split('@')[0],
                        user?.PhoneNumber,
                        session.User.UserMetadata?.AvatarUrl ?? session.User.UserMetadata?.Picture,
                        user?.Role ?? "traveler",
                        user?.IsActive ?? true,
                        session.User.CreatedAt ?? DateTime.UtcNow
                    )
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Supabase Google OAuth login");
                
                // Fallback to manual handling
                return await HandleGoogleLoginFallback(idToken);
            }
        }

        private async Task<AuthResponse> HandleGoogleLoginFallback(string idToken)
        {
            try
            {
                // Validate Google token manually
                var googleUser = await ValidateGoogleTokenAsync(idToken);
                if (googleUser == null)
                {
                    throw new Exception("Invalid Google token");
                }

                // Try to login with existing account
                try
                {
                    var loginResult = await _authService.LoginAsync(googleUser.Email, "12345678");
                    _logger.LogInformation("Google user logged in with existing account: {Email}", googleUser.Email);
                    return loginResult;
                }
                catch
                {
                    // User doesn't exist, create new account
                    _logger.LogInformation("Creating new user from Google login: {Email}", googleUser.Email);
                    
                    var registerResult = await _authService.RegisterAsync(
                        email: googleUser.Email,
                        password: "12345678", // Default password for Google users
                        fullName: googleUser.Name,
                        role: "traveler", // Default role
                        phoneNumber: null,
                        experience: null,
                        specialization: null,
                        languages: null,
                        bio: null,
                        certificatePath: null
                    );
                    
                    return registerResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google login fallback failed");
                throw new Exception("Không thể đăng nhập với tài khoản Google");
            }
        }

        private async Task<GoogleUserInfo?> ValidateGoogleTokenAsync(string idToken)
        {
            try
            {
                // Validate Google ID token
                var response = await _httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Google token validation failed: {StatusCode}", response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var tokenInfo = JsonSerializer.Deserialize<GoogleTokenInfo>(content, _json);

                if (tokenInfo?.Email == null)
                {
                    _logger.LogWarning("Invalid Google token response: missing email");
                    return null;
                }

                // Validate audience (client ID)
                if (tokenInfo.Audience != _googleClientId)
                {
                    _logger.LogWarning("Invalid Google token: audience mismatch. Expected: {Expected}, Got: {Actual}", 
                        _googleClientId, tokenInfo.Audience);
                    return null;
                }

                return new GoogleUserInfo
                {
                    Email = tokenInfo.Email,
                    Name = tokenInfo.Name ?? tokenInfo.Email.Split('@')[0],
                    Picture = tokenInfo.Picture,
                    EmailVerified = tokenInfo.EmailVerified == "true"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Google token");
                return null;
            }
        }
    }


}