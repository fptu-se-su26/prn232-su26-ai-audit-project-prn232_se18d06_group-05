import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/tour_entity.dart';
import '../repositories/tour_repository.dart';

/// Search tours use case
class SearchToursUseCase {
  final TourRepository repository;

  SearchToursUseCase(this.repository);

  Future<Either<Failure, List<TourEntity>>> call(String query) async {
    if (query.trim().isEmpty) {
      return Left(
        ValidationFailure(message: 'Từ khóa tìm kiếm không được để trống'),
      );
    }

    return await repository.searchTours(query.trim());
  }
}
