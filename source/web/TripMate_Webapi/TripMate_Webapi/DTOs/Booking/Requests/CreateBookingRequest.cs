namespace TripMate_WebAPI.DTOs.Booking;

/// <summary>
/// Tạo booking mới — cần tourAvailabilityId thay vì tourId + date trực tiếp
/// Schema mới: bookings.tour_availability_id → tour_availability.id
/// </summary>
public record CreateBookingRequest(
    string TourAvailabilityId, // UUID của bản ghi trong bảng tour_availability
    int Guests,
    string? Note
);
