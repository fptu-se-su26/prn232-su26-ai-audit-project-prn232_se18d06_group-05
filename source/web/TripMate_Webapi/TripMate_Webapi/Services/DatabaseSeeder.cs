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

    public DatabaseSeeder(SupabaseAuthService authService, IConfiguration config, ILogger<DatabaseSeeder> logger)
    {
        _authService = authService;
        _config = config;
        _logger = logger;
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
                await _authService.RegisterAsync(
                    email: account.Email,
                    password: account.Password,
                    fullName: account.FullName ?? account.Email,
                    role: account.Role ?? "traveler",
                    phoneNumber: account.Phone,
                    experience: account.Experience,
                    specialization: account.Specialization,
                    languages: account.Languages,
                    bio: account.Bio
                );
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
                                bio: account.Bio
                            );
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
}

