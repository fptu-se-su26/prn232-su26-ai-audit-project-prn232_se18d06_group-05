using TripMate_WebAPI.DTOs.Tour.Scheduling;

namespace TripMate_WebAPI.Services;

/// <summary>
/// Produces consistent, compact Guide-facing schedule labels.
/// </summary>
public static class TourScheduleFormatter
{
    public static string FormatDuration(TourScheduleDto schedule)
        => FormatDuration(schedule.DurationType, schedule.DurationDays, schedule.DurationMinutes);

    public static string FormatDuration(
        string? durationType,
        int durationDays,
        int? durationMinutes)
    {
        if (TourSchedulePolicy.NormalizeDurationType(durationType) == TourDurationTypes.MultiDay)
            return $"{Math.Max(2, durationDays)} days";

        var minutes = Math.Max(0, durationMinutes ?? 0);
        if (minutes < 60) return $"{minutes} min";

        var hours = minutes / 60;
        var remainingMinutes = minutes % 60;
        if (remainingMinutes == 0)
            return $"{hours} {(hours == 1 ? "hour" : "hours")}";

        return $"{hours} hr {remainingMinutes} min";
    }
}
