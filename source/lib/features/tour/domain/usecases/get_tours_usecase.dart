import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/tour_entity.dart';
import '../repositories/tour_repository.dart';

/// Get all tours use case
class GetToursUseCase {
  final TourRepository repository;

  GetToursUseCase(this.repository);

  Future<Either<Failure, List<TourEntity>>> call() async {
    return await repository.getTours();
  }
}
