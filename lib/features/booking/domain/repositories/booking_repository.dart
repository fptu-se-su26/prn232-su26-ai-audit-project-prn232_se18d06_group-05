import '../entities/booking_entity.dart';

abstract class BookingRepository {
  Future<List<BookingEntity>> getMyBookings();
  Future<BookingEntity> createBooking({
    required String tourId,
    required DateTime tourDate,
    required int guests,
    String? note,
  });
  Future<void> cancelBooking(String bookingId);
  Future<BookingEntity?> getBookingById(String bookingId);
}
