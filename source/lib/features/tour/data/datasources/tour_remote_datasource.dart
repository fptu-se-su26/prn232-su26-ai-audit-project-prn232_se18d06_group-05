import 'package:supabase_flutter/supabase_flutter.dart';
import '../../../../core/config/supabase_config.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/utils/logger.dart';
import '../../domain/entities/tour_entity.dart';
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
  Future<List<TourTemplateEntity>> getTourTemplates();
  Future<TourModel> createTourFromTemplate({
    required String guideId,
    required String templateId,
    required double price,
    required int durationHours,
    int maxParticipants = 10,
  });
}

class TourRemoteDataSourceImpl implements TourRemoteDataSource {
  final SupabaseClient client;

  TourRemoteDataSourceImpl({SupabaseClient? client})
    : client = client ?? SupabaseConfig.client;

  @override
  Future<List<TourModel>> getTours() async {
    try {
      Logger.info('Fetching all tours');

      // Join guide_tours with tour_templates to get complete tour info
      final response = await client
          .from('guide_tours')
          .select('''
            id,
            tour_template_id,
            guide_id,
            price,
            duration_hours,
            max_participants,
            status,
            rating,
            total_reviews,
            tour_templates!inner(
              title,
              description,
              location,
              images,
              created_at
            )
          ''')
          .eq('status', 'active')
          .order('rating', ascending: false);

      final tours = (response as List)
          .map((json) => _mapJoinedTourData(json))
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
          .from('guide_tours')
          .select('''
            id,
            tour_template_id,
            guide_id,
            price,
            duration_hours,
            max_participants,
            status,
            rating,
            total_reviews,
            tour_templates!inner(
              title,
              description,
              location,
              images,
              created_at
            )
          ''')
          .eq('id', tourId)
          .single();

      final tour = _mapJoinedTourData(response);
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
          .from('guide_tours')
          .select('''
            id,
            tour_template_id,
            guide_id,
            price,
            duration_hours,
            max_participants,
            status,
            rating,
            total_reviews,
            tour_templates!inner(
              title,
              description,
              location,
              images,
              created_at
            )
          ''')
          .eq('guide_id', guideId)
          .order('rating', ascending: false);

      final tours = (response as List)
          .map((json) => _mapJoinedTourData(json))
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
          .from('guide_tours')
          .select('''
            id,
            tour_template_id,
            guide_id,
            price,
            duration_hours,
            max_participants,
            status,
            rating,
            total_reviews,
            tour_templates!inner(
              title,
              description,
              location,
              images,
              created_at
            )
          ''')
          .eq('status', 'active')
          .or(
            'tour_templates.title.ilike.%$query%,tour_templates.location.ilike.%$query%',
          )
          .order('rating', ascending: false);

      final tours = (response as List)
          .map((json) => _mapJoinedTourData(json))
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

      // First, create or find tour template
      String templateId;

      // Check if template with same title and location exists
      final existingTemplate = await client
          .from('tour_templates')
          .select('id')
          .eq('title', title)
          .eq('location', location)
          .maybeSingle();

      if (existingTemplate != null) {
        templateId = existingTemplate['id'] as String;
      } else {
        // Create new template
        final templateResponse = await client
            .from('tour_templates')
            .insert({
              'title': title,
              'description': description,
              'location': location,
              'images': images,
            })
            .select('id')
            .single();

        templateId = templateResponse['id'] as String;
      }

      // Create guide tour
      final tourResponse = await client
          .from('guide_tours')
          .insert({
            'tour_template_id': templateId,
            'guide_id': guideId,
            'price': price,
            'duration_hours': durationHours,
            'max_participants': maxParticipants,
            'status': 'active',
          })
          .select('''
            id,
            tour_template_id,
            guide_id,
            price,
            duration_hours,
            max_participants,
            status,
            rating,
            total_reviews,
            tour_templates!inner(
              title,
              description,
              location,
              images,
              created_at
            )
          ''')
          .single();

      final tour = _mapJoinedTourData(tourResponse);
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

      // Update guide_tours table
      final guideTourUpdates = <String, dynamic>{};
      if (price != null) guideTourUpdates['price'] = price;
      if (durationHours != null) {
        guideTourUpdates['duration_hours'] = durationHours;
      }
      if (maxParticipants != null) {
        guideTourUpdates['max_participants'] = maxParticipants;
      }
      if (status != null) guideTourUpdates['status'] = status;

      if (guideTourUpdates.isNotEmpty) {
        await client
            .from('guide_tours')
            .update(guideTourUpdates)
            .eq('id', tourId);
      }

      // Update template if needed
      if (title != null ||
          description != null ||
          location != null ||
          images != null) {
        // Get template ID first
        final tourInfo = await client
            .from('guide_tours')
            .select('tour_template_id')
            .eq('id', tourId)
            .single();

        final templateId = tourInfo['tour_template_id'] as String;

        final templateUpdates = <String, dynamic>{};
        if (title != null) templateUpdates['title'] = title;
        if (description != null) templateUpdates['description'] = description;
        if (location != null) templateUpdates['location'] = location;
        if (images != null) templateUpdates['images'] = images;

        if (templateUpdates.isNotEmpty) {
          await client
              .from('tour_templates')
              .update(templateUpdates)
              .eq('id', templateId);
        }
      }

      // Fetch updated tour
      return await getTourById(tourId);
    } catch (e) {
      Logger.error('Error updating tour', e);
      throw ServerException(message: 'Không thể cập nhật tour');
    }
  }

