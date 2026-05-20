import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../features/auth/presentation/providers/auth_state_provider.dart';
import '../enums/user_role.dart';
import '../services/permission_service.dart';

/// Widget that shows content based on user permissions
class PermissionWidget extends ConsumerWidget {
  final Permission permission;
  final Widget child;
  final Widget? fallback;

  const PermissionWidget({
    super.key,
    required this.permission,
    required this.child,
    this.fallback,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authStateProvider);
    final user = authState.user;

    if (PermissionService.hasPermission(user, permission)) {
      return child;
    }

    return fallback ?? const SizedBox.shrink();
  }
}

/// Widget that shows content based on user role
class RoleWidget extends ConsumerWidget {
  final UserRole role;
  final Widget child;
  final Widget? fallback;

  const RoleWidget({
    super.key,
    required this.role,
    required this.child,
    this.fallback,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authStateProvider);
    final user = authState.user;

    if (PermissionService.hasRole(user, role)) {
      return child;
    }

    return fallback ?? const SizedBox.shrink();
  }
}

/// Widget that shows content if user has any of the roles
class AnyRoleWidget extends ConsumerWidget {
  final List<UserRole> roles;
  final Widget child;
  final Widget? fallback;

  const AnyRoleWidget({
    super.key,
    required this.roles,
    required this.child,
    this.fallback,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authStateProvider);
    final user = authState.user;

    if (PermissionService.hasAnyRole(user, roles)) {
      return child;
    }

    return fallback ?? const SizedBox.shrink();
  }
}

/// Widget that shows content if user has any of the permissions
class AnyPermissionWidget extends ConsumerWidget {
  final List<Permission> permissions;
  final Widget child;
  final Widget? fallback;

  const AnyPermissionWidget({
    super.key,
    required this.permissions,
    required this.child,
    this.fallback,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authStateProvider);
    final user = authState.user;

    if (PermissionService.hasAnyPermission(user, permissions)) {
      return child;
    }

    return fallback ?? const SizedBox.shrink();
  }
}

/// Button that is only enabled if user has permission
class PermissionButton extends ConsumerWidget {
  final Permission permission;
  final VoidCallback onPressed;
  final Widget child;
  final ButtonStyle? style;

  const PermissionButton({
    super.key,
    required this.permission,
    required this.onPressed,
    required this.child,
    this.style,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authStateProvider);
    final user = authState.user;

    final hasPermission = PermissionService.hasPermission(user, permission);

    return ElevatedButton(
      onPressed: hasPermission ? onPressed : null,
      style: style,
      child: child,
    );
  }
}

/// Icon button that is only enabled if user has permission
class PermissionIconButton extends ConsumerWidget {
  final Permission permission;
  final VoidCallback onPressed;
  final Icon icon;
  final String? tooltip;

  const PermissionIconButton({
    super.key,
    required this.permission,
    required this.onPressed,
    required this.icon,
    this.tooltip,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authStateProvider);
    final user = authState.user;

    final hasPermission = PermissionService.hasPermission(user, permission);

    return IconButton(
      onPressed: hasPermission ? onPressed : null,
      icon: icon,
      tooltip: tooltip,
    );
  }
}
