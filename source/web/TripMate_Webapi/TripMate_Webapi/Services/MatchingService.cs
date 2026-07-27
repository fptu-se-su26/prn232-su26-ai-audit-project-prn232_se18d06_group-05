using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TripMate_WebAPI.DTOs.Matching;
using TripMate_Webapi.Entities;
using TripMate_Webapi.Repositories;

namespace TripMate_WebAPI.Services;

public sealed class MatchingService : IMatchingService
{
    private static readonly Dictionary<string, string[]> Taxonomy = new(StringComparer.OrdinalIgnoreCase)
    {
        ["adventure"] = ["adventure", "hiking", "trekking", "outdoor", "climbing", "motorbike", "kayak", "phieu luu", "leo nui"],
        ["culture"] = ["culture", "cultural", "heritage", "tradition", "architecture", "temple", "museum", "van hoa", "pho co", "di san", "chua", "bao tang"],
        ["history"] = ["history", "historic", "ancient", "heritage", "museum", "lich su", "co kinh"],
        ["food"] = ["food", "street food", "cuisine", "culinary", "cooking", "restaurant", "market", "am thuc", "mon an", "an vat", "cho"],
        ["nature"] = ["nature", "forest", "mountain", "lake", "eco", "wildlife", "sunset", "thien nhien", "nui", "song", "hoang hon"],
        ["beach"] = ["beach", "island", "sea", "ocean", "snorkeling", "diving", "bien", "dao", "cu lao", "san ho"],
        ["nightlife"] = ["nightlife", "night", "bar", "club", "pub", "ve dem"],
        ["photography"] = ["photography", "photo", "camera", "instagram", "chup anh"],
        ["family"] = ["family", "children", "kids", "child friendly"],
        ["wellness"] = ["wellness", "relax", "spa", "massage", "chill"],
        ["local-life"] = ["local", "community", "village", "authentic", "hidden gem", "dia phuong", "lang"]
    };

    private readonly TourService _tourService;
    private readonly IGuideRepository _guideRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<MatchingService> _logger;