  @override
  Future<void> deleteTour(String tourId) async {
    try {
      Logger.info('Deleting tour: $tourId');

      await client.from('guide_tours').delete().eq('id', tourId);

      Logger.success('Tour deleted: $tourId');
    } catch (e) {
      Logger.error('Error deleting tour', e);
      throw ServerException(message: 'Không thể xóa tour');
    }
  }

  @override
  Future<List<TourTemplateEntity>> getTourTemplates() async {
    try {
      Logger.info('Fetching tour templates');

      final response = await client
          .from('tour_templates')
          .select()
          .order('created_at', ascending: false);

      final templates = (response as List)
          .map(
            (json) => TourTemplateEntity(
              id: json['id'] as String,
              title: json['title'] as String,
              description: json['description'] as String?,
              location: json['location'] as String,
              images: json['images'] != null
                  ? List<String>.from(json['images'] as List)
                  : [],
              createdAt: DateTime.parse(json['created_at'] as String),
            ),
          )
          .toList();

      Logger.success('Fetched ${templates.length} templates');
      return templates;
    } catch (e) {
      Logger.error('Error fetching tour templates', e);
      throw ServerException(message: 'Không thể tải danh sách mẫu tour');
    }
  }

  @override
  Future<TourModel> createTourFromTemplate({
    required String guideId,
    required String templateId,
    required double price,
    required int durationHours,
    int maxParticipants = 10,
  }) async {
    try {
      Logger.info('Creating tour from template: $templateId');

      final response = await client
          .from('guide_tours')
          .insert({
            'tour_template_id': templateId,
            'guide_id': guideId,
            'price': price,
            'duration_hours': durationHours,
            'max_participants': maxParticipants,
            'status': 'active',
          })
          .select('''
            id,
            tour_template_id,
            guide_id,
            price,
            duration_hours,
            max_participants,
            status,
            rating,
            total_reviews,
            tour_templates!inner(
              title,
              description,
              location,
              images,
              created_at
            )
          ''')
          .single();

      final tour = _mapJoinedTourData(response);
      Logger.success('Tour created from template: ${tour.id}');
      return tour;
    } catch (e) {
      Logger.error('Error creating tour from template', e);
      throw ServerException(message: 'Không thể tạo tour từ mẫu');
    }
  }

  /// Helper method to map joined tour data
  TourModel _mapJoinedTourData(Map<String, dynamic> json) {
    final template = json['tour_templates'] as Map<String, dynamic>;

    return TourModel(
      id: json['id'] as String,
      tourTemplateId: json['tour_template_id'] as String,
      guideId: json['guide_id'] as String,
      price: (json['price'] as num).toDouble(),
      durationHours: json['duration_hours'] as int,
      maxParticipants: json['max_participants'] as int? ?? 10,
      status: json['status'] as String? ?? 'active',
      rating: json['rating'] != null ? (json['rating'] as num).toDouble() : 0.0,
      totalReviews: json['total_reviews'] as int? ?? 0,
      title: template['title'] as String,
      description: template['description'] as String?,
      location: template['location'] as String,
      images: template['images'] != null
          ? List<String>.from(template['images'] as List)
          : [],
      createdAt: DateTime.parse(template['created_at'] as String),
      updatedAt: DateTime.now(), // Use current time as updated_at
    );
  }
}
