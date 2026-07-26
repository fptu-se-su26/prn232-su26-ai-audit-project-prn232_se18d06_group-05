using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TripMate_WebAPI.Services;

/// <summary>
/// Background service that periodically scans for completed bookings that are older than 3 days (72 hours),
/// and automatically releases the escrow payment to the guide if there are no disputes or bad reviews.
/// </summary>
public sealed class EscrowReleaseWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EscrowReleaseWorker> _logger;

    public EscrowReleaseWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<EscrowReleaseWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let startup complete
        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        
        // Run every 10 minutes in background
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var adminService = scope.ServiceProvider.GetRequiredService<AdminService>();
                
                _logger.LogInformation("[EscrowReleaseWorker] Scanning for completed bookings eligible for automatic release...");
                
                // Fetch all bookings
                var bookings = await adminService.GetBookingsAsync();
                
                // Filter bookings that:
                // 1. Are Completed (status == 2)
                // 2. Escrow is NOT released yet
                // 3. Payout status is 'eligible' (or failed, to retry)
                // 4. completion_state is 'confirmed' (both agreed it's completed)
                // 5. completed at least 3 days (72 hours) ago
                var eligibleIds = new List<string>();
                var now = DateTime.UtcNow;
                
                foreach (var b in bookings)
                {
                    if (b.Id != null &&
                        b.Status == 2 &&
                        !b.EscrowReleased &&
                        (string.Equals(b.PayoutStatus, "eligible", StringComparison.OrdinalIgnoreCase) || 
                         string.Equals(b.PayoutStatus, "failed", StringComparison.OrdinalIgnoreCase)) &&
                        string.Equals(b.CompletionState, "confirmed", StringComparison.OrdinalIgnoreCase) &&
                        b.TravelerCompletedAt.HasValue)
                    {
                        var completedTime = b.TravelerCompletedAt.Value;
                        var hoursPassed = (now - completedTime).TotalHours;
                        
                        if (hoursPassed >= 72) // 3 days
                        {
                            // Check traveler review rating
                            var rating = b.Reviews?.Rating;
                            if (rating.HasValue && rating.Value <= 2)
                            {
                                // Has bad feedback, skip auto-release. Admin must review manually.
                                _logger.LogWarning("[EscrowReleaseWorker] Booking {BookingId} has bad feedback ({Rating} stars). Skipping auto-release for manual review.", b.Id, rating.Value);
                                continue;
                            }
                            
                            eligibleIds.Add(b.Id);
                        }
                    }
                }
                
                if (eligibleIds.Any())
                {
                    _logger.LogInformation("[EscrowReleaseWorker] Found {Count} bookings eligible for automatic escrow release. Releasing...", eligibleIds.Count);
                    var success = await adminService.ReleaseEscrowBulkAsync(eligibleIds);
                    if (success)
                    {
                        _logger.LogInformation("[EscrowReleaseWorker] Successfully auto-released escrow for {Count} bookings.", eligibleIds.Count);
                    }
                    else
                    {
                        _logger.LogError("[EscrowReleaseWorker] Failed to auto-release escrow for some bookings.");
                    }
                }
                else
                {
                    _logger.LogInformation("[EscrowReleaseWorker] No completed bookings are currently eligible for auto-release.");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EscrowReleaseWorker] Scan failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
