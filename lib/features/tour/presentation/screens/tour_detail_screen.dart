import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../domain/entities/tour_entity.dart';
import '../../../booking/presentation/screens/booking_form_screen.dart';

class TourDetailScreen extends StatelessWidget {
  final TourEntity tour;
  const TourDetailScreen({super.key, required this.tour});

  @override
  Widget build(BuildContext context) {
    final fmt = NumberFormat.currency(
      locale: 'vi_VN',
      symbol: '₫',
      decimalDigits: 0,
    );

    return Scaffold(
      backgroundColor: Colors.white,
      body: CustomScrollView(
        slivers: [
          // ── Hero image + back button ──────────────────────────────────────
          SliverAppBar(
            expandedHeight: 280,
            pinned: true,
            backgroundColor: Colors.white,
            foregroundColor: Colors.black,
            flexibleSpace: FlexibleSpaceBar(
              background: tour.images.isNotEmpty
                  ? Image.network(
                      tour.images.first,
                      fit: BoxFit.cover,
                      errorBuilder: (_, __, ___) => _placeholder(),
                    )
                  : _placeholder(),
            ),
          ),

          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Title + rating
                  Text(
                    tour.title,
                    style: const TextStyle(
                      fontSize: 24,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      const Icon(Icons.star, size: 16, color: Colors.amber),
                      const SizedBox(width: 4),
                      Text(
                        '${tour.rating.toStringAsFixed(1)} '
                        '(${tour.totalReviews} đánh giá)',
                        style: const TextStyle(fontSize: 14),
                      ),
                      const SizedBox(width: 16),
                      const Icon(
                        Icons.location_on,
                        size: 16,
                        color: Colors.grey,
                      ),
                      const SizedBox(width: 4),
                      Expanded(
                        child: Text(
                          tour.location,
                          style: const TextStyle(
                            fontSize: 14,
                            color: Colors.grey,
                          ),
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),

                  const SizedBox(height: 20),
                  const Divider(),
                  const SizedBox(height: 16),

                  // Info chips
                  Wrap(
                    spacing: 12,
                    runSpacing: 8,
                    children: [
                      _InfoChip(Icons.access_time, '${tour.durationHours} giờ'),
                      _InfoChip(
                        Icons.people,
                        'Tối đa ${tour.maxParticipants} người',
                      ),
                      _InfoChip(Icons.confirmation_number, 'Xác nhận ngay'),
                    ],
                  ),

                  const SizedBox(height: 24),

                  // Description
                  if (tour.description != null) ...[
                    const Text(
                      'Mô tả',
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      tour.description!,
                      style: const TextStyle(
                        fontSize: 15,
                        height: 1.6,
                        color: Colors.black87,
                      ),
                    ),
                    const SizedBox(height: 24),
                  ],

                  // What's included
                  const Text(
                    'Bao gồm',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 8),
                  ...[
                    'Hướng dẫn viên chuyên nghiệp',
                    'Vé tham quan các điểm trong tour',
                    'Bảo hiểm du lịch',
                  ].map(
                    (e) => Padding(
                      padding: const EdgeInsets.only(bottom: 6),
                      child: Row(
                        children: [
                          const Icon(
                            Icons.check_circle,
                            size: 18,
                            color: Colors.green,
                          ),
                          const SizedBox(width: 8),
                          Text(e, style: const TextStyle(fontSize: 14)),
                        ],
                      ),
                    ),
                  ),

                  const SizedBox(height: 100), // space for bottom bar
                ],
              ),
            ),
          ),
        ],
      ),

      // ── Bottom booking bar ────────────────────────────────────────────────
      bottomNavigationBar: Container(
        padding: const EdgeInsets.fromLTRB(20, 12, 20, 24),
        decoration: BoxDecoration(
          color: Colors.white,
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.08),
              blurRadius: 12,
              offset: const Offset(0, -4),
            ),
          ],
        ),
        child: Row(
          children: [
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  fmt.format(tour.price),
                  style: const TextStyle(
                    fontSize: 22,
                    fontWeight: FontWeight.bold,
                    color: Color(0xFFE91E8C),
                  ),
                ),
                const Text(
                  '/ người',
                  style: TextStyle(fontSize: 12, color: Colors.grey),
                ),
              ],
            ),
            const SizedBox(width: 16),
            Expanded(
              child: ElevatedButton(
                onPressed: () => Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => BookingFormScreen(tour: tour),
                  ),
                ),
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
                  'Đặt tour ngay',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _placeholder() => Container(
    color: Colors.grey.shade200,
    child: const Icon(Icons.image, size: 64, color: Colors.grey),
  );
}

class _InfoChip extends StatelessWidget {
  final IconData icon;
  final String label;
  const _InfoChip(this.icon, this.label);

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: Colors.grey.shade100,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 16, color: Colors.grey.shade700),
          const SizedBox(width: 6),
          Text(
            label,
            style: TextStyle(fontSize: 13, color: Colors.grey.shade700),
          ),
        ],
      ),
    );
  }
}
