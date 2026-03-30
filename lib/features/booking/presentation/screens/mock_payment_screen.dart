import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../domain/entities/booking_entity.dart';
import 'booking_confirmation_screen.dart';

class MockPaymentScreen extends ConsumerStatefulWidget {
  final BookingEntity booking;
  const MockPaymentScreen({super.key, required this.booking});

  @override
  ConsumerState<MockPaymentScreen> createState() => _MockPaymentScreenState();
}

class _MockPaymentScreenState extends ConsumerState<MockPaymentScreen> {
  bool _processing = false;
  String _selectedMethod = 'momo';

  final _fmt = NumberFormat.currency(
    locale: 'vi_VN',
    symbol: '₫',
    decimalDigits: 0,
  );

  Future<void> _pay() async {
    setState(() => _processing = true);

    // Giả lập xử lý thanh toán 2 giây
    await Future.delayed(const Duration(seconds: 2));

    if (!mounted) return;
    setState(() => _processing = false);

    // Sau khi "thanh toán" thành công → navigate đến confirmation
    Navigator.pushReplacement(
      context,
      MaterialPageRoute(
        builder: (_) => BookingConfirmationScreen(booking: widget.booking),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        title: const Text(
          'Thanh toán',
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
            // Order summary
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: const Color(0xFFFFF0F7),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Column(
                children: [
                  const Row(
                    children: [
                      Icon(Icons.receipt_long, color: Color(0xFFE91E8C)),
                      SizedBox(width: 8),
                      Text(
                        'Tóm tắt đơn hàng',
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 16,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),
                  _SummaryRow('Tour', widget.booking.tourTitle),
                  const SizedBox(height: 8),
                  _SummaryRow('Địa điểm', widget.booking.tourLocation),
                  const SizedBox(height: 8),
                  _SummaryRow(
                    'Ngày',
                    '${widget.booking.tourDate.day}/${widget.booking.tourDate.month}/${widget.booking.tourDate.year}',
                  ),
                  const SizedBox(height: 8),
                  _SummaryRow('Số khách', '${widget.booking.guests} người'),
                  const Divider(height: 20),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        'Tổng cộng',
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 16,
                        ),
                      ),
                      Text(
                        _fmt.format(widget.booking.totalPrice),
                        style: const TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 18,
                          color: Color(0xFFE91E8C),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),

            const SizedBox(height: 28),

            // Payment methods
            const Text(
              'Phương thức thanh toán',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 12),

            ...[
              (
                'momo',
                'MoMo',
                Icons.account_balance_wallet,
                const Color(0xFFAE2070),
              ),
              ('vnpay', 'VNPay', Icons.payment, const Color(0xFF0066CC)),
              (
                'zalopay',
                'ZaloPay',
                Icons.mobile_friendly,
                const Color(0xFF0068FF),
              ),
              ('card', 'Thẻ tín dụng', Icons.credit_card, Colors.grey),
            ].map(
              (m) => _PaymentMethodTile(
                id: m.$1,
                label: m.$2,
                icon: m.$3,
                color: m.$4,
                selected: _selectedMethod == m.$1,
                onTap: () => setState(() => _selectedMethod = m.$1),
              ),
            ),

            const SizedBox(height: 12),

            // Test mode notice
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.orange.shade50,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: Colors.orange.shade200),
              ),
              child: Row(
                children: [
                  const Icon(
                    Icons.info_outline,
                    color: Colors.orange,
                    size: 18,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      'Chế độ test — không có giao dịch thực tế',
                      style: TextStyle(
                        fontSize: 12,
                        color: Colors.orange.shade800,
                      ),
                    ),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 32),

            // Pay button
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: _processing ? null : _pay,
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFFE91E8C),
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 18),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(14),
                  ),
                  elevation: 0,
                ),
                child: _processing
                    ? const Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          SizedBox(
                            width: 20,
                            height: 20,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          ),
                          SizedBox(width: 12),
                          Text('Đang xử lý...', style: TextStyle(fontSize: 16)),
                        ],
                      )
                    : Text(
                        'Thanh toán ${_fmt.format(widget.booking.totalPrice)}',
                        style: const TextStyle(
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
}

class _SummaryRow extends StatelessWidget {
  final String label, value;
  const _SummaryRow(this.label, this.value);

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: TextStyle(fontSize: 14, color: Colors.grey.shade600),
        ),
        Flexible(
          child: Text(
            value,
            style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w500),
            textAlign: TextAlign.right,
          ),
        ),
      ],
    );
  }
}

class _PaymentMethodTile extends StatelessWidget {
  final String id, label;
  final IconData icon;
  final Color color;
  final bool selected;
  final VoidCallback onTap;

  const _PaymentMethodTile({
    required this.id,
    required this.label,
    required this.icon,
    required this.color,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        margin: const EdgeInsets.only(bottom: 10),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        decoration: BoxDecoration(
          border: Border.all(
            color: selected ? const Color(0xFFE91E8C) : Colors.grey.shade300,
            width: selected ? 2 : 1,
          ),
          borderRadius: BorderRadius.circular(12),
          color: selected ? const Color(0xFFFFF0F7) : Colors.white,
        ),
        child: Row(
          children: [
            Icon(icon, color: color, size: 24),
            const SizedBox(width: 12),
            Text(
              label,
              style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w500),
            ),
            const Spacer(),
            if (selected)
              const Icon(
                Icons.check_circle,
                color: Color(0xFFE91E8C),
                size: 20,
              ),
          ],
        ),
      ),
    );
  }
}
