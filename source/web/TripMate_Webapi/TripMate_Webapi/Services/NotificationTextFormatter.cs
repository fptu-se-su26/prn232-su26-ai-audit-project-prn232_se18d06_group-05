using System.Globalization;
using System.Text.RegularExpressions;

namespace TripMate_WebAPI.Services;

/// <summary>
/// Keeps internal booking identifiers out of user-visible notification copy.
/// Booking IDs still belong in action URLs, structured data, and dedupe keys.
/// </summary>
public static class NotificationTextFormatter
{
    private const string UuidPattern =
        "[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}";

    private static readonly Regex LeadingBookingId = new(
        $"^Booking\\s+{UuidPattern}\\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex InlineBookingId = new(
        $"\\bbooking\\s+{UuidPattern}\\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string NormalizeVisibleText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;

        // This also repairs legacy rows when they are read from the database.
        var normalized = LeadingBookingId.Replace(value, "This trip");
        return InlineBookingId.Replace(normalized, "this trip");
    }

    public static string FormatTripDate(DateTime? bookingDate)
        => bookingDate?.ToString("MMM d, yyyy", CultureInfo.InvariantCulture) ?? "the scheduled date";

    public static string FormatVnd(decimal amount)
        => $"{amount.ToString("N0", CultureInfo.InvariantCulture)}₫";
}
