import '../../domain/entities/tour_entity.dart';

/// Tour model for data layer - compatible with new database schema
class TourModel extends TourEntity {
  const TourModel({
    required super.id,
    required super.tourTemplateId,
    required super.guideId,
    required super.price,
    required super.durationHours,
    required super.maxParticipants,
    super.status,
    super.rating,
    super.totalReviews,
    required super.title,
    super.description,
    required super.location,
    super.images,
    required super.createdAt,
    required super.updatedAt,
  });

  /// Create TourModel from JSON (Supabase - joined with tour_templates)
  factory TourModel.fromJson(Map<String, dynamic> json) {
    return TourModel(
      id: json['id'] as String,
      tourTemplateId: json['tour_template_id'] as String,
      guideId: json['guide_id'] as String,
      price: (json['price'] as num).toDouble(),
      durationHours: json['duration_hours'] as int,
      maxParticipants: json['max_participants'] as int? ?? 10,
      status: json['status'] as String? ?? 'active',
      rating: json['rating'] != null ? (json['rating'] as num).toDouble() : 0.0,
      totalReviews: json['total_reviews'] as int? ?? 0,
      // From tour_templates join
      title: json['title'] as String,
      description: json['description'] as String?,
      location: json['location'] as String,
      images: json['images'] != null
          ? List<String>.from(json['images'] as List)
          : [],
      createdAt: DateTime.parse(json['created_at'] as String),
      updatedAt: DateTime.parse(json['updated_at'] as String),
    );
  }

  /// Create TourModel from ASP.NET API JSON (camelCase)
  factory TourModel.fromApiJson(Map<String, dynamic> json) {
    return TourModel(
      id: json['id'] as String,
      tourTemplateId: json['tourTemplateId'] as String,
      guideId: json['guideId'] as String,
      price: (json['price'] as num).toDouble(),
      durationHours: json['durationHours'] as int,
      maxParticipants: json['maxParticipants'] as int? ?? 10,
      status: json['status'] as String? ?? 'active',
      rating: json['rating'] != null ? (json['rating'] as num).toDouble() : 0.0,
      totalReviews: json['totalReviews'] as int? ?? 0,
      title: json['title'] as String,
      description: json['description'] as String?,
      location: json['location'] as String,
      images: json['images'] != null
          ? List<String>.from(json['images'] as List)
          : [],
      createdAt: DateTime.parse(json['createdAt'] as String),
      updatedAt: DateTime.parse(json['updatedAt'] as String),
    );
  }

  /// Convert TourModel to JSON for creating/updating
  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'tour_template_id': tourTemplateId,
      'guide_id': guideId,
      'price': price,
      'duration_hours': durationHours,
      'max_participants': maxParticipants,
      'status': status,
      'rating': rating,
      'total_reviews': totalReviews,
    };
  }

  /// Convert to JSON for tour template creation
  Map<String, dynamic> toTemplateJson() {
    return {
      'title': title,
      'description': description,
      'location': location,
      'images': images,
    };
  }

  /// Create TourModel from TourEntity
  factory TourModel.fromEntity(TourEntity entity) {
    return TourModel(
      id: entity.id,
      tourTemplateId: entity.tourTemplateId,
      guideId: entity.guideId,
      price: entity.price,
      durationHours: entity.durationHours,
      maxParticipants: entity.maxParticipants,
      status: entity.status,
      rating: entity.rating,
      totalReviews: entity.totalReviews,
      title: entity.title,
      description: entity.description,
      location: entity.location,
      images: entity.images,
      createdAt: entity.createdAt,
      updatedAt: entity.updatedAt,
    );
  }
}
