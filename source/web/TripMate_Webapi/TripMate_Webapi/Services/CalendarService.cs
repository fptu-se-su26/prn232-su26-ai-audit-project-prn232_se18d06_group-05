using TripMate_Webapi.Entities;
using TripMate_Webapi.Repositories;
using TripMate_WebAPI.DTOs.Guide.Requests;
using TripMate_WebAPI.DTOs.Guide.Responses;
using System.Globalization;

namespace TripMate_WebAPI.Services
{
    public class CalendarService : ICalendarService
    {
        private readonly IGuideRepository _guideRepository;
        private readonly IBookingRepository _bookingRepository;
        private static readonly TimeSpan GuideResponseWindow = TimeSpan.FromHours(24);

        public CalendarService(
            IGuideRepository guideRepository,
            IBookingRepository bookingRepository)
        {
            _guideRepository = guideRepository;
            _bookingRepository = bookingRepository;
        }

        public async Task<CalendarDataDto> GetCalendarDataAsync(string guideProfileId, string start, string end)
        {
            var (rangeStart, rangeEnd) = ValidateRange(start, end);
            start = rangeStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            end = rangeEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // Availability and joined booking data are independent, so load them concurrently.
            var availabilityTask = _guideRepository.GetBlockedDatesInRangeAsync(guideProfileId, start, end);
            var bookingTask = _bookingRepository.GetGuideCalendarBookingsInRangeAsync(guideProfileId, start, end);
            await Task.WhenAll(availabilityTask, bookingTask);

            var availabilityEntities = await availabilityTask;
            var blockedDates = availabilityEntities
                .Select(a => new BlockedDateItem(a.Id, a.UnavailableDate, a.Reason))
                .ToList();

            var bookingRecords = await bookingTask;
            var bookings = new List<CalendarBookingItem>(bookingRecords.Count);

            foreach (var booking in bookingRecords)
            {
                var status = ResolveStatus(booking.Status, booking.CreatedAt);
                if (status is "cancelled" or "expired") continue;

                var startTime = NormalizeTime(booking.StartTime);
                var endTime = CalculateEndTime(startTime, booking.ExperiencePackage?.DurationHours ?? 0);

                bookings.Add(new CalendarBookingItem(
                    BookingId: booking.Id,
                    BookingDate: booking.BookingDate,
                    StartTime: startTime,
                    EndTime: endTime,
                    TravelerId: booking.TravelerId,
                    GuestName: booking.Traveler?.FullName ?? "Traveler",
                    TravelerAvatarUrl: booking.Traveler?.AvatarUrl ?? "/images/AVATAR.png",
                    GuestCount: booking.GuestCount,
                    GuideEarnings: booking.GuideEarnings,
                    PackageId: booking.ExperiencePackageId,
                    PackageTitle: booking.ExperiencePackage?.Title ?? "Unknown Tour",
                    CoverImageUrl: booking.ExperiencePackage?.CoverImageUrl ?? string.Empty,
                    MeetingPoint: booking.ExperiencePackage?.MeetingPoint ?? "Not provided",
                    TravelerNotes: booking.TravelerNotes,
                    Status: status
                ));
            }

            return new CalendarDataDto(blockedDates, bookings);
        }

        public async Task<SaveBlockedDatesResult> SaveBlockedDatesAsync(
            string guideProfileId,
            SaveBlockedDatesRequest req)
        {
            var (rangeStart, rangeEnd) = ValidateRange(req.RangeStart, req.RangeEnd);
            var addedDates = NormalizeAddedDates(req.AddedDates, rangeStart, rangeEnd);
            var removedDates = NormalizeRequestedDates(req.RemovedDates, rangeStart, rangeEnd);
            removedDates.RemoveAll(addedDates.ContainsKey);
            var normalizedRangeStart = rangeStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var normalizedRangeEnd = rangeEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            var bookings = await _bookingRepository.GetGuideBookingsInRangeAsync(
                guideProfileId,
                normalizedRangeStart,
                normalizedRangeEnd);

            // Pending bookings only remain active during their 24-hour response window.
            var activeBookingDates = bookings
                .Where(b => b.Status == 1 ||
                            (b.Status == 0 && DateTime.UtcNow < AsUtc(b.CreatedAt).Add(GuideResponseWindow)))
                .Select(b => b.BookingDate.ToString("yyyy-MM-dd"))
                .ToHashSet();

            var conflictingDates = addedDates.Keys
                .Where(activeBookingDates.Contains)
                .OrderBy(date => date)
                .ToList();
            var acceptedAdditions = addedDates
                .Where(item => !activeBookingDates.Contains(item.Key))
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);

