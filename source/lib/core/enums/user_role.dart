/// User roles in the application
enum UserRole {
  traveler('traveler', 'Du khách'),
  guide('guide', 'Hướng dẫn viên'),
  admin('admin', 'Quản trị viên');

  final String value;
  final String displayName;

  const UserRole(this.value, this.displayName);

  /// Get role from string value
  static UserRole fromString(String value) {
    return UserRole.values.firstWhere(
      (role) => role.value == value,
      orElse: () => UserRole.traveler,
    );
  }

  /// Check if role has permission
  bool hasPermission(Permission permission) {
    return _rolePermissions[this]?.contains(permission) ?? false;
  }

  /// Get all permissions for this role
  Set<Permission> get permissions => _rolePermissions[this] ?? {};
}

/// Permissions in the application
enum Permission {
  // Tour permissions
  viewTours,
  createTour,
  editOwnTour,
  deleteOwnTour,
  editAnyTour,
  deleteAnyTour,

  // Booking permissions
  createBooking,
  viewOwnBookings,
  viewTourBookings,
  cancelOwnBooking,
  confirmBooking,
  cancelAnyBooking,

  // Review permissions
  createReview,
  editOwnReview,
  deleteOwnReview,
  deleteAnyReview,

  // Profile permissions
  viewProfiles,
  editOwnProfile,
  editAnyProfile,
  deleteAnyProfile,

  // Admin permissions
  manageUsers,
  viewAnalytics,
  manageSettings,
}

/// Role-Permission mapping
const Map<UserRole, Set<Permission>> _rolePermissions = {
  // Traveler permissions
  UserRole.traveler: {
    Permission.viewTours,
    Permission.createBooking,
    Permission.viewOwnBookings,
    Permission.cancelOwnBooking,
    Permission.createReview,
    Permission.editOwnReview,
    Permission.deleteOwnReview,
    Permission.viewProfiles,
    Permission.editOwnProfile,
  },

  // Guide permissions (includes all traveler permissions + guide-specific)
  UserRole.guide: {
    // Traveler permissions
    Permission.viewTours,
    Permission.createBooking,
    Permission.viewOwnBookings,
    Permission.cancelOwnBooking,
    Permission.createReview,
    Permission.editOwnReview,
    Permission.deleteOwnReview,
    Permission.viewProfiles,
    Permission.editOwnProfile,

    // Guide-specific permissions
    Permission.createTour,
    Permission.editOwnTour,
    Permission.deleteOwnTour,
    Permission.viewTourBookings,
    Permission.confirmBooking,
  },

  // Admin permissions (all permissions)
  UserRole.admin: {
    // All permissions
    Permission.viewTours,
    Permission.createTour,
    Permission.editOwnTour,
    Permission.deleteOwnTour,
    Permission.editAnyTour,
    Permission.deleteAnyTour,
    Permission.createBooking,
    Permission.viewOwnBookings,
    Permission.viewTourBookings,
    Permission.cancelOwnBooking,
    Permission.confirmBooking,
    Permission.cancelAnyBooking,
    Permission.createReview,
    Permission.editOwnReview,
    Permission.deleteOwnReview,
    Permission.deleteAnyReview,
    Permission.viewProfiles,
    Permission.editOwnProfile,
    Permission.editAnyProfile,
    Permission.deleteAnyProfile,
    Permission.manageUsers,
    Permission.viewAnalytics,
    Permission.manageSettings,
  },
};
