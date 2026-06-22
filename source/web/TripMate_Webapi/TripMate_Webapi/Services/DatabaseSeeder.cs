using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace TripMate_WebAPI.Services;

public class DatabaseSeeder
{
    private readonly SupabaseAuthService _authService;
    private readonly IConfiguration _config;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly Supabase.Client _supabase;

    public DatabaseSeeder(SupabaseAuthService authService, IConfiguration config, ILogger<DatabaseSeeder> logger, Supabase.Client supabase)
    {
        _authService = authService;
        _config = config;
        _logger = logger;
        _supabase = supabase;
    }

    public async Task SeedAsync()
    {
        var autoSeed = _config.GetValue<bool>("SeedSettings:AutoSeed");
        if (!autoSeed)
        {
            _logger.LogInformation("Database seeding is disabled in configuration.");
            return;
        }

        var accounts = _config.GetSection("SeedSettings:Accounts").Get<List<SeedAccountConfig>>();
        if (accounts == null || !accounts.Any())
        {
            _logger.LogInformation("No seed accounts found in configuration.");
            return;
        }

        _logger.LogInformation("Starting database seeding for {Count} accounts...", accounts.Count);

        foreach (var account in accounts)
        {
            try
            {
                _logger.LogInformation("Seeding account: {Email} with role {Role}", account.Email, account.Role);
                var authResponse = await _authService.RegisterAsync(
                    email: account.Email,
                    password: account.Password,
                    fullName: account.FullName ?? account.Email,
                    role: account.Role ?? "traveler",
                    phoneNumber: account.Phone,
                    experience: account.Experience,
                    specialization: account.Specialization,
                    languages: account.Languages,
                    bio: account.Bio,
                    avatarUrl: account.AvatarUrl
                );
                
                if (account.Role == "guide" && authResponse?.User?.Id != null)
                {
                    await SeedGuideProfileAsync(authResponse.User.Id, account);
                }
                
                _logger.LogInformation("Successfully created account: {Email}", account.Email);
            }
            catch (Exception ex)
            {
                // If user already exists, we login and upsert/update their profile to ensure the role and details are correct!
                if (ex.Message.Contains("already_exists") || 
                    ex.Message.Contains("already registered") || 
                    ex.Message.Contains("đã được đăng ký") ||
                    ex.Message.Contains("400") ||
                    ex.Message.Contains("registered") ||
                    ex.Message.Contains("Lỗi xác thực"))
                {
                    _logger.LogInformation("Account {Email} already exists. Syncing profile role & details...", account.Email);
                    try
                    {
                        // Because Email Confirmations might be on, LoginAsync will fail. 
                        // Instead, we just fetch the existing profile directly by email to get the UserId.
                        var profileResponse = await _supabase.From<TripMate_Webapi.Entities.ProfileEntity>()
                            .Where(x => x.Email == account.Email)
                            .Get();
                        var existingProfile = profileResponse.Models.FirstOrDefault();
                        
                        if (existingProfile != null)
                        {
                            // We pass null for accessToken, which tells UpsertProfileAsync to use the ServiceRoleKey
                            await _authService.UpsertProfileAsync(
                                accessToken: null,
                                userId: existingProfile.Id,
                                email: account.Email,
                                fullName: account.FullName ?? account.Email,
                                role: account.Role ?? "traveler",
                                phoneNumber: account.Phone,
                                experience: account.Experience,
                                specialization: account.Specialization,
                                languages: account.Languages,
                                bio: account.Bio,
                                avatarUrl: account.AvatarUrl
                            );
                            
                            if (account.Role == "guide")
                            {
                                await SeedGuideProfileAsync(existingProfile.Id, account);
                            }
                            
                            _logger.LogInformation("Successfully updated/synced profile for existing account: {Email} (Role: {Role})", account.Email, account.Role);
                        }
                        else
                        {
                            _logger.LogWarning("Account {Email} exists in auth but not in profiles table. Could not sync.", account.Email);
                        }
                    }
                    catch (Exception syncEx)
                    {
                        _logger.LogError(syncEx, "Failed to sync profile for existing account {Email}", account.Email);
                    }
                }
                else
                {
                    _logger.LogError(ex, "Failed to seed account {Email}", account.Email);
                }
            }
        }
    }

