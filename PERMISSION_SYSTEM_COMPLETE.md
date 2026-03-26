# ✅ Permission System - Hoàn thành

## 🎉 Tổng quan

Đã hoàn thành hệ thống phân quyền (Role-Based Access Control) cho TripMate với 3 roles và nhiều permissions.

## 📦 Files đã tạo

### Core - Permission System
```
lib/core/
├── enums/
│   └── user_role.dart                      # Roles & Permissions enum
├── services/
│   └── permission_service.dart             # Permission checking service
└── widgets/
    └── permission_widget.dart              # Permission-based widgets
```

### Features - Role Selection
```
lib/features/auth/presentation/screens/
└── role_selection_screen.dart              # Màn hình chọn role
```

### Updated Files
```
lib/features/tour/presentation/
├── widgets/
│   └── tour_card.dart                      # Thêm edit/delete buttons
└── screens/
    └── tour_list_screen.dart               # Thêm create tour button
```

## 🎭 Roles & Permissions

### 1. Traveler (Du khách)
**Mặc định cho user mới**

Permissions:
- ✅ Xem tours
- ✅ Tạo booking
- ✅ Xem bookings của mình
- ✅ Hủy booking của mình
- ✅ Tạo review
- ✅ Sửa/xóa review của mình
- ✅ Xem profiles
- ✅ Sửa profile của mình

### 2. Guide (Hướng dẫn viên)
**Bao gồm tất cả quyền của Traveler + thêm:**

Additional Permissions:
- ✅ Tạo tour
- ✅ Sửa tour của mình
- ✅ Xóa tour của mình
- ✅ Xem bookings của tours mình tạo
- ✅ Xác nhận booking

### 3. Admin (Quản trị viên)
**Có tất cả quyền trong hệ thống**

Additional Permissions:
- ✅ Sửa/xóa bất kỳ tour nào
- ✅ Hủy bất kỳ booking nào
- ✅ Xóa bất kỳ review nào
- ✅ Sửa/xóa bất kỳ profile nào
- ✅ Quản lý users
- ✅ Xem analytics
- ✅ Quản lý settings

## 🔧 Cách sử dụng

### 1. Check Permission trong code

```dart
import 'package:flutter_tripmate_application/core/services/permission_service.dart';
import 'package:flutter_tripmate_application/core/enums/user_role.dart';

// Check single permission
if (PermissionService.hasPermission(user, Permission.createTour)) {
  // User có quyền tạo tour
}

// Check role
if (PermissionService.isGuide(user)) {
  // User là guide
}

// Check if can edit tour
if (PermissionService.canEditTour(user, tourGuideId)) {
  // User có quyền edit tour này
}
```

### 2. Sử dụng Permission Widgets

#### PermissionWidget
Hiển thị widget chỉ khi user có permission:

```dart
PermissionWidget(
  permission: Permission.createTour,
  child: ElevatedButton(
    onPressed: () => createTour(),
    child: const Text('Tạo Tour'),
  ),
  fallback: const Text('Bạn không có quyền tạo tour'),
)
```

#### RoleWidget
Hiển thị widget chỉ khi user có role:

```dart
RoleWidget(
  role: UserRole.guide,
  child: const Text('Chào mừng Guide!'),
  fallback: const Text('Bạn không phải guide'),
)
```

#### PermissionButton
Button tự động disable nếu không có quyền:

```dart
PermissionButton(
  permission: Permission.createTour,
  onPressed: () => createTour(),
  child: const Text('Tạo Tour'),
)
```

#### PermissionIconButton
Icon button với permission check:

```dart
PermissionIconButton(
  permission: Permission.editOwnTour,
  onPressed: () => editTour(),
  icon: const Icon(Icons.edit),
  tooltip: 'Chỉnh sửa',
)
```

### 3. Role Selection Screen

```dart
Navigator.push(
  context,
  MaterialPageRoute(
    builder: (context) => RoleSelectionScreen(
      onRoleSelected: (role) {
        // Update user role
        updateUserRole(role);
        Navigator.pop(context);
      },
    ),
  ),
);
```

## 🎨 UI Updates

### Tour Card
- Hiển thị nút Edit/Delete cho tours của guide
- Chỉ guide owner mới thấy nút
- Có confirmation dialog khi xóa

### Tour List Screen
- Nút "+" ở AppBar để tạo tour
- Chỉ guide và admin mới thấy nút này

## 🔐 Database RLS Policies

Cập nhật RLS policies để match với permission system:

```sql
-- Tours: Guides can create tours
CREATE POLICY "Guides can create tours"
  ON tours FOR INSERT
  WITH CHECK (
    auth.uid() = guide_id AND (
      SELECT role FROM profiles WHERE id = auth.uid()
    ) IN ('guide', 'admin')
  );

-- Tours: Guides can update own tours
CREATE POLICY "Guides can update own tours"
  ON tours FOR UPDATE
  USING (auth.uid() = guide_id);

-- Tours: Guides can delete own tours
CREATE POLICY "Guides can delete own tours"
  ON tours FOR DELETE
  USING (auth.uid() = guide_id);
```

## 📱 User Flow

### Đăng ký mới
1. User đăng ký → Role mặc định: `traveler`
2. (Optional) Hiển thị Role Selection Screen
3. User chọn role → Update profile

### Chuyển đổi role
1. User vào Settings
2. Click "Chuyển đổi vai trò"
3. Chọn role mới
4. Confirm → Update profile

## 🧪 Testing

### Test Permission Service

