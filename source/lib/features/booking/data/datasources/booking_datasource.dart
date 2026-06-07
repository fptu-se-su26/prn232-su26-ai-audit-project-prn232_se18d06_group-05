import 'package:dio/dio.dart';
import '../../../../core/config/api_config.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/services/token_storage.dart';
import '../../../../core/utils/logger.dart';
import '../models/booking_model.dart';
import '../../domain/entities/tour_availability_entity.dart';

class BookingDataSource {
  late final Dio _dio;

  BookingDataSource() {
    _dio = Dio(
      BaseOptions(
        baseUrl: ApiConfig.baseUrl,
        connectTimeout: ApiConfig.timeout,
        receiveTimeout: ApiConfig.timeout,
      ),
    );
  }

  Future<Options> _auth() async {
    final token = await TokenStorage.getAccessToken();
    return Options(
      headers: {
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      },
    );
  }

  /// Tạo booking mới
  /// Schema mới: gửi tourAvailabilityId thay vì tourId + tourDate
  Future<BookingModel> createBooking({
    required String tourAvailabilityId,
    required int guests,
    String? note,
  }) async {
    try {
      Logger.info(
        '[Booking] POST /bookings (availabilityId=$tourAvailabilityId, guests=$guests)',
      );
      final res = await _dio.post(
        '/bookings',
        options: await _auth(),
        data: {
          'tourAvailabilityId': tourAvailabilityId,
          'guests': guests,
          if (note != null && note.isNotEmpty) 'note': note,
        },
      );

      final data = res.data;
      if (data is Map<String, dynamic>) {
        return BookingModel.fromJson(data);
      }
      throw ServerException(message: 'Định dạng response không hợp lệ');
    } on DioException catch (e) {
      throw _map(e);
    }
  }

  /// Lấy danh sách booking của user hiện tại
  Future<List<BookingModel>> getMyBookings() async {
    try {
      Logger.info('[Booking] GET /bookings/my');
      final res = await _dio.get('/bookings/my', options: await _auth());
      final data = res.data;

      List<dynamic> list;
      if (data is Map<String, dynamic> && data['bookings'] is List) {
        list = data['bookings'] as List;
      } else if (data is List) {
        list = data;
      } else {
        return [];
      }

      return list
          .map((e) => BookingModel.fromJson(e as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw _map(e);
    }
  }

  /// Hủy booking
  Future<void> cancelBooking(String bookingId) async {
    try {
      Logger.info('[Booking] DELETE /bookings/$bookingId');
      await _dio.delete('/bookings/$bookingId', options: await _auth());
    } on DioException catch (e) {
      throw _map(e);
    }
  }

  /// Lấy danh sách ngày trống của một guide_tour
  Future<List<TourAvailabilityEntity>> getAvailability(
    String guideTourId,
  ) async {
    try {
      Logger.info('[Booking] GET /bookings/availability/$guideTourId');
      final res = await _dio.get(
        '/bookings/availability/$guideTourId',
        options: Options(headers: {'Content-Type': 'application/json'}),
      );

      final data = res.data;
      List<dynamic> list;
      if (data is Map<String, dynamic> && data['availability'] is List) {
        list = data['availability'] as List;
      } else if (data is List) {
        list = data;
      } else {
        return [];
      }

      return list
          .map((e) => _mapAvailability(e as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw _map(e);
    }
  }

  TourAvailabilityEntity _mapAvailability(Map<String, dynamic> json) {
    return TourAvailabilityEntity(
      id: (json['id'] ?? '') as String,
      guideTourId:
          (json['guideTourId'] ?? json['guide_tour_id'] ?? '') as String,
      date: DateTime.parse(
        json['date']?.toString().contains('T') == true
            ? json['date'].toString()
            : '${json['date']}T00:00:00.000Z',
      ),
      remainingSlots:
          (json['remainingSlots'] ?? json['remaining_slots'] ?? 0) as int,
    );
  }

  AppException _map(DioException e) {
    final body = e.response?.data;
    String msg = 'Lỗi kết nối';
    if (body is Map && body['message'] != null) msg = body['message'] as String;
    Logger.error('[Booking] ${e.response?.statusCode} — $msg');
    return ServerException(message: msg);
  }
}
