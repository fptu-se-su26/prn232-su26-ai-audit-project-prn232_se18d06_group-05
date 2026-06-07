import 'package:dio/dio.dart';

void main() async {
  final dio = Dio();
  try {
    final formData = FormData.fromMap({
      'email': 'lmp145892@gmail.com',
      'password': 'password123',
      'fullName': 'Test User',
      'role': 'traveler',
      'phoneNumber': '0123456789',
    });
    
    print('Sending request...');
    final res = await dio.post(
      'http://localhost:5122/api/auth/register',
      data: formData,
    );
    print('Success: \${res.data}');
  } on DioException catch (e) {
    print('Error \${e.response?.statusCode}');
    print(e.response?.data);
  } catch (e) {
    print('Unknown error: $e');
  }
}
