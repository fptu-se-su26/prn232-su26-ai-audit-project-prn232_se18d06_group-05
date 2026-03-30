class BookingEntity {
  final String id;
  final String tourId;
  final String tourTitle;
  final String tourLocation;
  final String travelerId;
  final DateTime tourDate;
  final int guests;
  final double unitPrice;
  final double totalPrice;
  final String? note;
  final String status; // pending | confirmed | completed | cancelled
  final DateTime createdAt;

  const BookingEntity({
    required this.id,
    required this.tourId,
    required this.tourTitle,
    required this.tourLocation,
    required this.travelerId,
    required this.tourDate,
    required this.guests,
    required this.unitPrice,
    required this.totalPrice,
    this.note,
    required this.status,
    required this.createdAt,
  });

  bool get isPending => status == 'pending';
  bool get isConfirmed => status == 'confirmed';
  bool get isCompleted => status == 'completed';
  bool get isCancelled => status == 'cancelled';
}
