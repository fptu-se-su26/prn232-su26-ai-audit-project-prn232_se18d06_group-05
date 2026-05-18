import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../../core/config/supabase_config.dart';
import '../../domain/entities/booking_entity.dart';
import '../providers/booking_provider.dart';
import '../providers/booking_realtime_provider.dart';
import 'booking_detail_screen.dart';

class MyBookingsScreen extends ConsumerStatefulWidget {
  const MyBookingsScreen({super.key});

  @override
  ConsumerState<MyBookingsScreen> createState() => _MyBookingsScreenState();
}

class _MyBookingsScreenState extends ConsumerState<MyBookingsScreen> {
  @override
  void initState() {
    super.initState();
    // Load initial data
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(myBookingsProvider.notifier).load();
    });
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authStateProvider);
    final travelerId = authState.user?.id;

    if (travelerId == null) {
      return const Center(child: Text('Vui lòng đăng nhập'));
    }

    // Watch realtime state
    final realtimeState = ref.watch(myBookingsRealtimeProvider(travelerId));
    final filter = ref.watch(myBookingsProvider).filter;

    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        title: const Text('Chuyến đi của tôi', style: TextStyle(fontWeight: FontWeight.bold)),
        backgroundColor: Colors.white,
        foregroundColor: Colors.black,
        elevation: 0,
        actions: [
          // Realtime status indicator
          Padding(
            padding: const EdgeInsets.only(right: 8),
            child: Center(
              child: Row(
                children: [
                  Icon(
                    realtimeState.isRealtimeConnected ? Icons.cloud_done : Icons.cloud_off,
                    size: 20,
                    color: realtimeState.isRealtimeConnected ? Colors.green : Colors.grey,
                  ),
                  const SizedBox(width: 4),
                  if (realtimeState.isRealtimeConnected)
                    const Text(
                      'Live',
                      style: TextStyle(fontSize: 12, color: Colors.green, fontWeight: FontWeight.bold),
                    ),
                ],
              ),
            ),
          ),
        ],
      ),
      body: Column(
        children: [
          // Filter chips
          _buildFilterBar(),
          // Content
          Expanded(
            child: _buildContent(realtimeState, filter),
          ),
        ],
      ),
    );
  }

  Widget _buildFilterBar() {
    final currentFilter = ref.watch(myBookingsProvider).filter;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border(bottom: BorderSide(color: Colors.grey.shade200)),
      ),
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Row(
          children: [
            _FilterChip('Tất cả', BookingStatusFilter.all),
            const SizedBox(width: 8),
            _FilterChip('Sắp tới', BookingStatusFilter.upcoming),
            const SizedBox(width: 8),
            _FilterChip('Hoàn thành', BookingStatusFilter.completed),
            const SizedBox(width: 8),
            _FilterChip('Đã hủy', BookingStatusFilter.cancelled),
          ],
        ),
      ),
    );
  }

  Widget _FilterChip(String label, BookingStatusFilter filter) {
    final currentFilter = ref.watch(myBookingsProvider).filter;
    final isSelected = currentFilter == filter;
    return FilterChip(
      label: Text(label, style: TextStyle(fontSize: 13)),
      selected: isSelected,
      onSelected: (selected) {
        ref.read(myBookingsProvider.notifier).setFilter(filter);
      },
      selectedColor: const Color(0xFFE91E8C),
      checkmarkColor: Colors.white,
      labelStyle: TextStyle(
        color: isSelected ? Colors.white : Colors.black87,
        fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
      ),
    );
  }

  Widget _buildContent(MyBookingsRealtimeState realtimeState, BookingStatusFilter filter) {
    // Use realtime bookings if available, otherwise fallback to regular provider
    final bookings = realtimeState.bookings.isNotEmpty
        ? realtimeState.bookings
        : ref.watch(myBookingsProvider).bookings;

    final isLoading = realtimeState.isLoading || ref.watch(myBookingsProvider).isLoading;
    final error = realtimeState.error ?? ref.watch(myBookingsProvider).error;

    // Apply filter
    final filteredBookings = _applyFilter(bookings, filter);

    if (isLoading && bookings.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (error != null && bookings.isEmpty) {
      return _buildErrorState(error);
    }

    if (filteredBookings.isEmpty) {
      return _buildEmptyState(filter);
    }

    return RefreshIndicator(
      onRefresh: () async {
        ref.read(myBookingsProvider.notifier).load();
        if (SupabaseConfig.isInitialized) {
          ref.read(myBookingsRealtimeProvider(ref.read(authStateProvider).user!.id).notifier).refresh();
        }
      },
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: filteredBookings.length,
        itemBuilder: (_, index) => _BookingCard(booking: filteredBookings[index]),
      ),
    );
  }

  List<BookingEntity> _applyFilter(List<BookingEntity> bookings, BookingStatusFilter filter) {
    if (filter == BookingStatusFilter.all) return bookings;
    return bookings.where((b) {
      switch (filter) {
        case BookingStatusFilter.upcoming:
          return b.isPending || b.isConfirmed;
        case BookingStatusFilter.completed:
          return b.isCompleted;
        case BookingStatusFilter.cancelled:
          return b.isCancelled;
        case BookingStatusFilter.all:
          return true;
      }
    }).toList();
  }

  Widget _buildErrorState(String error) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.error_outline, size: 64, color: Colors.red),
          const SizedBox(height: 16),
          const Text(
            'Không thể tải bookings',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          Text(
            error,
            style: TextStyle(color: Colors.grey.shade600),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 24),
          ElevatedButton(
            onPressed: () {
              ref.read(myBookingsProvider.notifier).load();
              final travelerId = ref.read(authStateProvider).user?.id;
              if (travelerId != null) {
                ref.read(myBookingsRealtimeProvider(travelerId).notifier).connect();
              }
            },
            child: const Text('Thử lại'),
          ),
        ],
      ),
    );
  }

  Widget _buildEmptyState(BookingStatusFilter filter) {
    final (icon, title, subtitle) = switch (filter) {
      BookingStatusFilter.all => (Icons.card_travel, 'Chưa có booking nào', 'Hãy khám phá và đặt tour ngay!'),
      BookingStatusFilter.upcoming => (Icons.event_busy, 'Chưa có booking sắp tới', 'Bạn chưa có booking nào sắp tới'),
      BookingStatusFilter.completed => (Icons.done_all, 'Chưa có booking hoàn thành', 'Bạn chưa hoàn thành booking nào'),
      BookingStatusFilter.cancelled => (Icons.cancel, 'Không có booking đã hủy', 'Bạn chưa hủy booking nào'),
    };

    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, size: 64, color: Colors.grey.shade300),
          const SizedBox(height: 16),
          Text(
            title,
            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          Text(
            subtitle,
            style: TextStyle(color: Colors.grey.shade600),
          ),
        ],
      ),
    );
  }
}

