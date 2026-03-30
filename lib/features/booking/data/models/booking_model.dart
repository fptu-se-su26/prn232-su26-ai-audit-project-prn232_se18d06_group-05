import '../../domain/entities/booking_entity.dart';

class BookingModel extends BookingEntity {
  const BookingModel({
    required super.id,
    required super.tourId,
    required super.tourTitle,
    required super.tourLocation,
    required super.travelerId,
    required super.tourDate,
    required super.guests,
    required super.unitPrice,
    required super.totalPrice,
    super.note,
    required super.status,
    required super.createdAt,
  });

  factory BookingModel.fromJson(Map<String, dynamic> json) {
    return BookingModel(
      id: json['id'] as String,
      tourId: json['tourId'] as String,
      tourTitle: json['tourTitle'] as String? ?? '',
      tourLocation: json['tourLocation'] as String? ?? '',
      travelerId: json['travelerId'] as String,
      tourDate: DateTime.parse(json['tourDate'] as String),
      guests: json['guests'] as int,
      unitPrice: (json['unitPrice'] as num).toDouble(),
      totalPrice: (json['totalPrice'] as num).toDouble(),
      note: json['note'] as String?,
      status: json['status'] as String? ?? 'pending',
      createdAt: DateTime.parse(json['createdAt'] as String),
    );
  }
}
