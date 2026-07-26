using System.Globalization;
using TripMate_Webapi.Entities;
using TripMate_Webapi.Repositories;

namespace TripMate_WebAPI.Services;

/// <summary>
/// Canonical booking creation boundary. All entry points use the same
/// validation, price calculation and initial payment state.
/// </summary>
public sealed class BookingCreationService
{
    public const string CustomPackageId = "00000000-0000-0000-0000-000000000000";

    private readonly IBookingRepository _bookings;
    private readonly IExperiencePackageRepository _packages;
    private readonly IGuideRepository _guides;

    public BookingCreationService(
        IBookingRepository bookings,
        IExperiencePackageRepository packages,
        IGuideRepository guides)
    {
        _bookings = bookings;
        _packages = packages;
        _guides = guides;
    }

    public async Task<BookingEntity> CreatePackageBookingAsync(
        string travelerId,
        string packageId,
        DateTime bookingDate,
        int guestCount,
        string? travelerNotes = null,
        string? requestedStartTime = null,
        string? userToken = null)
    {
        var package = await _packages.GetPackageByIdAsync(packageId)
            ?? throw new InvalidOperationException("Experience package not found.");

        if (!package.IsActive || !string.Equals(package.PublicationStatus, "published", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This experience package is not available for booking.");

        ValidateGuestCount(guestCount, package.MaxGroupSize);
        var date = NormalizeBookingDate(bookingDate);
        var durationDays = Math.Max(1, package.DurationDays);
        await EnsureGuideAvailableAsync(package.GuideProfileId, date, durationDays);
        await EnsureNoDuplicateAsync(travelerId, package.Id, package.GuideProfileId, date);

        var price = TourPricingCalculator.CalculateTourPrice(
            package.PricePerSession,
            package.PricePerPerson,
            package.IncludedGuestCount,
            guestCount);

        var startClock = ParseStartTime(requestedStartTime)
            ?? ParseStartTime(package.DefaultStartTime)
            ?? new TimeSpan(9, 0, 0);
        var (scheduledStartUtc, scheduledEndUtc) = CreatePackageScheduleSnapshot(
            package,
            date,
            startClock);

        return await PersistAsync(
            travelerId,
            package.GuideProfileId,
            package.Id,
            date,
            startClock,
            guestCount,
            travelerNotes,
            price,
            scheduledStartUtc,
            scheduledEndUtc,
            userToken);
    }

    public async Task<BookingEntity> CreateAgreedBookingAsync(
        string travelerId,
        string guideProfileId,
        DateTime bookingDate,
        DateTime? endDate,
        int guestCount,
        decimal agreedTotal,
        string? travelerNotes = null,
        string? userToken = null)
    {
        ValidateGuestCount(guestCount, int.MaxValue);
        var date = NormalizeBookingDate(bookingDate);
        var durationDays = endDate.HasValue
            ? Math.Max(1, (endDate.Value.Date - date).Days + 1)
            : 1;

        await EnsureGuideAvailableAsync(guideProfileId, date, durationDays);
        await EnsureNoDuplicateAsync(travelerId, CustomPackageId, guideProfileId, date);

        var customStartClock = new TimeSpan(9, 0, 0);
        var customStartUtc = ConvertLocalToUtc(date.Add(customStartClock), TourSchedulePolicy.DefaultTimeZone);
        var customEndUtc = ConvertLocalToUtc(
            date.AddDays(durationDays - 1).Add(customStartClock).AddHours(4),
            TourSchedulePolicy.DefaultTimeZone);

        return await PersistAsync(
            travelerId,
            guideProfileId,
            CustomPackageId,
            date,
            customStartClock,
            guestCount,
            travelerNotes,
            TourPricingCalculator.FromAgreedTotal(agreedTotal),
            customStartUtc,
            customEndUtc,
            userToken);
    }

    private async Task<BookingEntity> PersistAsync(
        string travelerId,
        string guideProfileId,
        string packageId,
        DateTime date,
        TimeSpan startClock,
        int guestCount,
        string? travelerNotes,
        BookingPriceBreakdown price,
        DateTime scheduledStartUtc,
        DateTime scheduledEndUtc,
        string? userToken)
    {
        var booking = new BookingEntity
        {
            TravelerId = travelerId,
            GuideProfileId = guideProfileId,
            ExperiencePackageId = packageId,
            BookingDate = date,
            StartTime = date.Add(startClock),
            GuestCount = guestCount,
            TotalAmount = price.TotalAmount,
            PlatformFee = price.PlatformFee,
            GuideEarnings = price.GuideEarnings,
            AmountPaid = 0,
            PaymentStatus = "unpaid",
            Status = -1,
            TravelerNotes = travelerNotes,
            ScheduledStartAt = scheduledStartUtc,
            ScheduledEndAt = scheduledEndUtc
        };

        return await _bookings.CreateBookingAsync(booking, userToken);
    }

    private async Task EnsureNoDuplicateAsync(
        string travelerId,
        string packageId,
        string guideProfileId,
        DateTime date)
    {
        var existing = await _bookings.GetBookingsByTravelerAsync(travelerId);
        var duplicate = existing.Any(b =>
            b.Status is >= -1 and < 2 &&
            b.ExperiencePackageId == packageId &&
            b.GuideProfileId == guideProfileId &&
            b.BookingDate.Date == date.Date);

        if (duplicate)
            throw new InvalidOperationException("You already have an active booking for this tour on this date.");
    }

    private async Task EnsureGuideAvailableAsync(string guideProfileId, DateTime date, int durationDays)
    {
        var endExclusive = date.AddDays(durationDays);
        var blocked = await _guides.GetBlockedDatesInRangeAsync(
            guideProfileId,
            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            endExclusive.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        if (blocked.Count > 0)
            throw new InvalidOperationException("The guide is unavailable for one or more dates in this trip.");

        var confirmedBookings = await _bookings.GetBookingsForGuideAsync(guideProfileId);
        var hasScheduleConflict = confirmedBookings.Any(existing =>
        {
            if (existing.Status != 1) return false;
            var existingStart = existing.BookingDate.Date;
            var existingDurationDays = Math.Max(1, existing.ExperiencePackage?.DurationDays ?? 1);
            var existingEndExclusive = existingStart.AddDays(existingDurationDays);
            return existingStart < endExclusive && date < existingEndExclusive;
        });

        if (hasScheduleConflict)
            throw new InvalidOperationException("The guide already has a confirmed trip during these dates.");
    }

    private static DateTime NormalizeBookingDate(DateTime date)
    {
        if (date == default)
            throw new ArgumentException("A booking date is required.", nameof(date));

        var result = date.Date;
        if (result < DateTime.UtcNow.Date)
            throw new InvalidOperationException("The booking date cannot be in the past.");

        return result;
    }

    private static void ValidateGuestCount(int guestCount, int maxGroupSize)
    {
        if (guestCount < 1)
            throw new InvalidOperationException("The booking must include at least one guest.");
        if (guestCount > Math.Max(1, maxGroupSize))
            throw new InvalidOperationException($"The maximum group size for this package is {maxGroupSize}.");
    }

    private static TimeSpan? ParseStartTime(string? value)
    {
        if (!TourSchedulePolicy.TryParseClockTime(value, out var result)) return null;
        return new TimeSpan(result.Hour, result.Minute, result.Second);
    }

    private static (DateTime StartUtc, DateTime EndUtc) CreatePackageScheduleSnapshot(
        ExperiencePackageEntity package,
        DateTime date,
        TimeSpan startClock)
    {
        var timeZone = string.IsNullOrWhiteSpace(package.TimeZone)
            ? TourSchedulePolicy.DefaultTimeZone
            : package.TimeZone;
        var localStart = date.Add(startClock);
        DateTime localEnd;

        if (string.Equals(package.DurationType, "multi_day", StringComparison.OrdinalIgnoreCase) &&
            ParseStartTime(package.DefaultEndTime) is { } endClock)
        {
            localEnd = date.AddDays(Math.Max(1, package.DurationDays) - 1).Add(endClock);
        }
        else
        {
            var durationMinutes = package.DurationMinutes
                ?? checked((int)Math.Round(package.DurationHours * 60m, MidpointRounding.AwayFromZero));
            localEnd = localStart.AddMinutes(Math.Max(TourSchedulePolicy.MinimumDurationMinutes, durationMinutes));
        }

        return (
            ConvertLocalToUtc(localStart, timeZone),
            ConvertLocalToUtc(localEnd, timeZone));
    }

    private static DateTime ConvertLocalToUtc(DateTime localTime, string timeZoneId)
    {
        TimeZoneInfo? zone = null;
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out zone) &&
            TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId))
        {
            TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out zone);
        }

        zone ??= TimeZoneInfo.Utc;
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified),
            zone);
    }
}
