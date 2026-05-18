import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import 'package:supabase_flutter/supabase_flutter.dart';
import '../../../../core/config/supabase_config.dart';
import '../../domain/entities/booking_entity.dart';
import '../providers/booking_provider.dart';
import '../providers/booking_realtime_provider.dart';
import '../../../chat/presentation/screens/chat_screen.dart';
import '../../../chat/presentation/providers/chat_provider.dart';

class BookingDetailScreen extends ConsumerWidget {
  final String bookingId;
  const BookingDetailScreen({super.key, required this.bookingId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Listen to realtime updates
    final realtimeState = ref.watch(bookingRealtimeProvider(bookingId));
    final fmt = NumberFormat.currency(locale: 'vi_VN', symbol: '₫', decimalDigits: 0);
    final dateFmt = DateFormat('EEEE, dd/MM/yyyy', 'vi');
    final timeFmt = DateFormat('HH:mm');

    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        title: const Text('Chi tiết booking', style: TextStyle(fontWeight: FontWeight.bold)),
        backgroundColor: Colors.white,
        foregroundColor: Colors.black,
        elevation: 0,
        actions: [
          // Realtime connection indicator
          IconButton(
            icon: Icon(
              realtimeState.isRealtimeConnected ? Icons.cloud_done : Icons.cloud_off,
              color: realtimeState.isRealtimeConnected ? Colors.green : Colors.grey,
            ),
            tooltip: realtimeState.isRealtimeConnected ? 'Realtime: Kết nối' : 'Realtime: Ngắt kết nối',
            onPressed: () => ref.read(bookingRealtimeProvider(bookingId).notifier).refresh(),
          ),
        ],
      ),
      body: realtimeState.isLoading
          ? const Center(child: CircularProgressIndicator())
          : realtimeState.error != null
              ? _buildErrorState(ref, realtimeState.error!)
              : realtimeState.booking == null
                  ? _buildNotFoundState()
                  : _buildContent(ref, realtimeState.booking!, fmt, dateFmt),
    );
  }

  Widget _buildErrorState(WidgetRef ref, String error) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.error_outline, size: 64, color: Colors.red),
          const SizedBox(height: 16),
          Text(
            'Không thể tải booking',
            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          Text(
            error,
            style: TextStyle(color: Colors.grey.shade600),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 24),
          ElevatedButton(
            onPressed: () => ref.read(bookingRealtimeProvider(bookingId).notifier).connect(),
            child: const Text('Thử lại'),
          ),
        ],
      ),
    );
  }

  Widget _buildNotFoundState() {
    return const Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.event_busy, size: 64, color: Colors.grey),
          SizedBox(height: 16),
          Text(
            'Không tìm thấy booking',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
          ),
        ],
      ),
    );
  }

  Widget _buildContent(
    WidgetRef ref,
    BookingEntity booking,
    NumberFormat fmt,
    DateFormat dateFmt,
  ) {
    return RefreshIndicator(
      onRefresh: () async => ref.read(bookingRealtimeProvider(bookingId).notifier).refresh(),
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Status badge
            _buildStatusBadge(booking),
            const SizedBox(height: 24),

            // Booking info card
            _buildBookingInfoCard(booking, fmt, dateFmt),
            const SizedBox(height: 24),

            // Tour details
            _buildTourDetails(booking),
            const SizedBox(height: 24),

            // Timeline
            _buildTimeline(booking),
            const SizedBox(height: 24),

            // Action buttons
            _buildActionButtons(context, ref, booking),
            const SizedBox(height: 32),
          ],
        ),
      ),
    );
  }

  Widget _buildStatusBadge(BookingEntity booking) {
    final (color, label, icon) = _getStatusConfig(booking.status);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      decoration: BoxDecoration(
        color: color.withOpacity(0.1),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: color, width: 1.5),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 18, color: color),
          const SizedBox(width: 8),
          Text(
            label,
            style: TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.bold,
              color: color,
            ),
          ),
        ],
      ),
    );
  }

  (Color color, String label, IconData icon) _getStatusConfig(String status) {
    switch (status) {
      case 'pending':
        return (Colors.orange, 'Chờ xác nhận', Icons.schedule);
      case 'confirmed':
        return (Colors.blue, 'Đã xác nhận', Icons.check_circle);
      case 'preparing':
        return (Colors.purple, 'Đang chuẩn bị', Icons.prep);
      case 'inProgress':
        return (Colors.blueAccent, 'Đang diễn ra', Icons.directions_walk);
      case 'completed':
        return (Colors.green, 'Hoàn thành', Icons.done_all);
      case 'cancelled':
        return (Colors.red, 'Đã hủy', Icons.cancel);
      case 'refunded':
        return (Colors.teal, 'Đã hoàn tiền', Icons.money_back);
      default:
        return (Colors.grey, 'Không rõ', Icons.help);
    }
  }

  Widget _buildBookingInfoCard(BookingEntity booking, NumberFormat fmt, DateFormat dateFmt) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.grey.shade50,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: Colors.grey.shade200),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.receipt_long, color: Color(0xFFE91E8C)),
              const SizedBox(width: 8),
              const Text(
                'Thông tin booking',
                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
              ),
            ],
          ),
          const SizedBox(height: 16),
          _InfoRow('Mã booking', '#${booking.id.substring(0, 8).toUpperCase()}'),
          const SizedBox(height: 12),
          _InfoRow('Tour', booking.tourTitle),
          const SizedBox(height: 12),
          _InfoRow('Địa điểm', booking.tourLocation),
          const SizedBox(height: 12),
          _InfoRow('Ngày tour', dateFmt.format(booking.tourDate)),
          const SizedBox(height: 12),
          _InfoRow('Số khách', '${booking.guests} người'),
          const Divider(height: 24),
          _InfoRow(
            'Tổng tiền',
            fmt.format(booking.totalPrice),
            valueColor: const Color(0xFFE91E8C),
            valueBold: true,
          ),
          const SizedBox(height: 12),
          _InfoRow(
            'Đặt lúc',
            '${dateFmt.format(booking.createdAt)} ${timeFmt.format(booking.createdAt)}',
          ),
        ],
      ),
    );
  }

  Widget _buildTourDetails(BookingEntity booking) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Tour',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 12),
        Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            border: Border.all(color: Colors.grey.shade300),
            borderRadius: BorderRadius.circular(12),
          ),
          child: Row(
            children: [
              Container(
                width: 60,
                height: 60,
                decoration: BoxDecoration(
                  color: Colors.grey.shade200,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: const Icon(Icons.tour, color: Colors.grey),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      booking.tourTitle,
                      style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 4),
                    Text(
                      booking.tourLocation,
                      style: TextStyle(fontSize: 12, color: Colors.grey.shade600),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildTimeline(BookingEntity booking) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Tiến trình booking',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 16),
        _TimelineItem(
          icon: Icons.check_circle,
          title: 'Đã đặt',
          subtitle: 'Booking được tạo',
          date: booking.createdAt,
          isActive: true,
          isCompleted: true,
        ),
        _TimelineItem(
          icon: Icons.verified,
          title: 'Xác nhận',
          subtitle: 'Guide xác nhận booking',
          date: null,
          isActive: booking.isConfirmed || booking.isCompleted,
          isCompleted: booking.isConfirmed || booking.isCompleted,
        ),
        _TimelineItem(
          icon: Icons.calendar_month,
          title: 'Ngày tour',
          subtitle: 'Tour diễn ra',
          date: booking.tourDate,
          isActive: false,
          isCompleted: booking.isCompleted,
        ),
        _TimelineItem(
          icon: Icons.star,
          title: 'Đánh giá',
          subtitle: 'Để lại review',
          date: null,
          isActive: false,
          isCompleted: false,
          locked: !booking.isCompleted,
        ),
      ],
    );
  }

  Widget _buildActionButtons(BuildContext context, WidgetRef ref, BookingEntity booking) {
    return Column(
      children: [
        // Chat with guide button
        SizedBox(
          width: double.infinity,
          child: ElevatedButton.icon(
            onPressed: () => _startChat(context, ref, booking),
            icon: const Icon(Icons.chat_bubble_outline),
            label: const Text('Nhắn tin với hướng dẫn viên'),
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFFE91E8C),
              foregroundColor: Colors.white,
              padding: const EdgeInsets.symmetric(vertical: 16),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            ),
          ),
        ),
        const SizedBox(height: 12),

        // Cancel button (only for pending/confirmed and >24h)
        if (booking.isPending || booking.isConfirmed) ...[
          SizedBox(
            width: double.infinity,
            child: OutlinedButton.icon(
              onPressed: () => _showCancelDialog(context, ref, booking),
              icon: const Icon(Icons.cancel_outlined, color: Colors.red),
              label: const Text('Hủy booking', style: TextStyle(color: Colors.red)),
              style: OutlinedButton.styleFrom(
                side: const BorderSide(color: Colors.red),
                padding: const EdgeInsets.symmetric(vertical: 16),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              ),
            ),
          ),
        ],
      ],
    );
  }

  Future<void> _startChat(BuildContext context, WidgetRef ref, BookingEntity booking) async {
    final conversationId = await ref
        .read(createConversationProvider.notifier)
        .createOrGet(guideId: booking.guideId, bookingId: booking.id);

    if (!context.mounted) return;

    if (conversationId != null) {
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
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Không thể tạo cuộc trò chuyện')),
      );
    }
  }

  void _showCancelDialog(BuildContext context, WidgetRef ref, BookingEntity booking) {
    showDialog(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Hủy booking'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Bạn có chắc muốn hủy booking này?'),
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.orange.shade50,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(Icons.info, size: 16, color: Colors.orange.shade700),
                      const SizedBox(width: 8),
                      Text(
                        'Chính sách hủy',
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          color: Colors.orange.shade700,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Text(
                    '• Hủy trước 48h: Hoàn 100%\n'
                    '• Hủy 24h-48h: Hoàn 50%\n'
                    '• Hủy <24h: Không hoàn tiền',
                    style: TextStyle(fontSize: 12, color: Colors.orange.shade800),
                  ),
                ],
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Huỷ'),
          ),
          TextButton(
            onPressed: () async {
              await ref.read(myBookingsProvider.notifier).cancel(booking.id);
              if (context.mounted) {
                Navigator.pop(context);
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(content: Text('Đã hủy booking thành công')),
                );
              }
            },
            style: TextButton.styleFrom(foregroundColor: Colors.red),
            child: const Text('Hủy'),
          ),
        ],
      ),
    );
  }
}

