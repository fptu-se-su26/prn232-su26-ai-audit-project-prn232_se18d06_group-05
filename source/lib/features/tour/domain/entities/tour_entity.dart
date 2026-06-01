import 'package:equatable/equatable.dart';

/// Tour entity representing a tour listing (compatible with new database schema)
/// This is an alias for GuideTourEntity to maintain backward compatibility
class TourEntity extends Equatable {
  final String id;
  final String tourTemplateId;
  final String guideId;
  final double price;
  final int durationHours;
  final int maxParticipants;
  final String status;
  final double rating;
  final int totalReviews;

  // Template information (populated from join)
  final String title;
  final String? description;
  final String location;
  final List<String> images;
  final DateTime createdAt;
  final DateTime updatedAt;

  const TourEntity({
    required this.id,
    required this.tourTemplateId,
    required this.guideId,
    required this.price,
    required this.durationHours,
    required this.maxParticipants,
    this.status = 'active',
    this.rating = 0.0,
    this.totalReviews = 0,
    required this.title,
    this.description,
    required this.location,
    this.images = const [],
    required this.createdAt,
    required this.updatedAt,
  });

  @override
  List<Object?> get props => [
    id,
    tourTemplateId,
    guideId,
    price,
    durationHours,
    maxParticipants,
    status,
    rating,
    totalReviews,
    title,
    description,
    location,
    images,
    createdAt,
    updatedAt,
  ];

  bool get isActive => status == 'active';
  bool get isInactive => status == 'inactive';
}

/// Guide Tour entity - represents a tour offered by a guide based on a template
class GuideTourEntity extends Equatable {
  final String id;
  final String tourTemplateId;
  final String guideId;
  final double price;
  final int durationHours;
  final int maxParticipants;
  final String status;
  final double rating;
  final int totalReviews;

  // Template information (populated from join)
  final String? title;
  final String? description;
  final String? location;
  final List<String> images;

  const GuideTourEntity({
    required this.id,
    required this.tourTemplateId,
    required this.guideId,
    required this.price,
    required this.durationHours,
    required this.maxParticipants,
    this.status = 'active',
    this.rating = 0.0,
    this.totalReviews = 0,
    this.title,
    this.description,
    this.location,
    this.images = const [],
  });

  @override
  List<Object?> get props => [
    id,
    tourTemplateId,
    guideId,
    price,
    durationHours,
    maxParticipants,
    status,
    rating,
    totalReviews,
    title,
    description,
    location,
    images,
  ];

  bool get isActive => status == 'active';
  bool get isInactive => status == 'inactive';
}

/// Tour Template entity - represents a tour template/category
class TourTemplateEntity extends Equatable {
  final String id;
  final String title;
  final String? description;
  final String location;
  final List<String> images;
  final DateTime createdAt;

  const TourTemplateEntity({
    required this.id,
    required this.title,
    this.description,
    required this.location,
    this.images = const [],
    required this.createdAt,
  });

  @override
  List<Object?> get props => [
    id,
    title,
    description,
    location,
    images,
    createdAt,
  ];
}
