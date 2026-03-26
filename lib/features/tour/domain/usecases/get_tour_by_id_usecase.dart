import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/tour_entity.dart';
import '../repositories/tour_repository.dart';

/// Get tour by ID use case
class GetTourByIdUseCase {
  final TourRepository repository;

  GetTourByIdUseCase(this.repository);

  Future<Either<Failure, TourEntity>> call(String tourId) async {
    if (tourId.isEmpty) {
      return Left(ValidationFailure(message: 'Tour ID không được để trống'));
    }

    return await repository.getTourById(tourId);
  }
}
