using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TripMate_WebAPI.Models;

namespace TripMate_WebAPI.Services
{
    public interface IPasswordResetService
    {
        Task<bool> SendPasswordResetEmailAsync(string email, string captchaToken);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    }

    public class PasswordResetService : IPasswordResetService
    {
        private readonly HttpClient _httpClient;
        private readonly IEmailService _emailService;
        private readonly string _supabaseUrl;
        private readonly string _anonKey;
        private readonly string _serviceRoleKey;
        private readonly string _recaptchaSecretKey;
        private readonly ILogger<PasswordResetService> _logger;
        
        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public PasswordResetService(
            HttpClient httpClient,
            IEmailService emailService,
            IConfiguration config,
            ILogger<PasswordResetService> logger)
        {
            _httpClient = httpClient;
            _emailService = emailService;
            _supabaseUrl = config["Supabase:Url"] ?? throw new Exception("Supabase URL not configured");
            _anonKey = config["Supabase:AnonKey"] ?? throw new Exception("Supabase Anon Key not configured");
            _serviceRoleKey = config["Supabase:ServiceRoleKey"] ?? throw new Exception("Supabase Service Role Key not configured");
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

                // Check if user exists
                var userExists = await CheckUserExistsAsync(email);
                if (!userExists)
                {
                    _logger.LogWarning("Password reset attempted for non-existent email: {Email}", email);
                    // Return true for security (don't reveal if email exists)
                    return true;
                }

                // Generate reset token (simple implementation)
                var resetToken = Guid.NewGuid().ToString("N");
                
                // Store reset token with expiration (24 hours)
                await StoreResetTokenAsync(email, resetToken);

                // Send email with reset link
                // You can change this to https://tripmate-w9sv.onrender.com for production
                var resetLink = $"https://tripmate-w9sv.onrender.com/Auth/ResetPassword?email={Uri.EscapeDataString(email)}&token={resetToken}";
                await _emailService.SendPasswordResetEmailAsync(email, resetLink);

                _logger.LogInformation("Password reset email sent to: {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to: {Email}", email);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            try
            {
                // Validate reset token
                var isValidToken = await ValidateResetTokenAsync(email, token);
                if (!isValidToken)
                {
                    _logger.LogWarning("Invalid reset token for email: {Email}", email);
                    return false;
                }

                // Get User ID from email using Admin API or profiles table
                var userId = await GetUserIdByEmailAsync(email);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found for email: {Email}", email);
                    return false;
                }

                // Reset password via Supabase Admin API
                var updateData = new
                {
                    password = newPassword
                };

                var request = new HttpRequestMessage(HttpMethod.Put, $"{_supabaseUrl}/auth/v1/admin/users/{userId}");
                request.Headers.Add("apikey", _serviceRoleKey);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(updateData),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    // Clear the reset token
                    await ClearResetTokenAsync(email, token);
                    _logger.LogInformation("Password reset successful for: {Email}", email);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for: {Email}", email);
                return false;
            }
        }

        private async Task<string?> GetUserIdByEmailAsync(string email)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{_supabaseUrl}/rest/v1/profiles?email=eq.{Uri.EscapeDataString(email)}&select=id");

                request.Headers.Add("apikey", _anonKey);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var profiles = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, _json);
                    if (profiles != null && profiles.Count > 0 && profiles[0].TryGetValue("id", out var idObj))
                    {
                        return idObj?.ToString();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user id for email: {Email}", email);
                return null;
            }
        }

        private async Task<bool> CheckUserExistsAsync(string email)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{_supabaseUrl}/rest/v1/profiles?email=eq.{Uri.EscapeDataString(email)}&select=id");

                request.Headers.Add("apikey", _anonKey);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var profiles = JsonSerializer.Deserialize<List<object>>(content, _json);
                    return profiles?.Count > 0;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user exists: {Email}", email);
                return false;
            }
        }

        private async Task StoreResetTokenAsync(string email, string token)
        {
            try
            {
                var tokenData = new
                {
                    id = Guid.NewGuid(),
                    email = email,
                    token = token,
                    expires_at = DateTime.UtcNow.AddHours(24),
                    created_at = DateTime.UtcNow,
                    used = false
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/password_reset_tokens");
                request.Headers.Add("apikey", _anonKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(tokenData),
                    Encoding.UTF8,
                    "application/json");

                await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing reset token");
            }
        }

        private async Task<bool> ValidateResetTokenAsync(string email, string token)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{_supabaseUrl}/rest/v1/password_reset_tokens?email=eq.{Uri.EscapeDataString(email)}&token=eq.{token}&used=eq.false&expires_at=gt.{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");

                request.Headers.Add("apikey", _anonKey);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokens = JsonSerializer.Deserialize<List<object>>(content, _json);
                    return tokens?.Count > 0;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating reset token");
                return false;
            }
        }

        private async Task ClearResetTokenAsync(string email, string token)
        {
            try
            {
                var updateData = new { used = true };

                var request = new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"{_supabaseUrl}/rest/v1/password_reset_tokens?email=eq.{Uri.EscapeDataString(email)}&token=eq.{token}");

                request.Headers.Add("apikey", _anonKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(updateData),
                    Encoding.UTF8,
                    "application/json");

                await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing reset token");
            }
        }

        private async Task<bool> ValidateRecaptchaAsync(string captchaResponse)
        {
            try
            {
                // Skip validation for test tokens
                if (captchaResponse == "test-captcha-response" || string.IsNullOrWhiteSpace(_recaptchaSecretKey))
                {
                    return true;
                }

                var parameters = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("secret", _recaptchaSecretKey),
                    new KeyValuePair<string, string>("response", captchaResponse)
                });

                var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", parameters);
                var content = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<RecaptchaVerifyResponse>(content, _json);
                
                if (result?.Success == true)
                {
                    _logger.LogInformation("reCAPTCHA validation successful");
                    return true;
                }
                else
                {
                    _logger.LogWarning("reCAPTCHA validation failed: {Errors}", string.Join(", ", result?.ErrorCodes ?? new string[0]));
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating reCAPTCHA");
                return false;
            }
        }
    }

    public class RecaptchaVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTimestamp { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }
    }
}