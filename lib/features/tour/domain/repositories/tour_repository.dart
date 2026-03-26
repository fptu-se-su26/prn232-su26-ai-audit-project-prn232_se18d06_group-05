import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/tour_entity.dart';

/// Tour repository interface
abstract class TourRepository {
  /// Get all active tours
  Future<Either<Failure, List<TourEntity>>> getTours();

  /// Get tour by ID
  Future<Either<Failure, TourEntity>> getTourById(String tourId);

  /// Get tours by guide ID
  Future<Either<Failure, List<TourEntity>>> getToursByGuide(String guideId);

  /// Search tours by location or title
  Future<Either<Failure, List<TourEntity>>> searchTours(String query);

  /// Create a new tour (guide only)
  Future<Either<Failure, TourEntity>> createTour({
    required String title,
    String? description,
    required String location,
    required double price,
    required int durationHours,
    int maxParticipants = 10,
    List<String> images = const [],
  });

  /// Update tour (guide only)
  Future<Either<Failure, TourEntity>> updateTour({
    required String tourId,
    String? title,
    String? description,
    String? location,
    double? price,
    int? durationHours,
    int? maxParticipants,
    List<String>? images,
    String? status,
  });

  /// Delete tour (guide only)
  Future<Either<Failure, void>> deleteTour(String tourId);
}
