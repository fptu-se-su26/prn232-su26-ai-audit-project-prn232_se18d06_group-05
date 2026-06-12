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
                        var loginResult = await _authService.LoginAsync(account.Email, account.Password);
                        if (loginResult != null && loginResult.User != null)
                        {
                            await _authService.UpsertProfileAsync(
                                accessToken: loginResult.AccessToken,
                                userId: loginResult.User.Id,
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
                            
                            if (account.Role == "guide" && loginResult.User?.Id != null)
                            {
                                await SeedGuideProfileAsync(loginResult.User.Id, account);
                            }
                            
                            _logger.LogInformation("Successfully updated/synced profile for existing account: {Email} (Role: {Role})", account.Email, account.Role);
                        }
                    }
                    catch (Exception loginEx)
                    {
                        _logger.LogError(loginEx, "Failed to login and update profile for existing account {Email}", account.Email);
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
                    CityArea = cities[random.Next(cities.Length)],
                    PricePerHour = random.Next(150000, 500000), // Random price 150k - 500k
                    IsVerified = true,
                    VerifiedAt = DateTime.UtcNow,
                    AverageRating = (decimal)(4.0 + random.NextDouble()),
                    TotalReviews = random.Next(5, 50),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CoverPhotoUrl = account.CoverPhotoUrl
                };
                
                await _supabase.From<TripMate_Webapi.Entities.GuideProfileEntity>().Insert(guideProfile);
                _logger.LogInformation("Seeded GuideProfile for {Email}", account.Email);
            }
            else
            {
                existing.CoverPhotoUrl = account.CoverPhotoUrl;
                existing.UpdatedAt = DateTime.UtcNow;
                await _supabase.From<TripMate_Webapi.Entities.GuideProfileEntity>().Update(existing);
                _logger.LogInformation("Updated GuideProfile CoverPhoto for {Email}", account.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed guide profile for {Email}", account.Email);
        }
    }
}
