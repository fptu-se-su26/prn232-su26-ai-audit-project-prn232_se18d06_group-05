import '../entities/booking_entity.dart';
import '../repositories/booking_repository.dart';

class GetMyBookingsUseCase {
  final BookingRepository repository;

  GetMyBookingsUseCase(this.repository);

  Future<List<BookingEntity>> call() => repository.getMyBookings();
}
