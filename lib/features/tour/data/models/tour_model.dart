import '../../domain/entities/tour_entity.dart';

/// Tour model for data layer
class TourModel extends TourEntity {
  const TourModel({
    required super.id,
    required super.guideId,
    required super.title,
    super.description,
    required super.location,
    required super.price,
    required super.durationHours,
    required super.maxParticipants,
    super.images,
    super.rating,
    super.totalReviews,
    super.status,
    required super.createdAt,
    required super.updatedAt,
  });

  /// Create TourModel from JSON (Supabase snake_case)
  factory TourModel.fromJson(Map<String, dynamic> json) {
    return TourModel(
      id: json['id'] as String,
      guideId: json['guide_id'] as String,
      title: json['title'] as String,
      description: json['description'] as String?,
      location: json['location'] as String,
      price: (json['price'] as num).toDouble(),
      durationHours: json['duration_hours'] as int,
      maxParticipants: json['max_participants'] as int? ?? 10,
      images: json['images'] != null
          ? List<String>.from(json['images'] as List)
          : [],
      rating: json['rating'] != null ? (json['rating'] as num).toDouble() : 0.0,
      totalReviews: json['total_reviews'] as int? ?? 0,
      status: json['status'] as String? ?? 'active',
      createdAt: DateTime.parse(json['created_at'] as String),
      updatedAt: DateTime.parse(json['updated_at'] as String),
    );
  }

  /// Create TourModel from ASP.NET API JSON (camelCase)
  factory TourModel.fromApiJson(Map<String, dynamic> json) {
    return TourModel(
      id: json['id'] as String,
      guideId: json['guideId'] as String? ?? '',
      title: json['title'] as String,
      description: json['description'] as String?,
      location: json['location'] as String,
      price: (json['price'] as num).toDouble(),
      durationHours: json['durationHours'] as int,
      maxParticipants: json['maxParticipants'] as int? ?? 10,
      images: json['images'] != null
          ? List<String>.from(json['images'] as List)
          : [],
      rating: json['rating'] != null ? (json['rating'] as num).toDouble() : 0.0,
      totalReviews: json['totalReviews'] as int? ?? 0,
      status: json['status'] as String? ?? 'active',
      createdAt: DateTime.parse(json['createdAt'] as String),
      updatedAt: DateTime.parse(json['updatedAt'] as String),
    );
  }

  /// Convert TourModel to JSON
  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'guide_id': guideId,
      'title': title,
      'description': description,
      'location': location,
      'price': price,
      'duration_hours': durationHours,
      'max_participants': maxParticipants,
      'images': images,
      'rating': rating,
      'total_reviews': totalReviews,
      'status': status,
      'created_at': createdAt.toIso8601String(),
      'updated_at': updatedAt.toIso8601String(),
    };
  }

  /// Create TourModel from TourEntity
  factory TourModel.fromEntity(TourEntity entity) {
    return TourModel(
      id: entity.id,
      guideId: entity.guideId,
      title: entity.title,
      description: entity.description,
      location: entity.location,
      price: entity.price,
      durationHours: entity.durationHours,
      maxParticipants: entity.maxParticipants,
      images: entity.images,
      rating: entity.rating,
      totalReviews: entity.totalReviews,
      status: entity.status,
      createdAt: entity.createdAt,
      updatedAt: entity.updatedAt,
    );
  }
}
