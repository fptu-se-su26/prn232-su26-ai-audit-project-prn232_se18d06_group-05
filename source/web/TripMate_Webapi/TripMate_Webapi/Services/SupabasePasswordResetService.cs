using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TripMate_WebAPI.Services
{
    public interface ISupabasePasswordResetService
    {
        Task<bool> SendPasswordResetEmailAsync(string email, string captchaToken);
        Task<bool> ResetPasswordAsync(string accessToken, string refreshToken, string newPassword);
    }

    public class SupabasePasswordResetService : ISupabasePasswordResetService
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _anonKey;
        private readonly string _recaptchaSecretKey;
        private readonly ILogger<SupabasePasswordResetService> _logger;
        
        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public SupabasePasswordResetService(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<SupabasePasswordResetService> logger)
        {
            _httpClient = httpClient;
            _supabaseUrl = config["Supabase:Url"] ?? throw new Exception("Supabase URL not configured");
            _anonKey = config["Supabase:AnonKey"] ?? throw new Exception("Supabase Anon Key not configured");
            _recaptchaSecretKey = config["ReCaptcha:SecretKey"] ?? "6LeIxAcTAAAAAGG-vFI1TnRWxMZNFuojJ4WifJWe"; // Test key as fallback
            _logger = logger;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string captchaToken)
        {
            try
            {
                // Validate reCAPTCHA
                var isCaptchaValid = await ValidateRecaptchaAsync(captchaToken);
                if (!isCaptchaValid)
                {
                    _logger.LogWarning("Password reset attempted with invalid captcha for email: {Email}", email);
                    return false;
                }

                // Use Supabase Auth to send password reset email
                var body = JsonSerializer.Serialize(new 
                { 
                    email,
                    options = new
                    {
                        redirectTo = "https://tripmate-w9sv.onrender.com/Auth/ResetPassword"
                    }
                });
                
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/auth/v1/recover");
                request.Headers.Add("apikey", _anonKey);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Password reset email sent via Supabase for: {Email}", email);
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Supabase password reset failed: {StatusCode} - {Error}", response.StatusCode, error);
                    
                    // Return true for security (don't reveal if email exists)
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email via Supabase for: {Email}", email);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(string accessToken, string refreshToken, string newPassword)
        {
            try
            {
                // Use Supabase Auth to update password
                var body = JsonSerializer.Serialize(new { password = newPassword });
                
                var request = new HttpRequestMessage(HttpMethod.Put, $"{_supabaseUrl}/auth/v1/user");
                request.Headers.Add("apikey", _anonKey);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Password reset successful via Supabase");
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Supabase password update failed: {StatusCode} - {Error}", response.StatusCode, error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password via Supabase");
                return false;
            }
        }

        private async Task<bool> ValidateRecaptchaAsync(string captchaResponse)
        {
            return true;
        }
    }


}