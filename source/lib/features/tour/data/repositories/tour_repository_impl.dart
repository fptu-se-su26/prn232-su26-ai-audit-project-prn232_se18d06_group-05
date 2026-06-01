import 'package:dartz/dartz.dart';
import '../../../../core/config/supabase_config.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/logger.dart';
import '../../domain/entities/tour_entity.dart';
import '../../domain/repositories/tour_repository.dart';
import '../datasources/tour_remote_datasource.dart';

class TourRepositoryImpl implements TourRepository {
  final TourRemoteDataSource remoteDataSource;

  TourRepositoryImpl({required this.remoteDataSource});

  @override
  Future<Either<Failure, List<TourEntity>>> getTours() async {
    try {
      final tours = await remoteDataSource.getTours();
      return Right(tours);
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message));
    } catch (e) {
      Logger.error('Unexpected error in getTours', e);
      return Left(ServerFailure(message: 'Lỗi không xác định'));
    }
  }

  @override
  Future<Either<Failure, TourEntity>> getTourById(String tourId) async {
    try {
      final tour = await remoteDataSource.getTourById(tourId);
      return Right(tour);
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message));
    } catch (e) {
      Logger.error('Unexpected error in getTourById', e);
      return Left(ServerFailure(message: 'Lỗi không xác định'));
    }
  }

  @override
  Future<Either<Failure, List<TourEntity>>> getToursByGuide(
    String guideId,
  ) async {
    try {
      final tours = await remoteDataSource.getToursByGuide(guideId);
      return Right(tours);
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message));
    } catch (e) {
      Logger.error('Unexpected error in getToursByGuide', e);
      return Left(ServerFailure(message: 'Lỗi không xác định'));
    }
  }

  @override
  Future<Either<Failure, List<TourEntity>>> searchTours(String query) async {
    try {
      final tours = await remoteDataSource.searchTours(query);
      return Right(tours);
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message));
    } catch (e) {
      Logger.error('Unexpected error in searchTours', e);
      return Left(ServerFailure(message: 'Lỗi không xác định'));
    }
  }

  @override
  Future<Either<Failure, TourEntity>> createTour({
    required String title,
    String? description,
    required String location,
    required double price,
    required int durationHours,
    int maxParticipants = 10,
    List<String> images = const [],
  }) async {
    try {
      // Get current user ID
      final userId = SupabaseConfig.client.auth.currentUser?.id;
      if (userId == null) {
        return Left(AuthFailure(message: 'Bạn cần đăng nhập'));
      }

      final tour = await remoteDataSource.createTour(
        guideId: userId,
        title: title,
        description: description,
        location: location,
        price: price,
        durationHours: durationHours,
        maxParticipants: maxParticipants,
        images: images,
      );
      return Right(tour);
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message));
    } catch (e) {
      Logger.error('Unexpected error in createTour', e);
      return Left(ServerFailure(message: 'Lỗi không xác định'));
    }
  }

  @override
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
  }) async {
    try {
      final tour = await remoteDataSource.updateTour(
        tourId: tourId,
        title: title,
        description: description,
        location: location,
        price: price,
        durationHours: durationHours,
        maxParticipants: maxParticipants,
        images: images,
        status: status,
      );
      return Right(tour);
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message));
    } catch (e) {
      Logger.error('Unexpected error in updateTour', e);
      return Left(ServerFailure(message: 'Lỗi không xác định'));
    }
  }

  @override
  Future<Either<Failure, void>> deleteTour(String tourId) async {
    try {
      await remoteDataSource.deleteTour(tourId);
      return const Right(null);
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message));
    } catch (e) {
      Logger.error('Unexpected error in deleteTour', e);
      return Left(ServerFailure(message: 'Lỗi không xác định'));
    }
  }

  @override
  Future<Either<Failure, List<TourTemplateEntity>>> getTourTemplates() async {
    try {
      final templates = await remoteDataSource.getTourTemplates();
      return Right(templates);
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message));
    } catch (e) {
      Logger.error('Unexpected error in getTourTemplates', e);
      return Left(ServerFailure(message: 'Lỗi không xác định'));
    }
  }

  @override
  Future<Either<Failure, TourEntity>> createTourFromTemplate({
    required String templateId,
    required double price,
    required int durationHours,
    int maxParticipants = 10,
  }) async {
    try {
      // Get current user ID
      final userId = SupabaseConfig.client.auth.currentUser?.id;
      if (userId == null) {
        return Left(AuthFailure(message: 'Bạn cần đăng nhập'));
      }

      final tour = await remoteDataSource.createTourFromTemplate(
        guideId: userId,
        templateId: templateId,
        price: price,
        durationHours: durationHours,
        maxParticipants: maxParticipants,
      );
      return Right(tour);
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message));
    } catch (e) {
      Logger.error('Unexpected error in createTourFromTemplate', e);
      return Left(ServerFailure(message: 'Lỗi không xác định'));
    }
  }
}
