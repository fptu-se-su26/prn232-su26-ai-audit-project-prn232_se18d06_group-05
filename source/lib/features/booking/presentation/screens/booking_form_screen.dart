import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../../features/tour/domain/entities/tour_entity.dart';
import '../../domain/entities/tour_availability_entity.dart';
import '../providers/booking_provider.dart';
import 'mock_payment_screen.dart';

class BookingFormScreen extends ConsumerStatefulWidget {
  final TourEntity tour;

  const BookingFormScreen({super.key, required this.tour});

  @override
  ConsumerState<BookingFormScreen> createState() => _BookingFormScreenState();
}

class _BookingFormScreenState extends ConsumerState<BookingFormScreen> {
  // Schema mới: user chọn availability slot (ngày + slot cụ thể)
  TourAvailabilityEntity? _selectedSlot;
  int _guests = 1;
  final _noteController = TextEditingController();
  final _fmt = NumberFormat.currency(
    locale: 'vi_VN',
    symbol: '₫',
    decimalDigits: 0,
  );

  @override
  void initState() {
    super.initState();
    // Load availability slots cho tour này
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(availabilityProvider.notifier).load(widget.tour.id);
    });
  }

  @override
  void dispose() {
    _noteController.dispose();
    super.dispose();
  }

  double get _total => widget.tour.price * _guests;

  Future<void> _submit() async {
    if (_selectedSlot == null) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Vui lòng chọn ngày tour')));
      return;
    }

    if (_guests > _selectedSlot!.remainingSlots) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Chỉ còn ${_selectedSlot!.remainingSlots} chỗ cho ngày này',
          ),
        ),
      );
      return;
    }

    final ok = await ref
        .read(createBookingProvider.notifier)
        .create(
          tourAvailabilityId: _selectedSlot!.id,
          guests: _guests,
          note: _noteController.text.trim().isEmpty
              ? null
              : _noteController.text.trim(),
        );

    if (!mounted) return;

    if (ok) {
      final booking = ref.read(createBookingProvider).result!;
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(builder: (_) => MockPaymentScreen(booking: booking)),
      );
    } else {
      final err = ref.read(createBookingProvider).error ?? 'Đặt tour thất bại';
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(err), backgroundColor: Colors.red));
    }
  }

  @override
  Widget build(BuildContext context) {
    final bookingState = ref.watch(createBookingProvider);
    final availState = ref.watch(availabilityProvider);

    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        title: const Text(
          'Đặt tour',
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
        backgroundColor: Colors.white,
        foregroundColor: Colors.black,
        elevation: 0,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Tour summary ────────────────────────────────────────────────
            _buildTourSummary(),
            const SizedBox(height: 28),

            // ── Availability slots ──────────────────────────────────────────
            const Text(
              'Chọn ngày tour',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 10),

            if (availState.isLoading)
              const Center(child: CircularProgressIndicator())
            else if (availState.error != null)
              _buildErrorBanner(availState.error!)
            else if (availState.slots.isEmpty)
              _buildNoSlotsMessage()
            else
              _buildSlotPicker(availState.slots),

            const SizedBox(height: 24),

            // ── Guests counter ──────────────────────────────────────────────
            const Text(
              'Số khách',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 10),
            _buildGuestCounter(),
            const SizedBox(height: 24),

            // ── Note ────────────────────────────────────────────────────────
            const Text(
              'Ghi chú (tuỳ chọn)',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 10),
            TextField(
              controller: _noteController,
              maxLines: 3,
              decoration: InputDecoration(
                hintText: 'Yêu cầu đặc biệt, dị ứng thức ăn...',
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                  borderSide: BorderSide(color: Colors.grey.shade300),
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                  borderSide: BorderSide(color: Colors.grey.shade300),
                ),
              ),
            ),
            const SizedBox(height: 32),

            // ── Price summary ───────────────────────────────────────────────
            _buildPriceSummary(),
            const SizedBox(height: 32),

            // ── Submit ──────────────────────────────────────────────────────
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed:
                    (bookingState.isLoading ||
                        _selectedSlot == null ||
                        availState.isLoading)
                    ? null
                    : _submit,
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFFE91E8C),
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 16),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  elevation: 0,
                ),
                child: bookingState.isLoading
                    ? const SizedBox(
                        width: 22,
                        height: 22,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : const Text(
                        'Xác nhận đặt tour',
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
              ),
            ),
            const SizedBox(height: 24),
          ],
        ),
      ),
    );
  }

  // ── Widgets ─────────────────────────────────────────────────────────────────

  Widget _buildTourSummary() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.grey.shade50,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.grey.shade200),
      ),
      child: Row(
        children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(8),
            child: widget.tour.images.isNotEmpty
                ? Image.network(
                    widget.tour.images.first,
                    width: 72,
                    height: 72,
                    fit: BoxFit.cover,
                    errorBuilder: (_, __, ___) => Container(
                      width: 72,
                      height: 72,
                      color: Colors.grey.shade200,
                    ),
                  )
                : Container(
                    width: 72,
                    height: 72,
                    color: Colors.grey.shade200,
                    child: const Icon(Icons.image),
                  ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  widget.tour.title,
                  style: const TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 15,
                  ),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
                const SizedBox(height: 4),
                Text(
                  widget.tour.location,
                  style: TextStyle(fontSize: 13, color: Colors.grey.shade600),
                ),
                const SizedBox(height: 4),
                Text(
                  '${_fmt.format(widget.tour.price)} / người',
                  style: const TextStyle(
                    fontSize: 14,
                    color: Color(0xFFE91E8C),
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSlotPicker(List<TourAvailabilityEntity> slots) {
    return SizedBox(
      height: 90,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        itemCount: slots.length,
        separatorBuilder: (_, __) => const SizedBox(width: 8),
        itemBuilder: (context, index) {
          final slot = slots[index];
          final isSelected = _selectedSlot?.id == slot.id;
          final isAvailable = slot.isAvailable;

          return GestureDetector(
            onTap: isAvailable
                ? () => setState(() {
                    _selectedSlot = slot;
                    // Reset guests nếu vượt quá remaining slots
                    if (_guests > slot.remainingSlots) {
                      _guests = slot.remainingSlots;
                    }
                  })
                : null,
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 200),
              width: 100,
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 10),
              decoration: BoxDecoration(
                color: isSelected
                    ? const Color(0xFFE91E8C)
                    : isAvailable
                    ? Colors.white
                    : Colors.grey.shade100,
                border: Border.all(
                  color: isSelected
                      ? const Color(0xFFE91E8C)
                      : isAvailable
                      ? Colors.grey.shade300
                      : Colors.grey.shade200,
                ),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(
                    DateFormat('dd/MM').format(slot.date),
                    style: TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 15,
                      color: isSelected
                          ? Colors.white
                          : isAvailable
                          ? Colors.black
                          : Colors.grey,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    DateFormat('EEE', 'vi').format(slot.date),
                    style: TextStyle(
                      fontSize: 11,
                      color: isSelected ? Colors.white70 : Colors.grey.shade600,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    isAvailable ? '${slot.remainingSlots} chỗ' : 'Hết chỗ',
                    style: TextStyle(
                      fontSize: 11,
                      color: isSelected
                          ? Colors.white70
                          : isAvailable
                          ? const Color(0xFF2E7D32)
                          : Colors.red.shade400,
                    ),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildGuestCounter() {
    final maxGuests =
        _selectedSlot?.remainingSlots ?? widget.tour.maxParticipants;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      decoration: BoxDecoration(
        border: Border.all(color: Colors.grey.shade300),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          const Icon(Icons.people_outline, color: Colors.grey),
          const SizedBox(width: 12),
          const Text('Số người', style: TextStyle(fontSize: 15)),
          const Spacer(),
          IconButton(
            onPressed: _guests > 1 ? () => setState(() => _guests--) : null,
            icon: const Icon(Icons.remove_circle_outline),
            color: const Color(0xFFE91E8C),
          ),
          Text(
            '$_guests',
            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
          ),
          IconButton(
            onPressed: _guests < maxGuests
                ? () => setState(() => _guests++)
                : null,
            icon: const Icon(Icons.add_circle_outline),
            color: const Color(0xFFE91E8C),
          ),
        ],
      ),
    );
  }

  Widget _buildPriceSummary() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFFFFF0F7),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        children: [
          _PriceLine(
            '${_fmt.format(widget.tour.price)} × $_guests người',
            _fmt.format(_total),
          ),
          const Divider(height: 20),
          _PriceLine(
            'Tổng cộng',
            _fmt.format(_total),
            bold: true,
            color: const Color(0xFFE91E8C),
          ),
        ],
      ),
    );
  }

  Widget _buildErrorBanner(String error) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.red.shade50,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: Colors.red.shade200),
      ),
      child: Row(
        children: [
          Icon(Icons.error_outline, color: Colors.red.shade700),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              error,
              style: TextStyle(color: Colors.red.shade700, fontSize: 13),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildNoSlotsMessage() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.grey.shade50,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.grey.shade200),
      ),
      child: const Row(
        children: [
          Icon(Icons.event_busy, color: Colors.grey),
          SizedBox(width: 12),
          Text(
            'Tour này hiện chưa có lịch trống',
            style: TextStyle(color: Colors.grey),
          ),
        ],
      ),
    );
  }
}

// ── Price row widget ──────────────────────────────────────────────────────────

class _PriceLine extends StatelessWidget {
  final String label;
  final String value;
  final bool bold;
  final Color? color;

  const _PriceLine(this.label, this.value, {this.bold = false, this.color});

  @override
  Widget build(BuildContext context) {
    final style = TextStyle(
      fontSize: bold ? 16 : 14,
      fontWeight: bold ? FontWeight.bold : FontWeight.normal,
      color: color,
    );
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(label, style: style),
        Text(value, style: style),
      ],
    );
  }
}
