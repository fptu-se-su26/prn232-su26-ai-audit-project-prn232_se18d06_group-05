import 'package:dio/dio.dart';
import '../../../../core/config/api_config.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/services/token_storage.dart';
import '../../../../core/utils/logger.dart';
import '../../domain/entities/tour_entity.dart';
import '../models/tour_model.dart';
import 'tour_remote_datasource.dart';

/// Tour datasource gọi ASP.NET Web API
class ApiTourDataSource implements TourRemoteDataSource {
  late final Dio _dio;

  ApiTourDataSource() {
    _dio = Dio(
      BaseOptions(
        baseUrl: ApiConfig.baseUrl,
        connectTimeout: ApiConfig.timeout,
        receiveTimeout: ApiConfig.timeout,
      ),
    );
    _dio.interceptors.add(
      LogInterceptor(
        requestBody: false,
        responseBody: true,
        logPrint: (o) => Logger.info(o.toString()),
      ),
    );
  }

  // ── Auth header helper ─────────────────────────────────────────────────────

  Future<Options> _authOptions() async {
    final token = await TokenStorage.getAccessToken();
    return Options(
      headers: {
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      },
    );
  }

  // ── Get Tours ──────────────────────────────────────────────────────────────

  @override
  Future<List<TourModel>> getTours() async {
    try {
      final res = await _dio.get('/tours');
      final data = res.data as Map<String, dynamic>;
      final list = data['tours'] as List;
      return list
          .map((e) => TourModel.fromApiJson(e as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }

  // ── Get Tour By Id ─────────────────────────────────────────────────────────

  @override
  Future<TourModel> getTourById(String tourId) async {
    try {
      final res = await _dio.get('/tours/$tourId');
      return TourModel.fromApiJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }

  // ── Search Tours ───────────────────────────────────────────────────────────

  @override
  Future<List<TourModel>> searchTours(String query) async {
    try {
      final res = await _dio.get('/tours', queryParameters: {'search': query});
      final data = res.data as Map<String, dynamic>;
      final list = data['tours'] as List;
      return list
          .map((e) => TourModel.fromApiJson(e as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }

  // ── Get Tours By Guide ─────────────────────────────────────────────────────

  @override
  Future<List<TourModel>> getToursByGuide(String guideId) async {
    try {
      final res = await _dio.get(
        '/tours',
        queryParameters: {'guideId': guideId},
      );
      final data = res.data as Map<String, dynamic>;
      final list = data['tours'] as List;
      return list
          .map((e) => TourModel.fromApiJson(e as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }

  // ── Create Tour ────────────────────────────────────────────────────────────

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
      final opts = await _authOptions();
      final res = await _dio.post(
        '/tours',
        options: opts,
        data: {
          'title': title,
          'description': description,
          'location': location,
          'price': price,
          'durationHours': durationHours,
          'maxParticipants': maxParticipants,
          'images': images,
        },
      );
      return TourModel.fromApiJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }

  // ── Update Tour ────────────────────────────────────────────────────────────

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
      final opts = await _authOptions();
      final res = await _dio.patch(
        '/tours/$tourId',
        options: opts,
        data: {
          'title': ?title,
          'description': ?description,
          'location': ?location,
          'price': ?price,
          'durationHours': ?durationHours,
          'maxParticipants': ?maxParticipants,
          'images': ?images,
          'status': ?status,
        },
      );
      return TourModel.fromApiJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }

  // ── Delete Tour ────────────────────────────────────────────────────────────

  @override
  Future<void> deleteTour(String tourId) async {
    try {
      final opts = await _authOptions();
      await _dio.delete('/tours/$tourId', options: opts);
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }

  // ── Get Tour Templates ─────────────────────────────────────────────────────

  @override
  Future<List<TourTemplateEntity>> getTourTemplates() async {
    try {
      final res = await _dio.get('/tour-templates');
      final data = res.data as Map<String, dynamic>;
      final list = data['templates'] as List;

      return list
          .map(
            (json) => TourTemplateEntity(
              id: json['id'] as String,
              title: json['title'] as String,
              description: json['description'] as String?,
              location: json['location'] as String,
              images: json['images'] != null
                  ? List<String>.from(json['images'] as List)
                  : [],
              createdAt: DateTime.parse(json['createdAt'] as String),
            ),
          )
          .toList();
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }

  // ── Create Tour From Template ──────────────────────────────────────────────

  @override
  Future<TourModel> createTourFromTemplate({
    required String guideId,
    required String templateId,
    required double price,
    required int durationHours,
    int maxParticipants = 10,
  }) async {
    try {
      final opts = await _authOptions();
      final res = await _dio.post(
        '/tours/from-template',
        options: opts,
        data: {
          'templateId': templateId,
          'price': price,
          'durationHours': durationHours,
          'maxParticipants': maxParticipants,
        },
      );
      return TourModel.fromApiJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }

  // ── Error mapping ──────────────────────────────────────────────────────────

  AppException _mapError(DioException e) {
    final body = e.response?.data;
    String msg = 'Lỗi kết nối tới server';
    if (body is Map && body['message'] != null) msg = body['message'] as String;
    if (e.type == DioExceptionType.connectionError) {
      msg = 'Không thể kết nối tới server';
    }
    Logger.error('[API Tour] ${e.response?.statusCode} — $msg');
    return ServerException(message: msg);
  }
}
