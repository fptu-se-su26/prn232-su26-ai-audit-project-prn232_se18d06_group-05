import '../enums/user_role.dart';
import '../../features/auth/domain/entities/user_entity.dart';

/// Service to check user permissions
class PermissionService {
  /// Check if user has a specific permission
  static bool hasPermission(UserEntity? user, Permission permission) {
    if (user == null) return false;

    final role = UserRole.fromString(user.role);
    return role.hasPermission(permission);
  }

  /// Check if user has any of the permissions
  static bool hasAnyPermission(UserEntity? user, List<Permission> permissions) {
    if (user == null) return false;

    return permissions.any((permission) => hasPermission(user, permission));
  }

  /// Check if user has all permissions
  static bool hasAllPermissions(
    UserEntity? user,
    List<Permission> permissions,
  ) {
    if (user == null) return false;

    return permissions.every((permission) => hasPermission(user, permission));
  }

  /// Check if user has a specific role
  static bool hasRole(UserEntity? user, UserRole role) {
    if (user == null) return false;

    return UserRole.fromString(user.role) == role;
  }

  /// Check if user has any of the roles
  static bool hasAnyRole(UserEntity? user, List<UserRole> roles) {
    if (user == null) return false;

    final userRole = UserRole.fromString(user.role);
    return roles.contains(userRole);
  }

  /// Get user role
  static UserRole getUserRole(UserEntity? user) {
    if (user == null) return UserRole.traveler;

    return UserRole.fromString(user.role);
  }

  /// Get all permissions for user
  static Set<Permission> getUserPermissions(UserEntity? user) {
    if (user == null) return {};

    final role = UserRole.fromString(user.role);
    return role.permissions;
  }

  /// Check if user is admin
  static bool isAdmin(UserEntity? user) {
    return hasRole(user, UserRole.admin);
  }

  /// Check if user is guide
  static bool isGuide(UserEntity? user) {
    return hasRole(user, UserRole.guide);
  }

  /// Check if user is traveler
  static bool isTraveler(UserEntity? user) {
    return hasRole(user, UserRole.traveler);
  }

  /// Check if user can create tours
  static bool canCreateTour(UserEntity? user) {
    return hasPermission(user, Permission.createTour);
  }

  /// Check if user can edit tour
  static bool canEditTour(UserEntity? user, String tourGuideId) {
    if (user == null) return false;

    // Admin can edit any tour
    if (hasPermission(user, Permission.editAnyTour)) return true;

    // Guide can edit own tour
    if (hasPermission(user, Permission.editOwnTour) && user.id == tourGuideId) {
      return true;
    }

    return false;
  }

  /// Check if user can delete tour
  static bool canDeleteTour(UserEntity? user, String tourGuideId) {
    if (user == null) return false;

    // Admin can delete any tour
    if (hasPermission(user, Permission.deleteAnyTour)) return true;

    // Guide can delete own tour
    if (hasPermission(user, Permission.deleteOwnTour) &&
        user.id == tourGuideId) {
      return true;
    }

    return false;
  }

  /// Check if user can manage bookings for a tour
  static bool canManageTourBookings(UserEntity? user, String tourGuideId) {
    if (user == null) return false;

    // Admin can manage any bookings
    if (isAdmin(user)) return true;

    // Guide can manage bookings for their tours
    if (hasPermission(user, Permission.viewTourBookings) &&
        user.id == tourGuideId) {
      return true;
    }

    return false;
  }

  /// Check if user can cancel booking
  static bool canCancelBooking(UserEntity? user, String bookingUserId) {
    if (user == null) return false;

    // Admin can cancel any booking
    if (hasPermission(user, Permission.cancelAnyBooking)) return true;

    // User can cancel own booking
    if (hasPermission(user, Permission.cancelOwnBooking) &&
        user.id == bookingUserId) {
      return true;
    }

    return false;
  }
}