            // Only touch dates changed by this client. Re-adding a date is an intentional
            // upsert so an edited reason replaces the previous value without rewriting
            // unrelated availability rows in the visible calendar range.
            var datesToDelete = removedDates
                .Concat(addedDates.Keys)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            await _guideRepository.DeleteBlockedDatesAsync(guideProfileId, datesToDelete);

            if (acceptedAdditions.Count > 0)
            {
                var entities = acceptedAdditions.Select(item => new GuideAvailabilityEntity
                {
                    GuideProfileId = guideProfileId,
                    UnavailableDate = item.Key,
                    Reason = item.Value
                }).ToList();

                await _guideRepository.InsertBlockedDatesAsync(entities);
            }

            var savedEntities = await _guideRepository
                .GetBlockedDatesInRangeAsync(guideProfileId, normalizedRangeStart, normalizedRangeEnd);
            var savedDates = savedEntities
                .OrderBy(entity => entity.UnavailableDate)
                .Select(entity => new BlockedDateItem(entity.Id, entity.UnavailableDate, entity.Reason))
                .ToList();

            return new SaveBlockedDatesResult(savedDates, conflictingDates);
        }

        private static string ResolveStatus(int status, DateTime createdAt)
        {
            return status switch
            {
                0 when DateTime.UtcNow >= AsUtc(createdAt).Add(GuideResponseWindow) => "expired",
                0 => "pending",
                1 => "confirmed",
                2 => "completed",
                3 => "cancelled",
                _ => "unknown"
            };
        }

        private static DateTime AsUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        private static string NormalizeTime(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var candidate = value.Trim();
            var timezoneIndex = candidate.IndexOfAny(['+', '-']);
            if (timezoneIndex > 0) candidate = candidate[..timezoneIndex];
            candidate = candidate.TrimEnd('Z');

            if (TimeSpan.TryParse(candidate, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
            }

            return value.Length >= 5 ? value[..5] : value;
        }

        private static string CalculateEndTime(string startTime, decimal durationHours)
        {
            if (durationHours <= 0 ||
                !TimeSpan.TryParse(startTime, CultureInfo.InvariantCulture, out var start))
            {
                return string.Empty;
            }

            return start.Add(TimeSpan.FromHours((double)durationHours))
                .ToString(@"hh\:mm", CultureInfo.InvariantCulture);
        }

        private static (DateOnly Start, DateOnly End) ValidateRange(string start, string end)
        {
            if (!DateOnly.TryParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var rangeStart) ||
                !DateOnly.TryParseExact(end, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var rangeEnd))
            {
                throw new ArgumentException("Calendar range must use the yyyy-MM-dd format.");
            }

            if (rangeEnd <= rangeStart || rangeEnd.DayNumber - rangeStart.DayNumber > 62)
            {
                throw new ArgumentException("Calendar range is invalid or too large.");
            }

            return (rangeStart, rangeEnd);
        }

        private static List<string> NormalizeRequestedDates(
            IEnumerable<string>? dates,
            DateOnly rangeStart,
            DateOnly rangeEnd)
        {
            var normalized = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in dates ?? [])
            {
                if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var date))
                {
                    throw new ArgumentException($"Invalid blocked date: {value}.");
                }

                if (date < rangeStart || date >= rangeEnd)
                {
                    throw new ArgumentException($"Blocked date {value} is outside the visible calendar range.");
                }

                normalized.Add(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            return normalized.OrderBy(date => date).ToList();
        }

        private static Dictionary<string, string?> NormalizeAddedDates(
            IEnumerable<BlockedDateChange>? changes,
            DateOnly rangeStart,
            DateOnly rangeEnd)
        {
            var normalized = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var change in changes ?? [])
            {
                if (!DateOnly.TryParseExact(change.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var date))
                {
                    throw new ArgumentException($"Invalid blocked date: {change.Date}.");
                }

                if (date < rangeStart || date >= rangeEnd)
                {
                    throw new ArgumentException($"Blocked date {change.Date} is outside the visible calendar range.");
                }

                var reason = string.IsNullOrWhiteSpace(change.Reason) ? null : change.Reason.Trim();
                if (reason?.Length > 160)
                {
                    throw new ArgumentException("Availability reason must be 160 characters or fewer.");
                }

                normalized[date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)] = reason;
            }

            return normalized;
        }
    }
}