```dart
test('Traveler cannot create tour', () {
  final traveler = UserEntity(role: 'traveler', ...);
  
  expect(
    PermissionService.hasPermission(traveler, Permission.createTour),
    false,
  );
});

test('Guide can create tour', () {
  final guide = UserEntity(role: 'guide', ...);
  
  expect(
    PermissionService.hasPermission(guide, Permission.createTour),
    true,
  );
});

test('Guide can edit own tour', () {
  final guide = UserEntity(id: 'guide-123', role: 'guide', ...);
  
  expect(
    PermissionService.canEditTour(guide, 'guide-123'),
    true,
  );
});

test('Guide cannot edit other tour', () {
  final guide = UserEntity(id: 'guide-123', role: 'guide', ...);
  
  expect(
    PermissionService.canEditTour(guide, 'other-guide-id'),
    false,
  );
});
```

### Test Permission Widgets

```dart
testWidgets('PermissionWidget shows child when has permission', (tester) async {
  // Setup provider with guide user
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        authStateProvider.overrideWith((ref) => AuthState(
          user: UserEntity(role: 'guide', ...),
        )),
      ],
      child: MaterialApp(
        home: PermissionWidget(
          permission: Permission.createTour,
          child: const Text('Create Tour'),
        ),
      ),
    ),
  );
  
  expect(find.text('Create Tour'), findsOneWidget);
});
```

## 🔄 Permission Matrix

| Permission | Traveler | Guide | Admin |
|-----------|----------|-------|-------|
| View Tours | ✅ | ✅ | ✅ |
| Create Tour | ❌ | ✅ | ✅ |
| Edit Own Tour | ❌ | ✅ | ✅ |
| Edit Any Tour | ❌ | ❌ | ✅ |
| Delete Own Tour | ❌ | ✅ | ✅ |
| Delete Any Tour | ❌ | ❌ | ✅ |
| Create Booking | ✅ | ✅ | ✅ |
| View Own Bookings | ✅ | ✅ | ✅ |
| View Tour Bookings | ❌ | ✅ | ✅ |
| Cancel Own Booking | ✅ | ✅ | ✅ |
| Cancel Any Booking | ❌ | ❌ | ✅ |
| Confirm Booking | ❌ | ✅ | ✅ |
| Create Review | ✅ | ✅ | ✅ |
| Edit Own Review | ✅ | ✅ | ✅ |
| Delete Any Review | ❌ | ❌ | ✅ |
| Manage Users | ❌ | ❌ | ✅ |

## 📝 Best Practices

### 1. Always check permissions
```dart
// ❌ Bad
if (user.role == 'guide') {
  createTour();
}

// ✅ Good
if (PermissionService.hasPermission(user, Permission.createTour)) {
  createTour();
}
```

### 2. Use permission widgets
```dart
// ❌ Bad
if (PermissionService.hasPermission(user, Permission.createTour)) {
  return ElevatedButton(...);
}
return SizedBox.shrink();

// ✅ Good
return PermissionWidget(
  permission: Permission.createTour,
  child: ElevatedButton(...),
);
```

### 3. Check ownership
```dart
// ✅ Good
if (PermissionService.canEditTour(user, tour.guideId)) {
  editTour();
}
```

## 🚀 Next Steps

### 1. Implement Role Switching
- Tạo Settings screen
- Thêm "Chuyển đổi vai trò" option
- Update profile role

### 2. Add Admin Panel
- User management
- Analytics dashboard
- System settings

### 3. Enhance Permissions
- Add more granular permissions
- Permission groups
- Custom permissions per user

### 4. Audit Log
- Log permission checks
- Track role changes
- Monitor access

## 🎯 Usage Examples

### Example 1: Tour Management
```dart
// In tour list screen
PermissionIconButton(
  permission: Permission.createTour,
  onPressed: () => navigateToCreateTour(),
  icon: const Icon(Icons.add),
)

// In tour card
if (PermissionService.canEditTour(user, tour.guideId)) {
  IconButton(
    icon: const Icon(Icons.edit),
    onPressed: () => editTour(tour),
  )
}
```

### Example 2: Booking Management
```dart
// Cancel booking button
PermissionButton(
  permission: Permission.cancelOwnBooking,
  onPressed: () => cancelBooking(),
  child: const Text('Hủy booking'),
)

// Confirm booking (guide only)
if (PermissionService.canManageTourBookings(user, tour.guideId)) {
  ElevatedButton(
    onPressed: () => confirmBooking(),
    child: const Text('Xác nhận'),
  )
}
```

## ✅ Checklist

- [x] Tạo UserRole enum với 3 roles
- [x] Tạo Permission enum với tất cả permissions
- [x] Tạo role-permission mapping
- [x] Implement PermissionService
- [x] Tạo Permission widgets
- [x] Update Tour Card với edit/delete buttons
- [x] Update Tour List Screen với create button
- [x] Tạo Role Selection Screen
- [ ] Implement role switching
- [ ] Add admin panel
- [ ] Add audit logging

## 🎓 Key Concepts

1. **Role-Based Access Control (RBAC)**: Phân quyền dựa trên vai trò
2. **Permission Checking**: Kiểm tra quyền trước khi thực hiện action
3. **UI Adaptation**: UI tự động thay đổi theo quyền của user
4. **Ownership Check**: Kiểm tra quyền sở hữu resource
5. **Fallback UI**: Hiển thị UI thay thế khi không có quyền

Hệ thống phân quyền đã sẵn sàng sử dụng! 🎉
