/// BookingEntity — cập nhật theo schema mới
/// bookings.tour_availability_id → tour_availability → guide_tours
class BookingEntity {
  final String id;
  final String tourAvailabilityId; // FK → tour_availability.id
  final String travelerId;
  final DateTime tourDate; // Lấy từ tour_availability.date
  final int guests;
  final double totalPrice;
  final String status; // pending | confirmed | completed | cancelled
  final DateTime createdAt;

  // Populated from joins
  final String? guideTourId; // tour_availability.guide_tour_id
  final String? tourTitle; // tour_templates.title
  final String? tourLocation; // tour_templates.location
  final String? guideId; // guide_tours.guide_id
  final int? remainingSlots; // tour_availability.remaining_slots
  final String? note;

  const BookingEntity({
    required this.id,
    required this.tourAvailabilityId,
    required this.travelerId,
    required this.tourDate,
    required this.guests,
    required this.totalPrice,
    required this.status,
    required this.createdAt,
    this.guideTourId,
    this.tourTitle,
    this.tourLocation,
    this.guideId,
    this.remainingSlots,
    this.note,
  });

  bool get isPending => status == 'pending';
  bool get isConfirmed => status == 'confirmed';
  bool get isCompleted => status == 'completed';
  bool get isCancelled => status == 'cancelled';
}
