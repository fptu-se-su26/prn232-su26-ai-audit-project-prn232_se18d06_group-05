namespace TripMate_WebAPI.Models;

// ── Request DTOs ──────────────────────────────────────────────────────────────

/// <summary>
/// Tạo booking mới — cần tourAvailabilityId thay vì tourId + date trực tiếp
/// Schema mới: bookings.tour_availability_id → tour_availability.id
/// </summary>
public record CreateBookingRequest(
    string TourAvailabilityId, // UUID của bản ghi trong bảng tour_availability
    int Guests,
    string? Note
);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record BookingDto(
    string Id,
    string TourAvailabilityId,
    string GuideTourId,       // Lấy qua join: tour_availability.guide_tour_id
    string TourTitle,
    string TourLocation,
    string TravelerId,
    DateOnly TourDate,        // Lấy từ tour_availability.date
    int Guests,
    double TotalPrice,
    string? Note,
    string Status,
    DateTime CreatedAt,
    // Thêm thông tin hữu ích
    int? RemainingSlots       // Số chỗ còn lại sau khi đặt
);

// ── Tour Availability DTOs ────────────────────────────────────────────────────

public record TourAvailabilityDto(
    string Id,
    string GuideTourId,
    DateOnly Date,
    int RemainingSlots
);