// ── Info Row ──────────────────────────────────────────────────────────────────

class _InfoRow extends StatelessWidget {
  final String label;
  final String value;
  final Color? valueColor;
  final bool valueBold;

  const _InfoRow(this.label, this.value, {this.valueColor, this.valueBold = false});

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          width: 100,
          child: Text(
            label,
            style: TextStyle(fontSize: 13, color: Colors.grey.shade600),
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: TextStyle(
              fontSize: 14,
              fontWeight: valueBold ? FontWeight.bold : FontWeight.w600,
              color: valueColor ?? Colors.black87,
            ),
          ),
        ),
      ],
    );
  }
}

// ── Timeline Item ─────────────────────────────────────────────────────────────

class _TimelineItem extends StatelessWidget {
  final IconData icon;
  final String title;
  final String subtitle;
  final DateTime? date;
  final bool isActive;
  final bool isCompleted;
  final bool locked;

  const _TimelineItem({
    required this.icon,
    required this.title,
    required this.subtitle,
    this.date,
    this.isActive = false,
    this.isCompleted = false,
    this.locked = false,
  });

  @override
  Widget build(BuildContext context) {
    final color = isCompleted
        ? const Color(0xFFE91E8C)
        : isActive
            ? Colors.orange
            : Colors.grey.shade400;

    return Padding(
      padding: const EdgeInsets.only(bottom: 20),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Icon
          Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(
              color: color.withOpacity(0.1),
              shape: BoxShape.circle,
              border: Border.all(color: color, width: 2),
            ),
            child: Icon(
              locked ? Icons.lock : icon,
              size: 20,
              color: color,
            ),
          ),
          const SizedBox(width: 12),
          // Content
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                    color: isCompleted || isActive ? Colors.black87 : Colors.grey.shade500,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  subtitle,
                  style: TextStyle(
                    fontSize: 12,
                    color: Colors.grey.shade600,
                  ),
                ),
                if (date != null) ...[
                  const SizedBox(height: 4),
                  Text(
                    '${date!.day}/${date!.month}/${date!.year}',
                    style: TextStyle(
                      fontSize: 11,
                      color: Colors.grey.shade500,
                    ),
                  ),
                ],
              ],
            ),
          ),
          // Status indicator
          if (isCompleted)
            const Icon(Icons.check_circle, size: 20, color: Color(0xFFE91E8C))
          else if (isActive)
            const Icon(Icons.schedule, size: 20, color: Colors.orange),
        ],
      ),
    );
  }
}
