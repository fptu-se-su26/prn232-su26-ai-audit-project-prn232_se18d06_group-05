import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../domain/entities/tour_entity.dart';
import '../../../booking/presentation/screens/booking_form_screen.dart';

class TourDetailScreen extends ConsumerWidget {
  final TourEntity tour;
  const TourDetailScreen({super.key, required this.tour});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final fmt = NumberFormat.currency(
      locale: 'vi_VN',
      symbol: '₫',
      decimalDigits: 0,
    );

    return Scaffold(
      backgroundColor: Colors.white,
      body: CustomScrollView(
        slivers: [
          // ── Hero image ────────────────────────────────────────────────────────
          SliverAppBar(
            expandedHeight: 300,
            pinned: true,
            backgroundColor: Colors.white,
            foregroundColor: Colors.black,
            flexibleSpace: FlexibleSpaceBar(
              background: tour.images.isNotEmpty
                  ? Image.network(
                      tour.images.first,
                      fit: BoxFit.cover,
                      errorBuilder: (_, _, _) => _placeholder(),
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
                  // ── Title + rating ──────────────────────────────────────────────
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
                        '${tour.rating.toStringAsFixed(1)} (${tour.totalReviews} đánh giá)',
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

                  const SizedBox(height: 16),

                  // ── Guide info ──────────────────────────────────────────────────
                  _GuideCard(guideId: tour.guideId),

                  const SizedBox(height: 20),
                  const Divider(),
                  const SizedBox(height: 16),

                  // ── Info chips ──────────────────────────────────────────────────
                  Wrap(
                    spacing: 10,
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

                  // ── Description ─────────────────────────────────────────────────
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

                  // ── Những gì dành cho bạn ───────────────────────────────────────
                  const Text(
                    'Những gì dành cho bạn',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 12),
                  ..._includes.map(
                    (item) => _IncludeItem(
                      icon: item.$1,
                      label: item.$2,
                      desc: item.$3,
                    ),
                  ),

                  const SizedBox(height: 24),

                  // ── Vị trí ──────────────────────────────────────────────────────
                  const Text(
                    'Vị trí',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 12),
                  _LocationSection(location: tour.location),

                  const SizedBox(height: 24),

                  // ── Tình trạng còn tour ─────────────────────────────────────────
                  const Text(
                    'Tình trạng còn tour',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 12),
                  _AvailabilitySection(tourId: tour.id),

                  const SizedBox(height: 24),

                  // ── Reviews ─────────────────────────────────────────────────────
                  if (tour.totalReviews > 0) ...[
                    Row(
                      children: [
                        const Text(
                          'Đánh giá',
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        const SizedBox(width: 8),
                        const Icon(Icons.star, size: 16, color: Colors.amber),
                        const SizedBox(width: 4),
                        Text(
                          tour.rating.toStringAsFixed(1),
                          style: const TextStyle(fontWeight: FontWeight.bold),
                        ),
                        Text(
                          ' (${tour.totalReviews})',
                          style: TextStyle(color: Colors.grey.shade600),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    _ReviewsSection(tourId: tour.id),
                  ],

                  const SizedBox(height: 100),
                ],
              ),
            ),
          ),
        ],
      ),

      // ── Bottom bar ────────────────────────────────────────────────────────
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

  static const _includes = [
    (Icons.person, 'Hướng dẫn viên', 'Chuyên nghiệp, am hiểu địa phương'),
    (Icons.confirmation_number, 'Vé tham quan', 'Tất cả điểm trong lịch trình'),
    (Icons.security, 'Bảo hiểm', 'Bảo hiểm du lịch toàn hành trình'),
    (Icons.restaurant, 'Ẩm thực', 'Thưởng thức đặc sản địa phương'),
    (Icons.directions_car, 'Di chuyển', 'Phương tiện đưa đón thoải mái'),
  ];

  Widget _placeholder() => Container(
    color: Colors.grey.shade200,
    child: const Icon(Icons.image, size: 64, color: Colors.grey),
  );
}

// ── Guide Card ────────────────────────────────────────────────────────────────

class _GuideCard extends StatelessWidget {
  final String guideId;
  const _GuideCard({required this.guideId});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.grey.shade50,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: Colors.grey.shade200),
      ),
      child: Row(
        children: [
          CircleAvatar(
            radius: 24,
            backgroundColor: const Color(0xFFFFE4E6),
            child: const Icon(Icons.person, color: Color(0xFFE91E8C)),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Hướng dẫn viên',
                  style: TextStyle(
                    fontSize: 11,
                    color: Colors.grey,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 2),
                const Text(
                  'Xem hồ sơ guide',
                  style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
                ),
              ],
            ),
          ),
          const Icon(Icons.chevron_right, color: Colors.grey),
        ],
      ),
    );
  }
}

// ── Include Item ──────────────────────────────────────────────────────────────

