import 'package:equatable/equatable.dart';

/// Tour entity representing a tour listing
class TourEntity extends Equatable {
  final String id;
  final String guideId;
  final String title;
  final String? description;
  final String location;
  final double price;
  final int durationHours;
  final int maxParticipants;
  final List<String> images;
  final double rating;
  final int totalReviews;
  final String status;
  final DateTime createdAt;
  final DateTime updatedAt;

  const TourEntity({
    required this.id,
    required this.guideId,
    required this.title,
    this.description,
    required this.location,
    required this.price,
    required this.durationHours,
    required this.maxParticipants,
    this.images = const [],
    this.rating = 0.0,
    this.totalReviews = 0,
    this.status = 'active',
    required this.createdAt,
    required this.updatedAt,
  });

  @override
  List<Object?> get props => [
    id,
    guideId,
    title,
    description,
    location,
    price,
    durationHours,
    maxParticipants,
    images,
    rating,
    totalReviews,
    status,
    createdAt,
    updatedAt,
  ];
}
