using TripMate_Webapi.Entities;
using TripMate_Webapi.Repositories;
using TripMate_WebAPI.DTOs.Guide.Requests;
using TripMate_WebAPI.DTOs.Guide.Responses;

namespace TripMate_WebAPI.Services
{
    public class CalendarService : ICalendarService
    {
        private readonly IGuideRepository _guideRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IExperiencePackageRepository _packageRepository;

        public CalendarService(
            IGuideRepository guideRepository,
            IBookingRepository bookingRepository,
            IExperiencePackageRepository packageRepository)
        {
            _guideRepository = guideRepository;
            _bookingRepository = bookingRepository;
            _packageRepository = packageRepository;
        }

        public async Task<CalendarDataDto> GetCalendarDataAsync(string guideProfileId, string start, string end)
        {
            // 1. Get blocked dates
            var availabilityEntities = await _guideRepository.GetBlockedDatesInRangeAsync(guideProfileId, start, end);
            var blockedDates = availabilityEntities.Select(a => new BlockedDateItem(a.Id, a.UnavailableDate)).ToList();

            // 2. Get bookings
            var bookingEntities = await _bookingRepository.GetGuideBookingsInRangeAsync(guideProfileId, start, end);
            var bookings = new List<CalendarBookingItem>();

            foreach (var b in bookingEntities)
            {
                var package = await _packageRepository.GetPackageByIdAsync(b.ExperiencePackageId, guideProfileId);
                
                // Truncate guest name if we had traveler profiles linked, for now just use ID or placeholder
                // The booking entity traveler id can be used to fetch the profile, 
                // but since we are mocking some traveler names we can just use "Traveler"
                var guestName = "Traveler";
                
                bookings.Add(new CalendarBookingItem(
                    BookingId: b.Id,
                    BookingDate: b.BookingDate.ToString("yyyy-MM-dd"),
                    GuestName: guestName,
                    GuestCount: b.GuestCount,
                    GuideEarnings: b.GuideEarnings,
                    PackageTitle: package?.Title ?? "Unknown Tour",
                    Status: b.Status.ToString()
                ));
            }

            return new CalendarDataDto(blockedDates, bookings);
        }

        public async Task SaveBlockedDatesAsync(string guideProfileId, SaveBlockedDatesRequest req)
        {
            // 1. Validate: Ensure no blocked dates overlap with existing confirmed/pending bookings
            var bookings = await _bookingRepository.GetGuideBookingsInRangeAsync(guideProfileId, req.RangeStart, req.RangeEnd);
            var activeBookingDates = bookings
                .Where(b => b.Status == 0 || b.Status == 1 || b.Status == 2)
                .Select(b => b.BookingDate.ToString("yyyy-MM-dd"))
                .ToHashSet();

            var finalBlockedDates = req.BlockedDates.Where(d => !activeBookingDates.Contains(d)).ToList();

            // 2. Delete all existing blocked dates in the current view range
            await _guideRepository.DeleteBlockedDatesInRangeAsync(guideProfileId, req.RangeStart, req.RangeEnd);

            // 3. Insert new blocked dates
            if (finalBlockedDates.Any())
            {
                var entities = finalBlockedDates.Select(date => new GuideAvailabilityEntity
                {
                    GuideProfileId = guideProfileId,
                    UnavailableDate = date
                }).ToList();

                await _guideRepository.InsertBlockedDatesAsync(entities);
            }
        }
    }
}
