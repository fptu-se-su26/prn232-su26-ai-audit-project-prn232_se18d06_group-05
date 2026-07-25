using System.Globalization;
using System.Text.Json;
using TripMate_WebAPI.DTOs.Tour.Scheduling;

namespace TripMate_WebAPI.Services;

/// <summary>
/// Owns the single conversion boundary between typed multi-day itineraries and
/// the existing timeline_json column.
/// </summary>
public static class TourItineraryMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static List<TourItineraryDayDto> DeserializeStructured(string? json)
    {
        try
        {
            var days = string.IsNullOrWhiteSpace(json)
                ? []
                : JsonSerializer.Deserialize<List<TourItineraryDayDto?>>(json, JsonOptions) ?? [];
            if (days.Any(day => day == null || (day.Items ?? []).Any(item => item == null)))
                throw new ArgumentException("The itinerary contains an invalid day or activity.");

            return days.Select(day => day!).ToList();
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("The itinerary format is invalid.", exception);
        }
    }

    public static List<Dictionary<string, string>> DeserializeLegacy(string? json)
    {
        try
        {
            return string.IsNullOrWhiteSpace(json)
                ? []
                : JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json, JsonOptions) ?? [];
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("The itinerary format is invalid.", exception);
        }
    }

    public static List<Dictionary<string, string>> Flatten(IEnumerable<TourItineraryDayDto> days)
    {
        var result = new List<Dictionary<string, string>>();
        foreach (var day in days.OrderBy(item => item.DayNumber))
        {
            if (day == null)
                throw new ArgumentException("The itinerary contains an invalid day.");

            var dayItems = day.Items ?? [];
            if (dayItems.Count == 0)
            {
                result.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["entryType"] = "day",
                    ["dayNumber"] = day.DayNumber.ToString(CultureInfo.InvariantCulture),
                    ["dayTitle"] = (day.Title ?? string.Empty).Trim(),
                    ["startTime"] = string.Empty,
                    ["endTime"] = string.Empty,
                    ["activity"] = string.Empty,
                    ["location"] = string.Empty
                });
                continue;
            }

            foreach (var item in dayItems)
            {
                if (item == null)
                    throw new ArgumentException($"Day {day.DayNumber} contains an invalid activity.");

                result.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["entryType"] = "activity",
                    ["dayNumber"] = day.DayNumber.ToString(CultureInfo.InvariantCulture),
                    ["dayTitle"] = (day.Title ?? string.Empty).Trim(),
                    ["startTime"] = NormalizeOrTrim(item.StartTime),
                    ["endTime"] = NormalizeOrTrim(item.EndTime),
                    ["activity"] = (item.Activity ?? string.Empty).Trim(),
                    ["location"] = (item.Location ?? string.Empty).Trim()
                });
            }
        }

        return result;
    }

    public static List<TourItineraryDayDto> Expand(
        IEnumerable<Dictionary<string, string>>? persistedItems)
    {
        var items = persistedItems?.Where(item => item != null).ToList() ?? [];
        if (items.Count == 0) return [];
        if (!IsStructured(items))
        {
            return
            [
                new TourItineraryDayDto
                {
                    DayNumber = 1,
                    Items = items.Select(item => new TourItineraryItemDto
                    {
                        StartTime = ReadValue(item, "time"),
                        EndTime = string.Empty,
                        Activity = ReadValue(item, "activity")
                    }).ToList()
                }
            ];
        }

        return items
            .Select((item, index) => new
            {
                Item = item,
                Index = index,
                DayNumber = ReadDayNumber(item)
            })
            .GroupBy(value => value.DayNumber)
            .OrderBy(group => group.Key)
            .Select(group => new TourItineraryDayDto
            {
                DayNumber = group.Key,
                Title = ReadValue(group.First().Item, "dayTitle"),
                Items = group
                    .Where(value => ReadValue(value.Item, "entryType") != "day")
                    .OrderBy(value => value.Index)
                    .Select(value => new TourItineraryItemDto
                    {
                        StartTime = ReadValue(value.Item, "startTime"),
                        EndTime = ReadValue(value.Item, "endTime"),
                        Activity = ReadValue(value.Item, "activity"),
                        Location = NullIfEmpty(ReadValue(value.Item, "location"))
                    })
                    .ToList()
            })
            .ToList();
    }

    public static List<Dictionary<string, string>> ToLegacyEditorTimeline(
        IEnumerable<Dictionary<string, string>>? persistedItems)
    {
        var items = persistedItems?.Where(item => item != null).ToList() ?? [];
        if (!IsStructured(items)) return items;

        return items
            .Where(item => ReadValue(item, "entryType") != "day")
            .Select(item => new Dictionary<string, string>
            {
                ["time"] = ReadValue(item, "startTime"),
                ["activity"] = ReadValue(item, "activity")
            })
            .ToList();
    }

    public static bool IsStructured(IEnumerable<Dictionary<string, string>>? persistedItems)
        => persistedItems?.Any(item => item != null && (
            item.ContainsKey("dayNumber") ||
            item.ContainsKey("startTime") ||
            item.ContainsKey("endTime"))) == true;

    public static IReadOnlyList<string> ValidateLegacyForPublish(
        IReadOnlyCollection<Dictionary<string, string>> items)
    {
        if (items.Count == 0)
            return ["Add at least one complete itinerary item before publishing."];

        return items.Any(item =>
                string.IsNullOrWhiteSpace(ReadValue(item, "time")) ||
                string.IsNullOrWhiteSpace(ReadValue(item, "activity")))
            ? ["Enter both the time and activity for every itinerary item."]
            : [];
    }

    private static int ReadDayNumber(IReadOnlyDictionary<string, string> item)
        => int.TryParse(ReadValue(item, "dayNumber"), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out var dayNumber) && dayNumber > 0
            ? dayNumber
            : 1;

    private static string ReadValue(IReadOnlyDictionary<string, string> item, string key)
        => item.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty;

    private static string NormalizeOrTrim(string? value)
        => TourSchedulePolicy.NormalizeClockTime(value) ?? (value ?? string.Empty).Trim();

    private static string? NullIfEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
