import 'package:equatable/equatable.dart';

/// Payment entity
class PaymentEntity extends Equatable {
  final String id;
  final String bookingId;
  final double amount;
  final String paymentMethod;
  final String status; // pending | completed | failed
  final DateTime createdAt;

  const PaymentEntity({
    required this.id,
    required this.bookingId,
    required this.amount,
    required this.paymentMethod,
    required this.status,
    required this.createdAt,
  });

  @override
  List<Object?> get props => [
    id,
    bookingId,
    amount,
    paymentMethod,
    status,
    createdAt,
  ];

  bool get isPending => status == 'pending';
  bool get isCompleted => status == 'completed';
  bool get isFailed => status == 'failed';

  String get statusDisplayName {
    switch (status) {
      case 'pending':
        return 'Chờ thanh toán';
      case 'completed':
        return 'Đã thanh toán';
      case 'failed':
        return 'Thanh toán thất bại';
      default:
        return 'Không xác định';
    }
  }

  String get paymentMethodDisplayName {
    switch (paymentMethod) {
      case 'credit_card':
        return 'Thẻ tín dụng';
      case 'bank_transfer':
        return 'Chuyển khoản';
      case 'cash':
        return 'Tiền mặt';
      case 'e_wallet':
        return 'Ví điện tử';
      default:
        return paymentMethod;
    }
  }
}
