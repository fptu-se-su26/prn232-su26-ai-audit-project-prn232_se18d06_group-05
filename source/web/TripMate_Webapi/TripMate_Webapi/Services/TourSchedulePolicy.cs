using System.Globalization;
using TripMate_WebAPI.DTOs.Tour.Scheduling;

namespace TripMate_WebAPI.Services;

/// <summary>
/// Central schedule rules shared by Guide create, edit, quality, and calendar flows.
/// Persistence and Razor views must not duplicate these calculations.
/// </summary>
public static class TourSchedulePolicy
{
    public const string DefaultTimeZone = "Asia/Ho_Chi_Minh";
    public const int MinimumDurationMinutes = 30;
    public const int MaximumDurationDays = 30;

    private static readonly string[] SupportedClockFormats = ["H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss"];

    public static IReadOnlyList<string> ValidateForPublish(TourScheduleDto? schedule)
    {
        var errors = new List<string>();
        if (schedule == null)
        {
            errors.Add("Add a tour schedule before publishing.");
            return errors;
        }

        var durationType = NormalizeDurationType(schedule.DurationType);
        if (durationType is not TourDurationTypes.SameDay and not TourDurationTypes.MultiDay)
            errors.Add("Choose either a same-day or multi-day tour.");

        if (schedule.DurationDays < 1 || schedule.DurationDays > MaximumDurationDays)
            errors.Add($"Tour duration must be between 1 and {MaximumDurationDays} days.");
        else if (durationType == TourDurationTypes.SameDay && schedule.DurationDays != 1)
            errors.Add("A same-day tour must use exactly one itinerary day.");
        else if (durationType == TourDurationTypes.MultiDay && schedule.DurationDays < 2)
            errors.Add("A multi-day tour must use at least two itinerary days.");

        var hasStart = TryParseClockTime(schedule.DefaultStartTime, out var start);
        var hasEnd = TryParseClockTime(schedule.DefaultEndTime, out var end);
        if (!hasStart) errors.Add("Add a valid default start time.");
        if (!hasEnd) errors.Add("Add a valid default end time.");

        if (hasStart && hasEnd &&
            durationType is TourDurationTypes.SameDay or TourDurationTypes.MultiDay)
        {
            var calculatedMinutes = CalculateElapsedMinutes(durationType, schedule.DurationDays, start, end);
            if (calculatedMinutes < MinimumDurationMinutes)
                errors.Add($"Tour duration must be at least {MinimumDurationMinutes} minutes.");
            else if (schedule.DurationMinutes.HasValue && schedule.DurationMinutes != calculatedMinutes)
                errors.Add("The saved duration does not match the selected start and end times.");
        }

        var timeZone = string.IsNullOrWhiteSpace(schedule.TimeZone)
            ? DefaultTimeZone
            : schedule.TimeZone.Trim();
        if (!IsSupportedTimeZone(timeZone))
            errors.Add("Choose a valid IANA time zone.");

        return errors;
    }

    public static string NormalizeDurationType(string? value)
        => (value ?? string.Empty).Trim().ToLowerInvariant();

    public static bool TryParseClockTime(string? value, out TimeOnly parsed)
    {
        return TimeOnly.TryParseExact(
            value?.Trim(),
            SupportedClockFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out parsed);
    }

    public static string? NormalizeClockTime(string? value)
        => TryParseClockTime(value, out var parsed)
            ? parsed.ToString("HH:mm", CultureInfo.InvariantCulture)
            : null;

    public static int? CalculateElapsedMinutes(TourScheduleDto? schedule)
    {
        if (schedule == null ||
            !TryParseClockTime(schedule.DefaultStartTime, out var start) ||
            !TryParseClockTime(schedule.DefaultEndTime, out var end))
        {
            return null;
        }

        return CalculateElapsedMinutes(
            NormalizeDurationType(schedule.DurationType),
            schedule.DurationDays,
            start,
            end);
    }

    public static decimal ToLegacyDurationHours(TourScheduleDto schedule)
    {
        var minutes = CalculateElapsedMinutes(schedule) ?? schedule.DurationMinutes;
        if (!minutes.HasValue || minutes.Value <= 0)
            throw new ArgumentException("The tour schedule does not contain a valid duration.", nameof(schedule));

        return Math.Round(minutes.Value / 60m, 1, MidpointRounding.AwayFromZero);
    }

    private static bool IsSupportedTimeZone(string timeZoneId)
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _))
            return true;

        return TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId) &&
               TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out _);
    }

    private static int CalculateElapsedMinutes(
        string durationType,
        int durationDays,
        TimeOnly start,
        TimeOnly end)
    {
        var startMinutes = (start.Hour * 60) + start.Minute;
        var endMinutes = (end.Hour * 60) + end.Minute;
        if (durationType == TourDurationTypes.MultiDay)
            return ((Math.Max(1, durationDays) - 1) * 24 * 60) + endMinutes - startMinutes;

        return endMinutes - startMinutes;
    }
}
