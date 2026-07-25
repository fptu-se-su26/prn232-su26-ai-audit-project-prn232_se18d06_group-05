using TripMate_WebAPI.DTOs.Tour.Scheduling;

namespace TripMate_WebAPI.Services;

/// <summary>
/// Validates ordered Guide itinerary days and prevents overlapping activities.
/// </summary>
public static class TourItineraryPolicy
{
    public const int MaximumItemsPerDay = 20;
    public const int MaximumDayTitleLength = 120;
    public const int MaximumActivityLength = 300;
    public const int MaximumLocationLength = 200;

    public static IReadOnlyList<string> ValidateForPublish(
        IReadOnlyCollection<TourItineraryDayDto>? days,
        int expectedDayCount)
    {
        var errors = new List<string>();
        if (days == null || days.Count == 0)
        {
            errors.Add("Add at least one itinerary day before publishing.");
            return errors;
        }

        if (expectedDayCount < 1 || days.Count != expectedDayCount)
            errors.Add("The itinerary day count must match the tour duration.");

        var orderedDays = days.OrderBy(day => day.DayNumber).ToList();
        if (orderedDays.Select(day => day.DayNumber).Distinct().Count() != orderedDays.Count ||
            orderedDays.Where((day, index) => day.DayNumber != index + 1).Any())
        {
            errors.Add("Itinerary days must be numbered consecutively from day 1.");
        }

        foreach (var day in orderedDays)
        {
            ValidateDay(day, errors);
        }

        return errors;
    }

    public static IReadOnlyList<string> ValidateForPublish(
        IReadOnlyCollection<TourItineraryDayDto>? days,
        TourScheduleDto schedule)
    {
        var errors = ValidateForPublish(days, schedule.DurationDays).ToList();
        if (days == null || days.Count == 0 ||
            !TourSchedulePolicy.TryParseClockTime(schedule.DefaultStartTime, out var tourStart) ||
            !TourSchedulePolicy.TryParseClockTime(schedule.DefaultEndTime, out var tourEnd))
        {
            return errors;
        }

        var orderedDays = days.OrderBy(day => day.DayNumber).ToList();
        ValidateStartBoundary(orderedDays[0], tourStart, errors);
        ValidateEndBoundary(orderedDays[^1], tourEnd, errors);
        return errors;
    }

    private static void ValidateDay(TourItineraryDayDto day, ICollection<string> errors)
    {
        var items = day.Items ?? [];
        if ((day.Title ?? string.Empty).Trim().Length > MaximumDayTitleLength)
            errors.Add($"Day {day.DayNumber} title must be {MaximumDayTitleLength} characters or fewer.");

        if (items.Count == 0)
        {
            errors.Add($"Add at least one activity to day {day.DayNumber}.");
            return;
        }
        if (items.Count > MaximumItemsPerDay)
            errors.Add($"Day {day.DayNumber} can contain up to {MaximumItemsPerDay} activities.");

        var validItems = new List<(TimeOnly Start, TimeOnly End)>();
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var itemLabel = $"Day {day.DayNumber}, activity {index + 1}";
            if (string.IsNullOrWhiteSpace(item.Activity))
                errors.Add($"{itemLabel} needs a description.");
            else if (item.Activity.Trim().Length > MaximumActivityLength)
                errors.Add($"{itemLabel} description must be {MaximumActivityLength} characters or fewer.");
            if ((item.Location ?? string.Empty).Trim().Length > MaximumLocationLength)
                errors.Add($"{itemLabel} location must be {MaximumLocationLength} characters or fewer.");

            var hasStart = TourSchedulePolicy.TryParseClockTime(item.StartTime, out var start);
            var hasEnd = TourSchedulePolicy.TryParseClockTime(item.EndTime, out var end);
            if (!hasStart || !hasEnd)
            {
                errors.Add($"{itemLabel} needs valid start and end times.");
                continue;
            }

            if (end <= start)
            {
                errors.Add($"{itemLabel} must end after it starts.");
                continue;
            }

            validItems.Add((start, end));
        }

        var orderedItems = validItems.OrderBy(item => item.Start).ToList();
        for (var index = 1; index < orderedItems.Count; index++)
        {
            if (orderedItems[index].Start < orderedItems[index - 1].End)
            {
                errors.Add($"Day {day.DayNumber} contains overlapping activities.");
                break;
            }
        }
    }

    private static void ValidateStartBoundary(
        TourItineraryDayDto firstDay,
        TimeOnly tourStart,
        ICollection<string> errors)
    {
        var activityStarts = (firstDay.Items ?? [])
            .Select(item => TourSchedulePolicy.TryParseClockTime(item.StartTime, out var parsed)
                ? parsed
                : (TimeOnly?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();
        if (activityStarts.Count > 0 && activityStarts.Min() < tourStart)
            errors.Add("The first-day itinerary cannot start before the tour start time.");
    }

    private static void ValidateEndBoundary(
        TourItineraryDayDto lastDay,
        TimeOnly tourEnd,
        ICollection<string> errors)
    {
        var activityEnds = (lastDay.Items ?? [])
            .Select(item => TourSchedulePolicy.TryParseClockTime(item.EndTime, out var parsed)
                ? parsed
                : (TimeOnly?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();
        if (activityEnds.Count > 0 && activityEnds.Max() > tourEnd)
            errors.Add("The final-day itinerary cannot end after the tour end time.");
    }
}
