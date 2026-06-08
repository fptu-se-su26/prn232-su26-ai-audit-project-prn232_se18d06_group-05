namespace TripMate_WebAPI.DTOs.Booking;

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
