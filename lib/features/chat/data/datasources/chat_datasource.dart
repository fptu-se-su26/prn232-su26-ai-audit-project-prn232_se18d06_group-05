import 'package:dio/dio.dart';
import '../../../../core/config/api_config.dart';
import '../../../../core/services/token_storage.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/utils/logger.dart';

class ChatDataSource {
  late final Dio _dio;

  ChatDataSource() {
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

  Future<String> createOrGetConversation({
    required String guideId,
    required String bookingId,
  }) async {
    try {
      final res = await _dio.post(
        '/chat/conversations',
        options: await _auth(),
        data: {'guideId': guideId, 'bookingId': bookingId},
      );
      return res.data['id'] as String;
    } on DioException catch (e) {
      throw _map(e);
    }
  }

  Future<List<ConversationItem>> getMyConversations() async {
    try {
      final res = await _dio.get('/chat/conversations', options: await _auth());
      final data = res.data as Map<String, dynamic>;
      return (data['conversations'] as List)
          .map((e) => ConversationItem.fromJson(e as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      // If table doesn't exist yet, return empty list instead of throwing
      if (e.response?.statusCode == 404 ||
          e.response?.data?.toString().contains('table') == true) {
        Logger.warning(
          '[Chat] Database tables not ready yet, returning empty list',
        );
        return [];
      }
      throw _map(e);
    }
  }

  AppException _map(DioException e) {
    final body = e.response?.data;
    String msg = 'Lỗi kết nối';
    if (body is Map && body['message'] != null) msg = body['message'] as String;
    Logger.error('[Chat] ${e.response?.statusCode} — $msg');
    return ServerException(message: msg);
  }
}

class ConversationItem {
  final String id;
  final String travelerId;
  final String guideId;
  final String? bookingId;
  final DateTime createdAt;

  const ConversationItem({
    required this.id,
    required this.travelerId,
    required this.guideId,
    this.bookingId,
    required this.createdAt,
  });

  factory ConversationItem.fromJson(Map<String, dynamic> json) {
    return ConversationItem(
      id: json['id'] as String,
      travelerId: json['travelerId'] as String,
      guideId: json['guideId'] as String,
      bookingId: json['bookingId'] as String?,
      createdAt: DateTime.parse(json['createdAt'] as String),
    );
  }
}
