import '../../domain/entities/booking_entity.dart';
import '../../domain/repositories/booking_repository.dart';
import '../datasources/booking_datasource.dart';

class BookingRepositoryImpl implements BookingRepository {
  final BookingDataSource dataSource;

  BookingRepositoryImpl(this.dataSource);

  @override
  Future<List<BookingEntity>> getMyBookings() => dataSource.getMyBookings();

  @override
  Future<BookingEntity> createBooking({
    required String tourId,
    required DateTime tourDate,
    required int guests,
    String? note,
  }) =>
      dataSource.createBooking(
        tourId: tourId,
        tourDate: tourDate,
        guests: guests,
        note: note,
      );

  @override
  Future<void> cancelBooking(String bookingId) =>
      dataSource.cancelBooking(bookingId);

  @override
  Future<BookingEntity?> getBookingById(String bookingId) =>
      dataSource.getBookingById(bookingId);
}
