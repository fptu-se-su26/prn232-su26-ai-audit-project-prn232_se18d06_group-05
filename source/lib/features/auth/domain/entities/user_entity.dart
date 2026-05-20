/// User entity - Domain model
class UserEntity {
  final String id;
  final String email;
  final String? fullName;
  final String? phone;
  final String? avatarUrl;
  final String role;
  final DateTime createdAt;

  const UserEntity({
    required this.id,
    required this.email,
    this.fullName,
    this.phone,
    this.avatarUrl,
    required this.role,
    required this.createdAt,
  });

  bool get isTraveler => role == 'traveler';
  bool get isGuide => role == 'guide';
  bool get isAdmin => role == 'admin';

  @override
  String toString() => 'UserEntity(id: $id, email: $email, role: $role)';
}