class _IncludeItem extends StatelessWidget {
  final IconData icon;
  final String label, desc;
  const _IncludeItem({
    required this.icon,
    required this.label,
    required this.desc,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        children: [
          Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(
              color: const Color(0xFFFFE4E6),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Icon(icon, color: const Color(0xFFE91E8C), size: 20),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(
                    fontWeight: FontWeight.w600,
                    fontSize: 14,
                  ),
                ),
                Text(
                  desc,
                  style: TextStyle(fontSize: 12, color: Colors.grey.shade600),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── Location Section ──────────────────────────────────────────────────────────

class _LocationSection extends StatelessWidget {
  final String location;
  const _LocationSection({required this.location});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Map placeholder — tap để mở Google Maps
        InkWell(
          onTap: () async {
            final uri = Uri.parse(
              'https://www.google.com/maps/search/?api=1&query=${Uri.encodeComponent(location)}',
            );
            if (await canLaunchUrl(uri)) await launchUrl(uri);
          },
          borderRadius: BorderRadius.circular(14),
          child: Container(
            height: 160,
            decoration: BoxDecoration(
              color: Colors.grey.shade200,
              borderRadius: BorderRadius.circular(14),
            ),
            child: Stack(
              children: [
                Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.map, size: 48, color: Colors.grey),
                      const SizedBox(height: 8),
                      Text(
                        location,
                        style: const TextStyle(
                          fontWeight: FontWeight.w600,
                          fontSize: 14,
                        ),
                      ),
                      const SizedBox(height: 4),
                      const Text(
                        'Nhấn để xem trên Google Maps',
                        style: TextStyle(fontSize: 12, color: Colors.grey),
                      ),
                    ],
                  ),
                ),
                Positioned(
                  top: 10,
                  right: 10,
                  child: Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 10,
                      vertical: 6,
                    ),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(20),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: 0.1),
                          blurRadius: 4,
                        ),
                      ],
                    ),
                    child: const Row(
                      children: [
                        Icon(
                          Icons.open_in_new,
                          size: 14,
                          color: Color(0xFFE91E8C),
                        ),
                        SizedBox(width: 4),
                        Text(
                          'Mở Maps',
                          style: TextStyle(
                            fontSize: 12,
                            color: Color(0xFFE91E8C),
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 8),
        Row(
          children: [
            const Icon(Icons.location_on, size: 16, color: Color(0xFFE91E8C)),
            const SizedBox(width: 6),
            Expanded(
              child: Text(
                location,
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ),
          ],
        ),
      ],
    );
  }
}

// ── Availability Section ──────────────────────────────────────────────────────

class _AvailabilitySection extends StatefulWidget {
  final String tourId;
  const _AvailabilitySection({required this.tourId});

  @override
  State<_AvailabilitySection> createState() => _AvailabilitySectionState();
}

class _AvailabilitySectionState extends State<_AvailabilitySection> {
  DateTime? _selected;

  Future<void> _pick() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: now.add(const Duration(days: 1)),
      firstDate: now.add(const Duration(days: 1)),
      lastDate: now.add(const Duration(days: 365)),
      selectableDayPredicate: (day) => day.isAfter(now),
    );
    if (picked != null) setState(() => _selected = picked);
  }

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: _pick,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        decoration: BoxDecoration(
          border: Border.all(
            color: _selected != null
                ? const Color(0xFFE91E8C)
                : Colors.grey.shade300,
          ),
          borderRadius: BorderRadius.circular(12),
        ),
        child: Row(
          children: [
            Icon(
              Icons.calendar_month,
              color: _selected != null ? const Color(0xFFE91E8C) : Colors.grey,
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                _selected != null
                    ? DateFormat('EEEE, dd/MM/yyyy', 'vi').format(_selected!)
                    : 'Chọn ngày để kiểm tra',
                style: TextStyle(
                  fontSize: 15,
                  color: _selected != null ? Colors.black : Colors.grey,
                ),
              ),
            ),
            if (_selected != null)
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 4,
                ),
                decoration: BoxDecoration(
                  color: Colors.green.shade50,
                  borderRadius: BorderRadius.circular(20),
                ),
                child: const Text(
                  'Còn chỗ',
                  style: TextStyle(
                    fontSize: 12,
                    color: Colors.green,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

// ── Reviews Section ───────────────────────────────────────────────────────────

class _ReviewsSection extends StatelessWidget {
  final String tourId;
  const _ReviewsSection({required this.tourId});

  @override
  Widget build(BuildContext context) {
    // Placeholder — sẽ load từ API sau
    return Column(
      children: [
        _ReviewCard(
          name: 'Nguyễn Văn A',
          rating: 5,
          comment: 'Tour tuyệt vời! Hướng dẫn viên rất nhiệt tình và am hiểu.',
          date: '15/03/2025',
        ),
        const SizedBox(height: 12),
        _ReviewCard(
          name: 'Trần Thị B',
          rating: 4,
          comment: 'Trải nghiệm rất tốt, sẽ quay lại lần sau.',
          date: '10/03/2025',
        ),
      ],
    );
  }
}

class _ReviewCard extends StatelessWidget {
  final String name, comment, date;
  final int rating;
  const _ReviewCard({
    required this.name,
    required this.rating,
    required this.comment,
    required this.date,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.grey.shade50,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.grey.shade200),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 18,
                backgroundColor: Colors.grey.shade300,
                child: Text(
                  name[0],
                  style: const TextStyle(fontWeight: FontWeight.bold),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      name,
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 13,
                      ),
                    ),
                    Row(
                      children: List.generate(
                        5,
                        (i) => Icon(
                          i < rating ? Icons.star : Icons.star_border,
                          size: 12,
                          color: Colors.amber,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              Text(
                date,
                style: TextStyle(fontSize: 11, color: Colors.grey.shade500),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(comment, style: const TextStyle(fontSize: 13, height: 1.4)),
        ],
      ),
    );
  }
}

// ── Info Chip ─────────────────────────────────────────────────────────────────

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
