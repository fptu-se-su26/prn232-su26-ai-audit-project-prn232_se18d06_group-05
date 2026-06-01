import 'package:equatable/equatable.dart';

/// Guide Certificate entity
class GuideCertificateEntity extends Equatable {
  final String id;
  final String guideId;
  final String certificateName;
  final String fileUrl;
  final String status; // pending | verified | rejected
  final DateTime createdAt;

  const GuideCertificateEntity({
    required this.id,
    required this.guideId,
    required this.certificateName,
    required this.fileUrl,
    required this.status,
    required this.createdAt,
  });

  @override
  List<Object?> get props => [
    id,
    guideId,
    certificateName,
    fileUrl,
    status,
    createdAt,
  ];

  bool get isPending => status == 'pending';
  bool get isVerified => status == 'verified';
  bool get isRejected => status == 'rejected';

  String get statusDisplayName {
    switch (status) {
      case 'pending':
        return 'Chờ xét duyệt';
      case 'verified':
        return 'Đã xác minh';
      case 'rejected':
        return 'Bị từ chối';
      default:
        return 'Không xác định';
    }
  }
}
