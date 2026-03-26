import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/enums/user_role.dart';

class RoleSelectionScreen extends ConsumerStatefulWidget {
  final Function(UserRole) onRoleSelected;

  const RoleSelectionScreen({super.key, required this.onRoleSelected});

  @override
  ConsumerState<RoleSelectionScreen> createState() =>
      _RoleSelectionScreenState();
}

class _RoleSelectionScreenState extends ConsumerState<RoleSelectionScreen> {
  UserRole? _selectedRole;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Chọn vai trò'), elevation: 0),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'Bạn muốn sử dụng TripMate với vai trò gì?',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 32),

            // Traveler role
            _buildRoleCard(
              role: UserRole.traveler,
              icon: Icons.explore,
              title: UserRole.traveler.displayName,
              description: 'Khám phá và đặt các tour du lịch',
              features: const [
                'Tìm kiếm và đặt tour',
                'Đánh giá tour',
                'Quản lý booking',
              ],
            ),
            const SizedBox(height: 16),

            // Guide role
            _buildRoleCard(
              role: UserRole.guide,
              icon: Icons.tour,
              title: UserRole.guide.displayName,
              description: 'Tạo và quản lý tour của bạn',
              features: const [
                'Tạo và quản lý tour',
                'Nhận booking từ khách',
                'Xem thống kê',
              ],
            ),
            const SizedBox(height: 32),

            // Continue button
            ElevatedButton(
              onPressed: _selectedRole != null
                  ? () => widget.onRoleSelected(_selectedRole!)
                  : null,
              style: ElevatedButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 16),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
              child: const Text('Tiếp tục', style: TextStyle(fontSize: 16)),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildRoleCard({
    required UserRole role,
    required IconData icon,
    required String title,
    required String description,
    required List<String> features,
  }) {
    final isSelected = _selectedRole == role;

    return Card(
      elevation: isSelected ? 4 : 1,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(
          color: isSelected ? Colors.blue : Colors.grey.shade300,
          width: isSelected ? 2 : 1,
        ),
      ),
      child: InkWell(
        onTap: () {
          setState(() {
            _selectedRole = role;
          });
        },
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Icon(
                    icon,
                    size: 32,
                    color: isSelected ? Colors.blue : Colors.grey,
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          title,
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                            color: isSelected ? Colors.blue : Colors.black,
                          ),
                        ),
                        Text(
                          description,
                          style: const TextStyle(
                            fontSize: 14,
                            color: Colors.grey,
                          ),
                        ),
                      ],
                    ),
                  ),
                  if (isSelected)
                    const Icon(Icons.check_circle, color: Colors.blue),
                ],
              ),
              const SizedBox(height: 12),
              ...features.map(
                (feature) => Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Row(
                    children: [
                      Icon(
                        Icons.check,
                        size: 16,
                        color: isSelected ? Colors.blue : Colors.grey,
                      ),
                      const SizedBox(width: 8),
                      Text(feature, style: const TextStyle(fontSize: 14)),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
