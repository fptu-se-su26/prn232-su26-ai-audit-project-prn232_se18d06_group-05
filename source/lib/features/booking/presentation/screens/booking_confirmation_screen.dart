import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../domain/entities/booking_entity.dart';
import '../../../chat/presentation/screens/conversation_list_screen.dart';
import '../../../chat/presentation/screens/chat_screen.dart';
import '../../../chat/presentation/providers/chat_provider.dart';

class BookingConfirmationScreen extends ConsumerWidget {
  final BookingEntity booking;
  const BookingConfirmationScreen({super.key, required this.booking});

  Future<void> _startChatWithGuide(BuildContext context, WidgetRef ref) async {
    final guideId = booking.guideId;
    if (guideId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Không thể tìm thấy thông tin hướng dẫn viên'),
        ),
      );
      return;
    }

    final conversationId = await ref
        .read(createConversationProvider.notifier)
        .createOrGet(guideId: guideId, bookingId: booking.id);

    if (!context.mounted) return;

    if (conversationId != null) {
      // Navigate trực tiếp đến chat screen
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (_) => ChatScreen(
            conversationId: conversationId,
            otherUserName: 'Hướng dẫn viên',
          ),
        ),
      );
    } else {
      // Fallback: mở conversation list
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Không thể tạo cuộc trò chuyện')),
      );
      Navigator.push(
        context,
        MaterialPageRoute(builder: (_) => const ConversationListScreen()),
      );
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final fmt = NumberFormat.currency(
      locale: 'vi_VN',
      symbol: '₫',
      decimalDigits: 0,
    );
    final dateFmt = DateFormat('EEEE, dd/MM/yyyy', 'vi');

    return Scaffold(
      backgroundColor: Colors.white,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            children: [
              const Spacer(),

              // Success icon
              Container(
                width: 96,
                height: 96,
                decoration: const BoxDecoration(
                  color: Color(0xFFE8F5E9),
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.check_circle,
                  size: 56,
                  color: Colors.green,
                ),
              ),
              const SizedBox(height: 24),

              const Text(
                'Đặt tour thành công!',
                style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              Text(
                'Mã booking: #${booking.id.substring(0, 8).toUpperCase()}',
                style: TextStyle(fontSize: 14, color: Colors.grey.shade600),
              ),

              const SizedBox(height: 32),

              // Booking details card
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(20),
                decoration: BoxDecoration(
                  color: Colors.grey.shade50,
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: Colors.grey.shade200),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _DetailRow(
                      Icons.tour,
                      'Tour',
                      booking.tourTitle ?? 'Không xác định',
                    ),
                    const SizedBox(height: 12),
                    _DetailRow(
                      Icons.location_on,
                      'Địa điểm',
                      booking.tourLocation ?? 'Không xác định',
                    ),
                    const SizedBox(height: 12),
                    _DetailRow(
                      Icons.calendar_today,
                      'Ngày',
                      dateFmt.format(booking.tourDate),
                    ),
                    const SizedBox(height: 12),
                    _DetailRow(
                      Icons.people,
                      'Số khách',
                      '${booking.guests} người',
                    ),
                    const SizedBox(height: 12),
                    _DetailRow(
                      Icons.payments,
                      'Tổng tiền',
                      fmt.format(booking.totalPrice),
                      valueColor: const Color(0xFFE91E8C),
                    ),
                    const SizedBox(height: 12),
                    _DetailRow(
                      Icons.info_outline,
                      'Trạng thái',
                      'Chờ xác nhận',
                      valueColor: Colors.orange,
                    ),
                  ],
                ),
              ),

              const SizedBox(height: 16),

              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: const Color(0xFFFFF8E1),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.info, size: 18, color: Colors.orange),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'Hướng dẫn viên sẽ xác nhận trong vòng 24 giờ.',
                        style: TextStyle(
                          fontSize: 13,
                          color: Colors.orange.shade800,
                        ),
                      ),
                    ),
                  ],
                ),
              ),

              const Spacer(),

              // Buttons
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: () =>
                      Navigator.of(context).popUntil((r) => r.isFirst),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFFE91E8C),
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                    elevation: 0,
                  ),
                  child: const Text(
                    'Về trang chủ',
                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
                  ),
                ),
              ),
              const SizedBox(height: 12),
              TextButton(
                onPressed: () =>
                    Navigator.of(context).popUntil((r) => r.isFirst),
                child: const Text('Xem chuyến đi của tôi'),
              ),
              const SizedBox(height: 8),
              TextButton.icon(
                onPressed: () => _startChatWithGuide(context, ref),
                icon: const Icon(
                  Icons.chat_bubble_outline,
                  color: Color(0xFFE91E8C),
                ),
                label: const Text(
                  'Nhắn tin với hướng dẫn viên',
                  style: TextStyle(color: Color(0xFFE91E8C)),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _DetailRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Color? valueColor;
  const _DetailRow(this.icon, this.label, this.value, {this.valueColor});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(icon, size: 18, color: Colors.grey.shade500),
        const SizedBox(width: 10),
        Text(
          '$label: ',
          style: TextStyle(fontSize: 14, color: Colors.grey.shade600),
        ),
        Expanded(
          child: Text(
            value,
            style: TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w600,
              color: valueColor ?? Colors.black87,
            ),
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ],
    );
  }
}