    public MatchingService(
        TourService tourService,
        IGuideRepository guideRepository,
        IBookingRepository bookingRepository,
        ILogger<MatchingService> logger)
    {
        _tourService = tourService;
        _guideRepository = guideRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    public async Task<MatchingResponse> FindMatchesAsync(
        MatchingRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        var startDate = request.StartDate;
        var endDate = request.EndDate ?? startDate;
        var hasDates = startDate.HasValue;
        var tripDays = hasDates
            ? endDate!.Value.DayNumber - startDate!.Value.DayNumber + 1
            : 0;

        var toursTask = _tourService.GetMatchableToursAsync();
        Task<List<GuideAvailabilityEntity>> blockedTask = hasDates
            ? _guideRepository.GetBlockedDatesInRangeAsync(
                startDate!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                endDate!.Value.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            : Task.FromResult(new List<GuideAvailabilityEntity>());
        var bookingsTask = hasDates
            ? _bookingRepository.GetConfirmedBookingsInRangeAsync(
                startDate!.Value.AddDays(-(TourSchedulePolicy.MaximumDurationDays - 1))
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                endDate!.Value.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            : Task.FromResult(new List<BookingEntity>());

        await Task.WhenAll(toursTask, blockedTask, bookingsTask);
        cancellationToken.ThrowIfCancellationRequested();

        var tours = await toursTask;
        var blockedDates = (await blockedTask)
            .GroupBy(x => x.GuideProfileId)
            .ToDictionary(
                x => x.Key,
                x => x.Select(d => d.UnavailableDate).ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);
        var confirmedByGuide = (await bookingsTask)
            .GroupBy(x => x.GuideProfileId)
            .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.Ordinal);

        var destination = NormalizeLocation(request.Destination);
        var requestedInterests = CanonicalizeInterests(
            (request.Interests ?? []).Append(request.Vibe ?? string.Empty));
        var results = new List<TourMatchResult>();

        foreach (var tour in tours)
        {
            var guide = tour.GuideProfile;
            if (guide?.Id is null || !guide.IsVerified || guide.Profile?.IsActive == false)
                continue;
            if (!tour.IsActive || !string.Equals(tour.PublicationStatus, "published", StringComparison.OrdinalIgnoreCase))
                continue;
            if (request.GuestCount > Math.Max(1, tour.MaxGroupSize))
                continue;

            var tourLocation = string.IsNullOrWhiteSpace(tour.City) ? guide.CityArea : tour.City;
            if (destination is not null && !LocationMatches(destination, tourLocation))
                continue;

            var durationDays = Math.Max(1, tour.DurationDays);
            if (hasDates && durationDays > tripDays)
                continue;

            if (hasDates)
            {
                var tourEndExclusive = startDate!.Value.AddDays(durationDays);
                if (HasBlockedDate(guide.Id, startDate.Value, tourEndExclusive, blockedDates))
                    continue;
                if (HasConfirmedConflict(
                    startDate.Value,
                    tourEndExclusive,
                    confirmedByGuide.GetValueOrDefault(guide.Id) ?? []))
                    continue;
            }

            var estimatedTotal = TourPricingCalculator.CalculateTotal(
                tour.PricePerSession,
                tour.PricePerPerson,
                tour.IncludedGuestCount,
                request.GuestCount);
            var scored = Score(
                request,
                tour,
                guide,
                tourLocation,
                estimatedTotal,
                requestedInterests,
                hasDates);

            results.Add(new TourMatchResult
            {
                PackageId = tour.Id ?? string.Empty,
                PackageTitle = tour.Title ?? "Local experience",
                Description = tour.Description,
                GuideId = guide.Id,
                UserId = guide.UserId,
                Name = guide.Profile?.FullName ?? "Local Guide",
                AvatarUrl = guide.Profile?.AvatarUrl,
                Bio = guide.Bio,
                CityArea = tourLocation,
                Specialties = guide.Specialties ?? [],
                Languages = (tour.Languages?.Count > 0 ? tour.Languages : guide.Languages) ?? [],
                CoverPhotoUrl = string.IsNullOrWhiteSpace(tour.CoverImageUrl)
                    ? guide.CoverPhotoUrl
                    : tour.CoverImageUrl,
                AverageRating = guide.AverageRating,
                TotalReviews = guide.TotalReviews,
                MatchScore = scored.Score,
                Confidence = scored.Confidence,
                MatchReasons = scored.Reasons,
                Warnings = scored.Warnings,
                PricePerSession = tour.PricePerSession,
                AdditionalGuestFee = Math.Max(0, tour.PricePerPerson ?? 0),
                IncludedGuestCount = Math.Max(1, tour.IncludedGuestCount),
                MaxGroupSize = Math.Max(1, tour.MaxGroupSize),
                EstimatedTotal = estimatedTotal,
                DurationDays = durationDays,
                DurationMinutes = tour.DurationMinutes
                    ?? (int)Math.Round(tour.DurationHours * 60m),
                AvailableDate = startDate
            });
        }

        var matches = results
            .OrderByDescending(x => x.MatchScore)
            .ThenByDescending(x => x.TotalReviews)
            .ThenBy(x => x.EstimatedTotal)
            .Take(Math.Clamp(request.Limit, 1, 20))
            .ToList();

        _logger.LogInformation(
            "Smart matching evaluated {CandidateCount} tours, kept {EligibleCount}, returned {ResultCount}. Destination={Destination}, Guests={Guests}, HasDates={HasDates}",
            tours.Count,
            results.Count,
            matches.Count,
            request.Destination,
            request.GuestCount,
            hasDates);

        return new MatchingResponse
        {
            TotalCandidates = tours.Count,
            EligibleCandidates = results.Count,
            Matches = matches
        };
    }

    private static void Validate(MatchingRequest request)
    {
        if (request.GuestCount is < 1 or > 50)
            throw new ArgumentException("Guest count must be between 1 and 50.");
        if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate < request.StartDate)
            throw new ArgumentException("End date cannot be before start date.");
        if (!request.StartDate.HasValue && request.EndDate.HasValue)
            throw new ArgumentException("Start date is required when an end date is provided.");
    }

    private static MatchScore Score(
        MatchingRequest request,
        ExperiencePackageRow tour,
        GuideProfileRow guide,
        string? tourLocation,
        decimal estimatedTotal,
        HashSet<string> requestedInterests,
        bool hasDates)
    {
        double earned = 0;
        double available = 0;
        var reasons = new List<string>();
        var warnings = new List<string>();
        var requestedSignals = 0;
        var matchedSignals = 0;

        if (!string.IsNullOrWhiteSpace(tourLocation) && !string.IsNullOrWhiteSpace(request.Destination))
        {
            requestedSignals++;
            matchedSignals++;
            reasons.Add($"Located in {tourLocation}");
        }

        if (requestedInterests.Count > 0)
        {
            requestedSignals++;
            available += 35;
            var candidateInterests = CanonicalizeInterests(GetCandidateText(tour, guide));
            var matched = requestedInterests.Intersect(candidateInterests).Order().ToList();
            earned += 35d * matched.Count / requestedInterests.Count;
            if (matched.Count > 0)
            {
                matchedSignals++;
                reasons.Add($"Matches {string.Join(", ", matched.Take(3))} interests");
            }
        }

        var budgetMax = ResolveBudgetMaximum(request);
        if (budgetMax.HasValue)
        {
            requestedSignals++;
            available += 25;
            var ratio = budgetMax.Value == 0 ? decimal.MaxValue : estimatedTotal / budgetMax.Value;
            if (ratio <= 1)
            {
                matchedSignals++;
                earned += 25;
                reasons.Add("Fits your selected budget");
            }
            else if (ratio <= 1.15m)
            {
                matchedSignals++;
                earned += 15;
                warnings.Add("Slightly above your selected budget");
            }
            else if (ratio <= 1.35m)
            {
                earned += 7;
                warnings.Add("Above your selected budget");
            }
        }

        if ((request.PreferredLanguages?.Count ?? 0) > 0)
        {
            requestedSignals++;
            available += 15;
            var availableLanguages = ((tour.Languages?.Count > 0 ? tour.Languages : guide.Languages) ?? [])
                .Select(NormalizeText)
                .ToHashSet(StringComparer.Ordinal);
            var requestedLanguages = request.PreferredLanguages!.Select(NormalizeText).ToHashSet(StringComparer.Ordinal);
            var languageMatches = requestedLanguages.Intersect(availableLanguages).ToList();
            if (languageMatches.Count > 0)
            {
                matchedSignals++;
                earned += 15;
                reasons.Add($"Available in {string.Join(", ", languageMatches)}");
            }
            else
            {
                warnings.Add("Preferred language is not listed");
            }
        }

        available += 10;
        var rating = Math.Clamp((double)guide.AverageRating, 0, 5);
        var reviewConfidence = Math.Min(1d, Math.Log10(guide.TotalReviews + 1) / Math.Log10(21));
        earned += 10 * (rating / 5d) * (0.7d + 0.3d * reviewConfidence);
        if (guide.AverageRating >= 4.5m && guide.TotalReviews > 0)
        {
            reasons.Add($"Highly rated ({guide.AverageRating:0.0} from {guide.TotalReviews} review(s))");
        }

        if (hasDates)
        {
            requestedSignals++;
            matchedSignals++;
            available += 10;
            earned += 10;
            reasons.Add($"Available for your group of {request.GuestCount}");
        }
        else
        {
            warnings.Add("Choose dates to verify guide availability");
        }

        available += 5;
        var completeness = 0d;
        if (!string.IsNullOrWhiteSpace(tour.Description)) completeness += 1;
        if (!string.IsNullOrWhiteSpace(tour.CoverImageUrl)) completeness += 1;
        if ((tour.TimelineJson?.Count ?? 0) > 0) completeness += 1;
        if ((tour.Tags?.Count ?? 0) > 0) completeness += 1;
        if (!string.IsNullOrWhiteSpace(guide.Bio)) completeness += 1;
        earned += completeness;

        var score = available <= 0 ? 0 : (int)Math.Round(earned / available * 100);
        var confidence = requestedSignals >= 4 && matchedSignals == requestedSignals
            ? "high"
            : matchedSignals >= 2
                ? "medium"
                : "low";
        return new MatchScore(Math.Clamp(score, 0, 100), confidence, reasons, warnings);
    }

    private static decimal? ResolveBudgetMaximum(MatchingRequest request)
    {
        if (request.BudgetMax is > 0) return request.BudgetMax;
        var tier = NormalizeText(request.BudgetTier);
        if (tier.Contains("budget")) return 1_000_000m;
        if (tier.Contains("standard") || tier.Contains("moderate")) return 2_000_000m;
        if (tier.Contains("premium") || tier.Contains("luxury")) return 5_000_000m;
        return null;
    }

    private static IEnumerable<string> GetCandidateText(ExperiencePackageRow tour, GuideProfileRow guide)
    {
        if (tour.Tags is not null)
            foreach (var value in tour.Tags) yield return value;
        if (guide.Specialties is not null)
            foreach (var value in guide.Specialties) yield return value;
        yield return tour.Title ?? string.Empty;
        yield return tour.Description ?? string.Empty;
        if (tour.TimelineJson is not null)
            foreach (var activity in tour.TimelineJson)
                foreach (var value in activity.Values)
                    yield return value;
    }

    private static HashSet<string> CanonicalizeInterests(IEnumerable<string> values)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var normalized = NormalizeText(value);
            foreach (var (canonical, synonyms) in Taxonomy)
            {
                if (synonyms.Any(s => normalized.Contains(NormalizeText(s), StringComparison.Ordinal)))
                    result.Add(canonical);
            }
        }
        return result;
    }

    private static bool HasBlockedDate(
        string guideId,
        DateOnly start,
        DateOnly endExclusive,
        IReadOnlyDictionary<string, HashSet<string>> blockedDates)
    {
        if (!blockedDates.TryGetValue(guideId, out var guideDates)) return false;
        for (var day = start; day < endExclusive; day = day.AddDays(1))
            if (guideDates.Contains(day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))) return true;
        return false;
    }

    private static bool HasConfirmedConflict(
        DateOnly start,
        DateOnly endExclusive,
        IReadOnlyCollection<BookingEntity> bookings)
    {
        foreach (var booking in bookings)
        {
            var existingStart = DateOnly.FromDateTime(booking.BookingDate);
            var existingEndLocal = booking.ScheduledEndAt.HasValue
                ? booking.ScheduledEndAt.Value.ToUniversalTime().AddHours(7)
                : booking.BookingDate;
            var existingEndDate = DateOnly.FromDateTime(existingEndLocal);
            var existingEndExclusive = existingEndDate.AddDays(1);
            if (existingStart < endExclusive && start < existingEndExclusive) return true;
        }
        return false;
    }

    private static string? NormalizeLocation(string? value)
    {
        var normalized = NormalizeText(value);
        if (string.IsNullOrWhiteSpace(normalized) || normalized is "vietnam" or "other") return null;
        return normalized.Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private static bool LocationMatches(string normalizedDestination, string? candidate)
    {
        var normalizedCandidate = NormalizeLocation(candidate);
        if (normalizedCandidate is null) return false;
        return normalizedCandidate.Contains(normalizedDestination, StringComparison.Ordinal) ||
               normalizedDestination.Contains(normalizedCandidate, StringComparison.Ordinal);
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var withoutMarks = new string(decomposed
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray())
            .Replace('đ', 'd');
        return Regex.Replace(withoutMarks, "[^a-z0-9]+", " ").Trim();
    }

    private sealed record MatchScore(
        int Score,
        string Confidence,
        List<string> Reasons,
        List<string> Warnings);
}
