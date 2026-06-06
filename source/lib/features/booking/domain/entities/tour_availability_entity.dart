import 'package:equatable/equatable.dart';

/// TourAvailabilityEntity — đại diện cho bảng tour_availability
/// Quản lý ngày và số chỗ còn lại của mỗi guide_tour
class TourAvailabilityEntity extends Equatable {
  final String id;
  final String guideTourId;
  final DateTime date;
  final int remainingSlots;

  const TourAvailabilityEntity({
    required this.id,
    required this.guideTourId,
    required this.date,
    required this.remainingSlots,
  });

  @override
  List<Object?> get props => [id, guideTourId, date, remainingSlots];

  bool get isAvailable => remainingSlots > 0;
  bool get isFull => remainingSlots == 0;

  String get availabilityStatus {
    if (remainingSlots == 0) return 'Hết chỗ';
    if (remainingSlots == 1) return 'Còn 1 chỗ';
    return 'Còn $remainingSlots chỗ';
  }
}
