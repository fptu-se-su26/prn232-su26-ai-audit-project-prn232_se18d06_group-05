import '../../domain/entities/booking_entity.dart';

class BookingModel extends BookingEntity {
  const BookingModel({
    required super.id,
    required super.tourAvailabilityId,
    required super.travelerId,
    required super.tourDate,
    required super.guests,
    required super.totalPrice,
    required super.status,
    required super.createdAt,
    super.guideTourId,
    super.tourTitle,
    super.tourLocation,
    super.guideId,
    super.remainingSlots,
    super.note,
  });

  /// Parse từ ASP.NET API (camelCase) hoặc Supabase (snake_case)
  factory BookingModel.fromJson(Map<String, dynamic> json) {
    String parseDateOnly(dynamic value) {
      if (value == null) return DateTime.now().toIso8601String();
      final s = value.toString();
      return s.contains('T') ? s : '${s}T00:00:00.000Z';
    }

    return BookingModel(
      id: (json['id'] ?? '') as String,
      // Schema mới: tourAvailabilityId
      tourAvailabilityId:
          (json['tourAvailabilityId'] ?? json['tour_availability_id'] ?? '')
              as String,
      travelerId: (json['travelerId'] ?? json['traveler_id'] ?? '') as String,
      tourDate: DateTime.parse(
        parseDateOnly(json['tourDate'] ?? json['tour_date']),
      ),
      guests: (json['guests'] as num? ?? 0).toInt(),
      totalPrice: ((json['totalPrice'] ?? json['total_price']) as num? ?? 0)
          .toDouble(),
      status: (json['status'] as String?) ?? 'pending',
      createdAt: DateTime.parse(
        (json['createdAt'] ??
                json['created_at'] ??
                DateTime.now().toIso8601String())
            .toString(),
      ),
      // Optional join fields
      guideTourId:
          json['guideTourId'] as String? ?? json['guide_tour_id'] as String?,
      tourTitle: json['tourTitle'] as String? ?? json['tour_title'] as String?,
      tourLocation:
          json['tourLocation'] as String? ?? json['tour_location'] as String?,
      guideId: json['guideId'] as String? ?? json['guide_id'] as String?,
      remainingSlots:
          json['remainingSlots'] as int? ?? json['remaining_slots'] as int?,
      note: json['note'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'tour_availability_id': tourAvailabilityId,
      'traveler_id': travelerId,
      'tour_date': tourDate.toIso8601String().split('T')[0],
      'guests': guests,
      'total_price': totalPrice,
      'status': status,
      'created_at': createdAt.toIso8601String(),
      if (note != null) 'note': note,
    };
  }

  factory BookingModel.fromEntity(BookingEntity entity) {
    return BookingModel(
      id: entity.id,
      tourAvailabilityId: entity.tourAvailabilityId,
      travelerId: entity.travelerId,
      tourDate: entity.tourDate,
      guests: entity.guests,
      totalPrice: entity.totalPrice,
      status: entity.status,
      createdAt: entity.createdAt,
      guideTourId: entity.guideTourId,
      tourTitle: entity.tourTitle,
      tourLocation: entity.tourLocation,
      guideId: entity.guideId,
      remainingSlots: entity.remainingSlots,
      note: entity.note,
    );
  }
}