// ── Booking Card ──────────────────────────────────────────────────────────────

class _BookingCard extends StatelessWidget {
  final BookingEntity booking;
  const _BookingCard({required this.booking});

  @override
  Widget build(BuildContext context) {
    final fmt = NumberFormat.currency(locale: 'vi_VN', symbol: '₫', decimalDigits: 0);
    final dateFmt = DateFormat('dd/MM/yyyy');

    final (statusColor, statusLabel) = _getStatusConfig(booking.status);

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: Colors.grey.shade200),
      ),
      child: InkWell(
        onTap: () => Navigator.push(
          context,
          MaterialPageRoute(builder: (_) => BookingDetailScreen(bookingId: booking.id)),
        ),
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header: Tour title + Status
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          booking.tourTitle,
                          style: const TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 15,
                          ),
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                        const SizedBox(height: 4),
                        Row(
                          children: [
                            const Icon(Icons.location_on, size: 12, color: Colors.grey),
                            const SizedBox(width: 4),
                            Expanded(
                              child: Text(
                                booking.tourLocation,
                                style: TextStyle(fontSize: 12, color: Colors.grey.shade600),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                    decoration: BoxDecoration(
                      color: statusColor.withOpacity(0.1),
                      borderRadius: BorderRadius.circular(20),
                      border: Border.all(color: statusColor, width: 1),
                    ),
                    child: Text(
                      statusLabel,
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        color: statusColor,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),

              // Divider
              Divider(height: 1, color: Colors.grey.shade200),
              const SizedBox(height: 12),

              // Info row
              Row(
                children: [
                  // Date
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            const Icon(Icons.calendar_today, size: 14, color: Colors.grey),
                            const SizedBox(width: 6),
                            Text(
                              dateFmt.format(booking.tourDate),
                              style: TextStyle(fontSize: 13, color: Colors.grey.shade700),
                            ),
                          ],
                        ),
                        const SizedBox(height: 4),
                        Row(
                          children: [
                            const Icon(Icons.people, size: 14, color: Colors.grey),
                            const SizedBox(width: 6),
                            Text(
                              '${booking.guests} người',
                              style: TextStyle(fontSize: 13, color: Colors.grey.shade700),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                  // Price
                  Text(
                    fmt.format(booking.totalPrice),
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFFE91E8C),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  (Color color, String label) _getStatusConfig(String status) {
    switch (status) {
      case 'pending':
        return (Colors.orange, 'Chờ xác nhận');
      case 'confirmed':
        return (Colors.blue, 'Đã xác nhận');
      case 'preparing':
        return (Colors.purple, 'Đang chuẩn bị');
      case 'inProgress':
        return (Colors.blueAccent, 'Đang diễn ra');
      case 'completed':
        return (Colors.green, 'Hoàn thành');
      case 'cancelled':
        return (Colors.red, 'Đã hủy');
      case 'refunded':
        return (Colors.teal, 'Đã hoàn tiền');
      default:
        return (Colors.grey, 'Không rõ');
    }
  }
}
