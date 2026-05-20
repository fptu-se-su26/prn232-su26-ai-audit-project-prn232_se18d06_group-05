import 'package:supabase_flutter/supabase_flutter.dart';
import '../../../../core/config/supabase_config.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/utils/logger.dart';
import '../models/tour_model.dart';

/// Tour remote data source
abstract class TourRemoteDataSource {
  Future<List<TourModel>> getTours();
  Future<TourModel> getTourById(String tourId);
  Future<List<TourModel>> getToursByGuide(String guideId);
  Future<List<TourModel>> searchTours(String query);
  Future<TourModel> createTour({
    required String guideId,
    required String title,
    String? description,
    required String location,
    required double price,
    required int durationHours,
    int maxParticipants = 10,
    List<String> images = const [],
  });
  Future<TourModel> updateTour({
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
  Future<void> deleteTour(String tourId);
}

class TourRemoteDataSourceImpl implements TourRemoteDataSource {
  final SupabaseClient client;

  TourRemoteDataSourceImpl({SupabaseClient? client})
    : client = client ?? SupabaseConfig.client;

  @override
  Future<List<TourModel>> getTours() async {
    try {
      Logger.info('Fetching all tours');

      final response = await client
          .from('tours')
          .select()
          .eq('status', 'active')
          .order('created_at', ascending: false);

      final tours = (response as List)
          .map((json) => TourModel.fromJson(json as Map<String, dynamic>))
          .toList();

      Logger.success('Fetched ${tours.length} tours');
      return tours;
    } catch (e) {
      Logger.error('Error fetching tours', e);
      throw ServerException(message: 'Không thể tải danh sách tour');
    }
  }

  @override
  Future<TourModel> getTourById(String tourId) async {
    try {
      Logger.info('Fetching tour: $tourId');

      final response = await client
          .from('tours')
          .select()
          .eq('id', tourId)
          .single();

      final tour = TourModel.fromJson(response);
      Logger.success('Fetched tour: ${tour.title}');
      return tour;
    } catch (e) {
      Logger.error('Error fetching tour', e);
      throw ServerException(message: 'Không thể tải thông tin tour');
    }
  }

  @override
  Future<List<TourModel>> getToursByGuide(String guideId) async {
    try {
      Logger.info('Fetching tours by guide: $guideId');

      final response = await client
          .from('tours')
          .select()
          .eq('guide_id', guideId)
          .order('created_at', ascending: false);

      final tours = (response as List)
          .map((json) => TourModel.fromJson(json as Map<String, dynamic>))
          .toList();

      Logger.success('Fetched ${tours.length} tours for guide');
      return tours;
    } catch (e) {
      Logger.error('Error fetching tours by guide', e);
      throw ServerException(message: 'Không thể tải danh sách tour');
    }
  }

  @override
  Future<List<TourModel>> searchTours(String query) async {
    try {
      Logger.info('Searching tours: $query');

      final response = await client
          .from('tours')
          .select()
          .eq('status', 'active')
          .or('title.ilike.%$query%,location.ilike.%$query%')
          .order('created_at', ascending: false);

      final tours = (response as List)
          .map((json) => TourModel.fromJson(json as Map<String, dynamic>))
          .toList();

      Logger.success('Found ${tours.length} tours');
      return tours;
    } catch (e) {
      Logger.error('Error searching tours', e);
      throw ServerException(message: 'Không thể tìm kiếm tour');
    }
  }

  @override
  Future<TourModel> createTour({
    required String guideId,
    required String title,
    String? description,
    required String location,
    required double price,
    required int durationHours,
    int maxParticipants = 10,
    List<String> images = const [],
  }) async {
    try {
      Logger.info('Creating tour: $title');

      final response = await client
          .from('tours')
          .insert({
            'guide_id': guideId,
            'title': title,
            'description': description,
            'location': location,
            'price': price,
            'duration_hours': durationHours,
            'max_participants': maxParticipants,
            'images': images,
            'status': 'active',
          })
          .select()
          .single();

      final tour = TourModel.fromJson(response);
      Logger.success('Tour created: ${tour.id}');
      return tour;
    } catch (e) {
      Logger.error('Error creating tour', e);
      throw ServerException(message: 'Không thể tạo tour');
    }
  }

  @override
  Future<TourModel> updateTour({
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
      Logger.info('Updating tour: $tourId');

      final updates = <String, dynamic>{};
      if (title != null) updates['title'] = title;
      if (description != null) updates['description'] = description;
      if (location != null) updates['location'] = location;
      if (price != null) updates['price'] = price;
      if (durationHours != null) updates['duration_hours'] = durationHours;
      if (maxParticipants != null) {
        updates['max_participants'] = maxParticipants;
      }
      if (images != null) updates['images'] = images;
      if (status != null) updates['status'] = status;

      final response = await client
          .from('tours')
          .update(updates)
          .eq('id', tourId)
          .select()
          .single();

      final tour = TourModel.fromJson(response);
      Logger.success('Tour updated: ${tour.id}');
      return tour;
    } catch (e) {
      Logger.error('Error updating tour', e);
      throw ServerException(message: 'Không thể cập nhật tour');
    }
  }

  @override
  Future<void> deleteTour(String tourId) async {
    try {
      Logger.info('Deleting tour: $tourId');

      await client.from('tours').delete().eq('id', tourId);

      Logger.success('Tour deleted: $tourId');
    } catch (e) {
      Logger.error('Error deleting tour', e);
      throw ServerException(message: 'Không thể xóa tour');
    }
  }
}
