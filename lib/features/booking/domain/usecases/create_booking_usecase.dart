import '../entities/booking_entity.dart';
import '../repositories/booking_repository.dart';

class CreateBookingUseCase {
  final BookingRepository repository;

  CreateBookingUseCase(this.repository);

  Future<BookingEntity> call({
    required String tourId,
    required DateTime tourDate,
    required int guests,
    String? note,
  }) {
    return repository.createBooking(
      tourId: tourId,
      tourDate: tourDate,
      guests: guests,
      note: note,
    );
  }
}
