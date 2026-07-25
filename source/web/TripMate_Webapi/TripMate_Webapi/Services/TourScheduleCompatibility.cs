using TripMate_WebAPI.DTOs.Tour.Scheduling;
using TripMate_Webapi.Entities;

namespace TripMate_WebAPI.Services;

/// <summary>
/// Maps the Guide schedule contract to the existing experience package model
/// while duration_hours remains part of the shared database/API contract.
/// </summary>
public static class TourScheduleCompatibility
{
    public static bool HasConfiguredSchedule(ExperiencePackageEntity entity)
        => TourSchedulePolicy.ValidateForPublish(FromEntity(entity)).Count == 0;

    public static TourScheduleDto FromEntity(ExperiencePackageEntity entity)
    {
        var fallback = FromLegacyDuration(entity.DurationHours);
        var durationType = TourSchedulePolicy.NormalizeDurationType(entity.DurationType);
        if (durationType is not TourDurationTypes.SameDay and not TourDurationTypes.MultiDay)
            durationType = fallback.DurationType;

        var durationDays = entity.DurationDays > 0 ? entity.DurationDays : fallback.DurationDays;
        if (durationType == TourDurationTypes.SameDay) durationDays = 1;
        if (durationType == TourDurationTypes.MultiDay && durationDays < 2) durationDays = 2;

        return new TourScheduleDto
        {
            DurationType = durationType,
            DurationMinutes = entity.DurationMinutes ?? fallback.DurationMinutes,
            DurationDays = durationDays,
            DefaultStartTime = TourSchedulePolicy.NormalizeClockTime(entity.DefaultStartTime),
            DefaultEndTime = TourSchedulePolicy.NormalizeClockTime(entity.DefaultEndTime),
            TimeZone = string.IsNullOrWhiteSpace(entity.TimeZone)
                ? TourSchedulePolicy.DefaultTimeZone
                : entity.TimeZone.Trim()
        };
    }

    public static TourScheduleDto FromLegacyDuration(decimal durationHours)
    {
        var safeHours = durationHours >= 0.5m ? durationHours : 4m;
        var durationType = safeHours > 24m
            ? TourDurationTypes.MultiDay
            : TourDurationTypes.SameDay;

        return new TourScheduleDto
        {
            DurationType = durationType,
            DurationMinutes = Math.Max(
                TourSchedulePolicy.MinimumDurationMinutes,
                (int)Math.Round(safeHours * 60m, MidpointRounding.AwayFromZero)),
            DurationDays = durationType == TourDurationTypes.MultiDay
                ? Math.Max(2, (int)Math.Ceiling(safeHours / 24m))
                : 1,
            TimeZone = TourSchedulePolicy.DefaultTimeZone
        };
    }

    public static void Apply(
        ExperiencePackageEntity entity,
        TourScheduleDto schedule,
        decimal fallbackDurationHours)
    {
        var durationType = TourSchedulePolicy.NormalizeDurationType(schedule.DurationType);
        if (durationType is not TourDurationTypes.SameDay and not TourDurationTypes.MultiDay)
            durationType = TourDurationTypes.SameDay;

        var durationDays = Math.Clamp(schedule.DurationDays, 1, TourSchedulePolicy.MaximumDurationDays);
        if (durationType == TourDurationTypes.SameDay) durationDays = 1;
        if (durationType == TourDurationTypes.MultiDay && durationDays < 2) durationDays = 2;

        var normalizedSchedule = new TourScheduleDto
        {
            DurationType = durationType,
            DurationDays = durationDays,
            DurationMinutes = schedule.DurationMinutes,
            DefaultStartTime = TourSchedulePolicy.NormalizeClockTime(schedule.DefaultStartTime),
            DefaultEndTime = TourSchedulePolicy.NormalizeClockTime(schedule.DefaultEndTime),
            TimeZone = string.IsNullOrWhiteSpace(schedule.TimeZone)
                ? TourSchedulePolicy.DefaultTimeZone
                : schedule.TimeZone.Trim()
        };

        var calculatedMinutes = TourSchedulePolicy.CalculateElapsedMinutes(normalizedSchedule);
        var durationMinutes = calculatedMinutes is >= TourSchedulePolicy.MinimumDurationMinutes
            ? calculatedMinutes
            : normalizedSchedule.DurationMinutes is >= TourSchedulePolicy.MinimumDurationMinutes
                ? normalizedSchedule.DurationMinutes
                : null;

        entity.DurationType = normalizedSchedule.DurationType;
        entity.DurationDays = normalizedSchedule.DurationDays;
        entity.DurationMinutes = durationMinutes;
        entity.DefaultStartTime = normalizedSchedule.DefaultStartTime;
        entity.DefaultEndTime = normalizedSchedule.DefaultEndTime;
        entity.TimeZone = normalizedSchedule.TimeZone;
        normalizedSchedule.DurationMinutes = durationMinutes;
        entity.DurationHours = durationMinutes.HasValue
            ? TourSchedulePolicy.ToLegacyDurationHours(normalizedSchedule)
            : fallbackDurationHours >= 0.5m ? fallbackDurationHours : 4m;
    }
}
