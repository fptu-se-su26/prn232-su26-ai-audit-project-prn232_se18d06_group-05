import 'package:dio/dio.dart';
import '../../../../core/config/api_config.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/services/token_storage.dart';
import '../../../../core/utils/logger.dart';
import '../../domain/entities/tour_entity.dart';
import '../models/tour_model.dart';
import 'tour_remote_datasource.dart';

/// Tour datasource gọi ASP.NET Web API
/// baseUrl = 'http://localhost:5122/api'
/// Backend route: [Route("api/[controller]")] → /api/tours
/// Nên path chỉ cần '/tours' (không thêm /api/ nữa)
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
      Logger.info('[API Tour] GET /tours');
      final res = await _dio.get('/tours');
      final data = res.data as Map<String, dynamic>;
      final list = data['tours'] as List? ?? [];
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
      Logger.info('[API Tour] GET /tours/$tourId');
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
      Logger.info('[API Tour] GET /tours?search=$query');
      final res = await _dio.get('/tours', queryParameters: {'search': query});
      final data = res.data as Map<String, dynamic>;
      final list = data['tours'] as List? ?? [];
      return list
          .map((e) => TourModel.fromApiJson(e as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }

  // ── Get Tours By Guide ─────────────────────────────────────────────────────
  // Backend không có filter guideId qua query — lấy tất cả rồi filter client-side

  @override
  Future<List<TourModel>> getToursByGuide(String guideId) async {
    try {
      Logger.info('[API Tour] GET /tours (filter by guideId=$guideId)');
      final res = await _dio.get('/tours');
      final data = res.data as Map<String, dynamic>;
      final list = data['tours'] as List? ?? [];
      final all = list
          .map((e) => TourModel.fromApiJson(e as Map<String, dynamic>))
          .toList();
      // Filter client-side theo guideId
      return all.where((t) => t.guideId == guideId).toList();
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
      Logger.info('[API Tour] POST /tours (guide=$guideId, title=$title)');
      final opts = await _authOptions();
      final res = await _dio.post(
        '/tours',
        options: opts,
        data: {
          'title': title,
          if (description != null) 'description': description,
          'location': location,
          'price': price,
          'durationHours': durationHours,
          'maxParticipants': maxParticipants,
          if (images.isNotEmpty) 'images': images,
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
      Logger.info('[API Tour] PATCH /tours/$tourId');
      final opts = await _authOptions();

      // Chỉ gửi các field được truyền vào (không null)
      final body = <String, dynamic>{};
      if (title != null) body['title'] = title;
      if (description != null) body['description'] = description;
      if (location != null) body['location'] = location;
      if (price != null) body['price'] = price;
      if (durationHours != null) body['durationHours'] = durationHours;
      if (maxParticipants != null) body['maxParticipants'] = maxParticipants;
      if (images != null) body['images'] = images;
      if (status != null) body['status'] = status;

      final res = await _dio.patch(
        '/tours/$tourId',
        options: opts,
        data: body,
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
      Logger.info('[API Tour] DELETE /tours/$tourId');
      final opts = await _authOptions();
      await _dio.delete('/tours/$tourId', options: opts);
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }

  // ── Get Tour Templates ─────────────────────────────────────────────────────
  // Endpoint /tour-templates chưa có trên backend → trả list rỗng

  @override
  Future<List<TourTemplateEntity>> getTourTemplates() async {
    Logger.info('[API Tour] getTourTemplates — endpoint chưa implement, trả rỗng');
    return [];
  }

  // ── Create Tour From Template ──────────────────────────────────────────────
  // Endpoint /tours/from-template chưa có trên backend → throw NotImplemented

  @override
  Future<TourModel> createTourFromTemplate({
    required String guideId,
    required String templateId,
    required double price,
    required int durationHours,
    int maxParticipants = 10,
  }) async {
    throw ServerException(
      message: 'Tính năng tạo tour từ template chưa được hỗ trợ',
    );
  }

  // ── Error mapping ──────────────────────────────────────────────────────────

  AppException _mapError(DioException e) {
    final statusCode = e.response?.statusCode;
    final body = e.response?.data;
    String msg = 'Lỗi kết nối tới server';

    if (body is Map && body['message'] != null) {
      msg = body['message'] as String;
    } else if (e.type == DioExceptionType.connectionError) {
      msg = 'Không thể kết nối tới server. Kiểm tra API đang chạy.';
    } else if (e.type == DioExceptionType.connectionTimeout ||
        e.type == DioExceptionType.receiveTimeout) {
      msg = 'Kết nối quá thời gian.';
    }

    Logger.error('[API Tour] HTTP $statusCode — $msg');
    if (statusCode == 401) return AppAuthException(message: msg);
    return ServerException(message: msg);
  }
}
