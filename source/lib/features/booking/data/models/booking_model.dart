import '../../domain/entities/booking_entity.dart';

class BookingModel extends BookingEntity {
  const BookingModel({
    required super.id,
    required super.guideTourId,
    required super.travelerId,
    required super.tourDate,
    required super.guests,
    required super.totalPrice,
    required super.status,
    required super.createdAt,
    super.tourTitle,
    super.tourLocation,
    super.guideId,
  });

  factory BookingModel.fromJson(Map<String, dynamic> json) {
    return BookingModel(
      id: json['id'] as String,
      guideTourId: json['guide_tour_id'] as String,
      travelerId: json['traveler_id'] as String,
      tourDate: DateTime.parse(json['tour_date'] as String),
      guests: json['guests'] as int,
      totalPrice: (json['total_price'] as num).toDouble(),
      status: json['status'] as String? ?? 'pending',
      createdAt: DateTime.parse(json['created_at'] as String),
      // Populated from joins
      tourTitle: json['tour_title'] as String?,
      tourLocation: json['tour_location'] as String?,
      guideId: json['guide_id'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'guide_tour_id': guideTourId,
      'traveler_id': travelerId,
      'tour_date': tourDate.toIso8601String().split('T')[0], // Date only
      'guests': guests,
      'total_price': totalPrice,
      'status': status,
      'created_at': createdAt.toIso8601String(),
    };
  }

  factory BookingModel.fromEntity(BookingEntity entity) {
    return BookingModel(
      id: entity.id,
      guideTourId: entity.guideTourId,
      travelerId: entity.travelerId,
      tourDate: entity.tourDate,
      guests: entity.guests,
      totalPrice: entity.totalPrice,
      status: entity.status,
      createdAt: entity.createdAt,
      tourTitle: entity.tourTitle,
      tourLocation: entity.tourLocation,
      guideId: entity.guideId,
    );
  }
}
