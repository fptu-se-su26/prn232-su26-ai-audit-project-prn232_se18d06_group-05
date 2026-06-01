class BookingEntity {
  final String id;
  final String guideTourId;
  final String travelerId;
  final DateTime tourDate;
  final int guests;
  final double totalPrice;
  final String status; // pending | confirmed | completed | cancelled
  final DateTime createdAt;

  // Populated from joins
  final String? tourTitle;
  final String? tourLocation;
  final String? guideId;

  const BookingEntity({
    required this.id,
    required this.guideTourId,
    required this.travelerId,
    required this.tourDate,
    required this.guests,
    required this.totalPrice,
    required this.status,
    required this.createdAt,
    this.tourTitle,
    this.tourLocation,
    this.guideId,
  });

  bool get isPending => status == 'pending';
  bool get isConfirmed => status == 'confirmed';
  bool get isCompleted => status == 'completed';
  bool get isCancelled => status == 'cancelled';
}