    private async Task SeedGuideProfileAsync(string userId, SeedAccountConfig account)
    {
        try
        {
            var response = await _supabase.From<TripMate_Webapi.Entities.GuideProfileEntity>()
                .Where(x => x.UserId == userId)
                .Get();
                
            var existing = response.Models.FirstOrDefault();
                
            if (existing == null)
            {
                var random = new Random();
                var cities = new[] { "Hanoi", "Ho Chi Minh City", "Da Nang", "Sapa", "Hoi An" };
                
                var guideProfile = new TripMate_Webapi.Entities.GuideProfileEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Bio = account.Bio ?? "Passionate local expert ready to show you the best of Vietnam.",
                    Languages = (account.Languages ?? "English, Vietnamese").Split(',').Select(s => s.Trim()).ToList(),
                    Specialties = (account.Specialization ?? "Culture, Food").Split(',').Select(s => s.Trim()).ToList(),
                    CityArea = account.CityArea ?? cities[random.Next(cities.Length)],
                    PricePerHour = random.Next(150000, 500000), // Random price 150k - 500k
                    IsVerified = true,
                    VerifiedAt = DateTime.UtcNow,
                    AverageRating = (decimal)(4.0 + random.NextDouble()),
                    TotalReviews = random.Next(5, 50),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CoverPhotoUrl = account.CoverPhotoUrl
                };
                
                try
                {
                    await _supabase.From<TripMate_Webapi.Entities.GuideProfileEntity>().Insert(guideProfile);
                    _logger.LogInformation("Seeded GuideProfile for {Email}", account.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to insert GuideProfile using C# Client. Trying raw bypass...");
                    var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
                    var anonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");
                    var serviceKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");
                    
                    using var http = new System.Net.Http.HttpClient();
                    var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{supabaseUrl}/rest/v1/guide_profiles");
                    req.Headers.Add("apikey", anonKey);
                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", serviceKey ?? anonKey);
                    req.Headers.Add("Prefer", "return=representation");
                    
                    var body = new
                    {
                        user_id = guideProfile.UserId,
                        bio = guideProfile.Bio,
                        city_area = guideProfile.CityArea,
                        price_per_hour = guideProfile.PricePerHour,
                        is_verified = guideProfile.IsVerified,
                        verified_at = guideProfile.VerifiedAt,
                        languages = guideProfile.Languages,
                        specialties = guideProfile.Specialties,
                        hidden_gems_urls = guideProfile.HiddenGemsUrls,
                        average_rating = guideProfile.AverageRating,
                        total_reviews = guideProfile.TotalReviews,
                        cover_photo_url = guideProfile.CoverPhotoUrl
                    };
                    req.Content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
                    await http.SendAsync(req);
                    _logger.LogInformation("Seeded GuideProfile via bypass for {Email}", account.Email);
                }
            }
            else
            {
                existing.CoverPhotoUrl = account.CoverPhotoUrl;
                if (account.CityArea != null) existing.CityArea = account.CityArea;
                existing.UpdatedAt = DateTime.UtcNow;
                
                try
                {
                    // Fallback to raw HTTP PATCH bypass for updating to avoid RLS issues
                    using (var http = new System.Net.Http.HttpClient())
                    {
                        var baseUrl = _config.GetValue<string>("Supabase:Url") ?? Environment.GetEnvironmentVariable("SUPABASE_URL");
                        var serviceKey = _config.GetValue<string>("Supabase:ServiceRoleKey") ?? Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");
                        var anonKey = _config.GetValue<string>("Supabase:AnonKey") ?? Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");
                        var tokenToUse = string.IsNullOrEmpty(serviceKey) ? anonKey : serviceKey;
                        
                        var req = new System.Net.Http.HttpRequestMessage(new System.Net.Http.HttpMethod("PATCH"), $"{baseUrl}/rest/v1/guide_profiles?id=eq.{existing.Id}");
                        req.Headers.Add("apikey", tokenToUse);
                        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenToUse);
                        req.Headers.Add("Prefer", "return=minimal");
                        
                        var body = new {
                            cover_photo_url = existing.CoverPhotoUrl,
                            city_area = existing.CityArea,
                            updated_at = existing.UpdatedAt
                        };
                        req.Content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
                        
                        var patchRes = await http.SendAsync(req);
                        if (!patchRes.IsSuccessStatusCode) 
                        {
                            var errContent = await patchRes.Content.ReadAsStringAsync();
                            throw new Exception($"Supabase PATCH failed: {patchRes.StatusCode} - {errContent}");
                        }
                    }
                    _logger.LogInformation("Updated GuideProfile CoverPhoto and CityArea for {Email} via bypass", account.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update GuideProfile for {Email}", account.Email);
                }
            }
            
            // Re-fetch the guide profile to get its ID
            response = await _supabase.From<TripMate_Webapi.Entities.GuideProfileEntity>()
                .Where(x => x.UserId == userId)
                .Get();
            var currentGuide = response.Models.FirstOrDefault();
            
            if (currentGuide != null)
            {
                // Seed an experience package for this guide if they don't have one
                var pkgResponse = await _supabase.From<TripMate_Webapi.Entities.ExperiencePackageEntity>()
                    .Where(x => x.GuideProfileId == currentGuide.Id)
                    .Get();
                
                if (pkgResponse.Models == null || !pkgResponse.Models.Any())
                {
                    var newPkg = new TripMate_Webapi.Entities.ExperiencePackageEntity
                    {
                        Id = Guid.NewGuid().ToString(),
                        GuideProfileId = currentGuide.Id,
                        Title = "City Discovery Tour",
                        Description = "Explore the best hidden gems and popular spots in the city with a local expert.",
                        DurationHours = 4,
                        PricePerSession = 500000,
                        PricePerPerson = null,
                        MaxGroupSize = 5,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    try
                    {
                        await _supabase.From<TripMate_Webapi.Entities.ExperiencePackageEntity>().Insert(newPkg);
                        _logger.LogInformation("Seeded ExperiencePackage for {Email}", account.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to insert ExperiencePackage for {Email}. Using raw HttpClient bypass...", account.Email);
                        
                        var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
                        var anonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");
                        
                        // Using raw bypass just like bookings, but for packages
                        using var http = new System.Net.Http.HttpClient();
                        var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{supabaseUrl}/rest/v1/experience_packages");
                        req.Headers.Add("apikey", anonKey);
                        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", anonKey);
                        req.Headers.Add("Prefer", "return=representation");
                        
                        var body = new
                        {
                            id = newPkg.Id,
                            guide_profile_id = newPkg.GuideProfileId,
                            title = newPkg.Title,
                            description = newPkg.Description,
                            duration_hours = newPkg.DurationHours,
                            price_per_session = newPkg.PricePerSession,
                            max_group_size = newPkg.MaxGroupSize,
                            is_active = newPkg.IsActive
                        };
                        
                        req.Content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
                        await http.SendAsync(req);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed guide profile for {Email}", account.Email);
        }
    }
}
