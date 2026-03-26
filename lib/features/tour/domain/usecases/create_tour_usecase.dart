import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/tour_entity.dart';
import '../repositories/tour_repository.dart';

/// Create tour use case
class CreateTourUseCase {
  final TourRepository repository;

  CreateTourUseCase(this.repository);

  Future<Either<Failure, TourEntity>> call({
    required String title,
    String? description,
    required String location,
    required double price,
    required int durationHours,
    int maxParticipants = 10,
    List<String> images = const [],
  }) async {
    // Validation
    if (title.trim().isEmpty) {
      return Left(ValidationFailure(message: 'Tiêu đề không được để trống'));
    }

    if (location.trim().isEmpty) {
      return Left(ValidationFailure(message: 'Địa điểm không được để trống'));
    }

    if (price <= 0) {
      return Left(ValidationFailure(message: 'Giá phải lớn hơn 0'));
    }

    if (durationHours <= 0) {
      return Left(ValidationFailure(message: 'Thời gian tour phải lớn hơn 0'));
    }

    if (maxParticipants <= 0) {
      return Left(
        ValidationFailure(message: 'Số người tham gia phải lớn hơn 0'),
      );
    }

    return await repository.createTour(
      title: title.trim(),
      description: description?.trim(),
      location: location.trim(),
      price: price,
      durationHours: durationHours,
      maxParticipants: maxParticipants,
      images: images,
    );
  }
}
