import 'package:dio/dio.dart';
import '../../../../core/config/api_config.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/services/token_storage.dart';
import '../../../../core/utils/logger.dart';
import '../models/booking_model.dart';

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

  Future<BookingModel> createBooking({
    required String tourId,
    required DateTime tourDate,
    required int guests,
    String? note,
  }) async {
    try {
      final res = await _dio.post(
        '/bookings',
        options: await _auth(),
        data: {
          'tourId': tourId,
          'tourDate':
              '${tourDate.year.toString().padLeft(4, '0')}-${tourDate.month.toString().padLeft(2, '0')}-${tourDate.day.toString().padLeft(2, '0')}',
          'guests': guests,
          'note': ?note,
        },
      );
      return BookingModel.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _map(e);
    }
  }

  Future<List<BookingModel>> getMyBookings() async {
    try {
      final res = await _dio.get('/bookings/my', options: await _auth());
      final data = res.data as Map<String, dynamic>;
      return (data['bookings'] as List)
          .map((e) => BookingModel.fromJson(e as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw _map(e);
    }
  }

  Future<void> cancelBooking(String bookingId) async {
    try {
      await _dio.delete('/bookings/$bookingId', options: await _auth());
    } on DioException catch (e) {
      throw _map(e);
    }
  }

  AppException _map(DioException e) {
    final body = e.response?.data;
    String msg = 'Lỗi kết nối';
    if (body is Map && body['message'] != null) msg = body['message'] as String;
    Logger.error('[Booking] ${e.response?.statusCode} — $msg');
    return ServerException(message: msg);
  }
}
