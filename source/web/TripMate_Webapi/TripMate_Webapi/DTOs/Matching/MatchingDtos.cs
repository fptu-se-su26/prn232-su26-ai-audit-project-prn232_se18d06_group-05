using System.ComponentModel.DataAnnotations;

namespace TripMate_WebAPI.DTOs.Matching;

public sealed class MatchingRequest
{
    [StringLength(100)]
    public string? Destination { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [Range(1, 50)]
    public int GuestCount { get; set; } = 1;

    [StringLength(100)]
    public string? Vibe { get; set; }

    [StringLength(50)]
    public string? BudgetTier { get; set; }

    [Range(0, 1_000_000_000)]
    public decimal? BudgetMax { get; set; }

    public List<string> Interests { get; set; } = [];

    public List<string> PreferredLanguages { get; set; } = [];

    [Range(1, 20)]
    public int Limit { get; set; } = 6;
}

public sealed class MatchingResponse
{
    public bool Success { get; init; } = true;
    public int TotalCandidates { get; init; }
    public int EligibleCandidates { get; init; }
    public IReadOnlyList<TourMatchResult> Matches { get; init; } = [];
}

public sealed class TourMatchResult
{
    public string PackageId { get; init; } = string.Empty;
    public string PackageTitle { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string GuideId { get; init; } = string.Empty;
    public string? UserId { get; init; }
    public string Name { get; init; } = "Local Guide";
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public string? CityArea { get; init; }
    public List<string> Specialties { get; init; } = [];
    public List<string> Languages { get; init; } = [];
    public string? CoverPhotoUrl { get; init; }
    public decimal AverageRating { get; init; }
    public int TotalReviews { get; init; }
    public int MatchScore { get; init; }
    public string Confidence { get; init; } = "low";
    public List<string> MatchReasons { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public decimal PricePerSession { get; init; }
    public decimal AdditionalGuestFee { get; init; }
    public int IncludedGuestCount { get; init; }
    public int MaxGroupSize { get; init; }
    public decimal EstimatedTotal { get; init; }
    public int DurationDays { get; init; } = 1;
    public int DurationMinutes { get; init; }
    public DateOnly? AvailableDate { get; init; }
}
