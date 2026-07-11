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
                else if (ex.Message.Contains("yêu cầu xác thực email") || ex.Message.Contains("Đăng ký thành công"))
                {
                    _logger.LogInformation("Successfully created account: {Email} (Requires email confirmation)", account.Email);
                }
                else
                {
                    _logger.LogError(ex, "Failed to seed account {Email}", account.Email);
                }
            }
        }

        try
        {
            await SeedBookingsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding bookings at startup");
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
                    IsVerified = account.Email != "guide2@tripmate.com" && account.Email != "minhtuan2@tripmate.com",
                    VerifiedAt = (account.Email != "guide2@tripmate.com" && account.Email != "minhtuan2@tripmate.com") ? (DateTime?)DateTime.UtcNow : null,
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

            // M4: Đảm bảo gói Custom Itinerary (00000000-0000-0000-0000-000000000000) luôn tồn tại
            await EnsureCustomPackageExistsAsync();
            
            _logger.LogInformation("Database seeding completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }

    private async Task EnsureCustomPackageExistsAsync()
    {
        try
        {
            var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var anonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");
            using var http = new System.Net.Http.HttpClient();

            // Check if it exists
            var checkReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"{supabaseUrl}/rest/v1/experience_packages?id=eq.00000000-0000-0000-0000-000000000000");
            checkReq.Headers.Add("apikey", anonKey);
            checkReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", anonKey);
            var checkRes = await http.SendAsync(checkReq);
            var content = await checkRes.Content.ReadAsStringAsync();
            
            if (content == "[]")
            {
                // Find any guide id
                var guideReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"{supabaseUrl}/rest/v1/guide_profiles?limit=1");
                guideReq.Headers.Add("apikey", anonKey);
                guideReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", anonKey);
                var guideRes = await http.SendAsync(guideReq);
                var guideContent = await guideRes.Content.ReadAsStringAsync();
                var guides = System.Text.Json.JsonSerializer.Deserialize<List<TripMate_Webapi.Entities.GuideProfileEntity>>(guideContent);
                
                if (guides != null && guides.Any())
                {
                    var guideId = guides.First().Id;
                    var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{supabaseUrl}/rest/v1/experience_packages");
                    req.Headers.Add("apikey", anonKey);
                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", anonKey);
                    req.Headers.Add("Prefer", "return=representation");
                    
                    var body = new
                    {
                        id = "00000000-0000-0000-0000-000000000000",
                        guide_profile_id = guideId,
                        title = "Custom Itinerary",
                        description = "A personalized tour based on your preferences.",
                        duration_hours = 4,
                        price_per_session = 0,
                        price_per_person = 500000,
                        max_group_size = 10,
                        is_active = true,
                        created_at = DateTime.UtcNow
                    };
                    
                    req.Content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
                    await http.SendAsync(req);
                    _logger.LogInformation("Seeded dummy Custom Package 000...000 successfully.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to ensure custom package exists.");
        }
    }

    private async Task SeedBookingsAsync()
    {
        try
        {
            var supabaseUrl = _config.GetValue<string>("Supabase:Url");
            var serviceKey = _config.GetValue<string>("Supabase:ServiceRoleKey");
            var anonKey = _config.GetValue<string>("Supabase:AnonKey");
            var tokenToUse = string.IsNullOrEmpty(serviceKey) ? anonKey : serviceKey;

            _logger.LogInformation("SUPABASE CONFIG - URL: '{Url}', ServiceKey Length: {ServiceKeyLength}, AnonKey Length: {AnonKeyLength}", 
                supabaseUrl, 
                serviceKey?.Length ?? 0, 
                anonKey?.Length ?? 0);

            using var http = new System.Net.Http.HttpClient();
            
            // 1. Check if bookings table is empty
            var checkReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"{supabaseUrl}/rest/v1/bookings?select=id&limit=1");
            checkReq.Headers.Add("apikey", tokenToUse);
            checkReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenToUse);
            var checkRes = await http.SendAsync(checkReq);
            if (checkRes.IsSuccessStatusCode)
            {
                var checkContent = await checkRes.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(checkContent);
                if (doc.RootElement.GetArrayLength() > 0)
                {
                    _logger.LogInformation("Bookings table is not empty, skipping bookings seed.");
                    return;
                }
            }

            _logger.LogInformation("Bookings table is empty. Seeding mock bookings...");

            // 2. Get any traveler profile ID
            var travReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"{supabaseUrl}/rest/v1/profiles?role=eq.traveler&select=id&limit=1");
            travReq.Headers.Add("apikey", tokenToUse);
            travReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenToUse);
            var travRes = await http.SendAsync(travReq);
            if (!travRes.IsSuccessStatusCode)
            {
                var errContent = await travRes.Content.ReadAsStringAsync();
                _logger.LogInformation("Failed to query traveler. Status: {Status}, Error: {Error}", travRes.StatusCode, errContent);
                return;
            }
            var travContent = await travRes.Content.ReadAsStringAsync();
            using var travDoc = System.Text.Json.JsonDocument.Parse(travContent);
            if (travDoc.RootElement.GetArrayLength() == 0)
            {
                _logger.LogInformation("No travelers found in database to seed bookings.");
                return;
            }
            var travelerId = travDoc.RootElement[0].GetProperty("id").GetString();
            if (string.IsNullOrEmpty(travelerId)) return;

            // 3. Get all guide profiles
            var guideReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"{supabaseUrl}/rest/v1/guide_profiles?select=id");
            guideReq.Headers.Add("apikey", tokenToUse);
            guideReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenToUse);
            var guideRes = await http.SendAsync(guideReq);
            if (!guideRes.IsSuccessStatusCode)
            {
                var errContent = await guideRes.Content.ReadAsStringAsync();
                _logger.LogInformation("Failed to query guide profiles. Status: {Status}, Error: {Error}", guideRes.StatusCode, errContent);
                return;
            }
            var guideContent = await guideRes.Content.ReadAsStringAsync();
            using var guideDoc = System.Text.Json.JsonDocument.Parse(guideContent);
            var guideProfiles = guideDoc.RootElement;
            if (guideProfiles.GetArrayLength() == 0)
            {
                _logger.LogInformation("No guide profiles found in database to seed bookings.");
                return;
            }

            var random = new Random();
            int createdCount = 0;
            for (int i = 0; i < Math.Min(guideProfiles.GetArrayLength(), 3); i++)
            {
                var guideId = guideProfiles[i].GetProperty("id").GetString();
                if (string.IsNullOrEmpty(guideId)) continue;
                _logger.LogInformation("Processing guide {GuideId} for bookings seeding...", guideId);

                // 4. Get experience package for this guide
                var pkgReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"{supabaseUrl}/rest/v1/experience_packages?guide_profile_id=eq.{guideId}&select=id,price_per_session&limit=1");
                pkgReq.Headers.Add("apikey", tokenToUse);
                pkgReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenToUse);
                var pkgRes = await http.SendAsync(pkgReq);
                if (!pkgRes.IsSuccessStatusCode)
                {
                    var errContent = await pkgRes.Content.ReadAsStringAsync();
                    _logger.LogInformation("Failed to query experience package for guide {GuideId}. Status: {Status}, Error: {Error}", guideId, pkgRes.StatusCode, errContent);
                    continue;
                }
                var pkgContent = await pkgRes.Content.ReadAsStringAsync();
                using var pkgDoc = System.Text.Json.JsonDocument.Parse(pkgContent);
                string pkgId;
                decimal pricePerSession = 500000;
                if (pkgDoc.RootElement.GetArrayLength() == 0)
                {
                    _logger.LogInformation("Guide {GuideId} has no experience packages. Seeding a mock package first...", guideId);
                    pkgId = Guid.NewGuid().ToString();
                    
                    var pkgPostReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{supabaseUrl}/rest/v1/experience_packages");
                    pkgPostReq.Headers.Add("apikey", tokenToUse);
                    pkgPostReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenToUse);
                    pkgPostReq.Headers.Add("Prefer", "return=minimal");
                    
                    var pkgBody = new
                    {
                        id = pkgId,
                        guide_profile_id = guideId,
                        title = "City Discovery Tour",
                        description = "Explore the best hidden gems and popular spots in the city with a local expert.",
                        duration_hours = 4.0m,
                        price_per_session = pricePerSession,
                        max_group_size = 5,
                        is_active = true,
                        created_at = DateTime.UtcNow.ToString("o")
                    };
                    
                    pkgPostReq.Content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(pkgBody), System.Text.Encoding.UTF8, "application/json");
                    var pkgPostRes = await http.SendAsync(pkgPostReq);
                    if (!pkgPostRes.IsSuccessStatusCode)
                    {
                        var pkgPostErr = await pkgPostRes.Content.ReadAsStringAsync();
                        _logger.LogInformation("Failed to seed mock experience package for Guide {GuideId}: {Error}", guideId, pkgPostErr);
                        continue;
                    }
                    _logger.LogInformation("Successfully seeded mock experience package for Guide {GuideId}.", guideId);
                }
                else
                {
                    pkgId = pkgDoc.RootElement[0].GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
                    var priceVal = pkgDoc.RootElement[0].GetProperty("price_per_session");
                    pricePerSession = priceVal.ValueKind == System.Text.Json.JsonValueKind.Number ? priceVal.GetDecimal() : 500000;
                }
                
                decimal totalAmount = pricePerSession;
                
                var platformFee = totalAmount * 0.15m;
                var guideEarnings = totalAmount - platformFee;
                var status = random.Next(0, 4); // 0=Pending, 1=Confirmed, 2=Completed, 3=Cancelled

                // 5. Post to bookings
                var postReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{supabaseUrl}/rest/v1/bookings");
                postReq.Headers.Add("apikey", tokenToUse);
                postReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenToUse);
                postReq.Headers.Add("Prefer", "return=minimal");

                var body = new
                {
                    id = Guid.NewGuid().ToString(),
                    traveler_id = travelerId,
                    guide_profile_id = guideId,
                    experience_package_id = pkgId,
                    booking_date = DateTime.UtcNow.AddDays(random.Next(1, 10)).ToString("o"),
                    start_time = DateTime.UtcNow.AddHours(random.Next(1, 8)).ToString("HH:mm:ss"),
                    guest_count = random.Next(1, 4),
                    total_amount = totalAmount,
                    platform_fee = platformFee,
                    guide_earnings = guideEarnings,
                    status = status,
                    escrow_released = status == 2,
                    cancel_reason = status == 3 ? "Change of plans" : null
                };

                postReq.Content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
                var postRes = await http.SendAsync(postReq);
                if (postRes.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Seeded Booking via bypass for Guide {GuideId} with status {Status}", guideId, status);
                    createdCount++;
                }
                else
                {
                    var err = await postRes.Content.ReadAsStringAsync();
                    _logger.LogInformation("Failed to seed Booking via bypass: {Error}", err);
                }
            }
            _logger.LogInformation("Bookings seeding complete. Created {Count} bookings.", createdCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed bookings via bypass");
        }
    }
}
