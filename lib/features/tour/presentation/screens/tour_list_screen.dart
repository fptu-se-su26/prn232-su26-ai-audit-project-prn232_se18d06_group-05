import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/enums/user_role.dart';
import '../../../../core/widgets/permission_widget.dart';
import '../../../auth/presentation/providers/auth_state_provider.dart';
import '../providers/tour_list_provider.dart';
import '../widgets/tour_card.dart';

class TourListScreen extends ConsumerStatefulWidget {
  const TourListScreen({super.key});

  @override
  ConsumerState<TourListScreen> createState() => _TourListScreenState();
}

class _TourListScreenState extends ConsumerState<TourListScreen> {
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    // Load tours when screen opens
    Future.microtask(() => ref.read(tourListProvider.notifier).loadTours());
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final tourState = ref.watch(tourListProvider);
    final authState = ref.watch(authStateProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Khám phá Tours'),
        elevation: 0,
        actions: [
          // Create tour button (only for guides)
          PermissionIconButton(
            permission: Permission.createTour,
            onPressed: () {
              ScaffoldMessenger.of(
                context,
              ).showSnackBar(const SnackBar(content: Text('Tạo tour mới')));
            },
            icon: const Icon(Icons.add),
            tooltip: 'Tạo tour',
          ),
        ],
      ),
      drawer: _buildDrawer(context, ref, authState),
      body: Column(
        children: [
          // Search bar
          Padding(
            padding: const EdgeInsets.all(16),
            child: TextField(
              controller: _searchController,
              decoration: InputDecoration(
                hintText: 'Tìm kiếm tour, địa điểm...',
                prefixIcon: const Icon(Icons.search),
                suffixIcon: _searchController.text.isNotEmpty
                    ? IconButton(
                        icon: const Icon(Icons.clear),
                        onPressed: () {
                          _searchController.clear();
                          ref.read(tourListProvider.notifier).loadTours();
                        },
                      )
                    : null,
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
              onSubmitted: (value) {
                if (value.trim().isNotEmpty) {
                  ref.read(tourListProvider.notifier).searchTours(value);
                }
              },
            ),
          ),

          // Tour list
          Expanded(child: _buildTourList(tourState)),
        ],
      ),
    );
  }

  Widget _buildTourList(TourListState state) {
    if (state.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (state.error != null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.error_outline, size: 64, color: Colors.red),
            const SizedBox(height: 16),
            Text(
              state.error!,
              style: const TextStyle(fontSize: 16),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 16),
            ElevatedButton(
              onPressed: () {
                ref.read(tourListProvider.notifier).loadTours();
              },
              child: const Text('Thử lại'),
            ),
          ],
        ),
      );
    }

    if (state.tours.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.explore_off, size: 64, color: Colors.grey),
            const SizedBox(height: 16),
            Text(
              _searchController.text.isEmpty
                  ? 'Chưa có tour nào'
                  : 'Không tìm thấy tour phù hợp',
              style: const TextStyle(fontSize: 16, color: Colors.grey),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => ref.read(tourListProvider.notifier).refresh(),
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: state.tours.length,
        itemBuilder: (context, index) {
          final tour = state.tours[index];
          return TourCard(tour: tour);
        },
      ),
    );
  }
}

Widget _buildDrawer(BuildContext context, WidgetRef ref, authState) {
  final user = authState.user;

  return Drawer(
    child: ListView(
      padding: EdgeInsets.zero,
      children: [
        UserAccountsDrawerHeader(
          decoration: const BoxDecoration(color: Colors.blue),
          accountName: Text(user?.fullName ?? 'User'),
          accountEmail: Text(user?.email ?? ''),
          currentAccountPicture: CircleAvatar(
            backgroundColor: Colors.white,
            child: Text(
              user?.fullName?.substring(0, 1).toUpperCase() ?? 'U',
              style: const TextStyle(fontSize: 24, color: Colors.blue),
            ),
          ),
        ),
        ListTile(
          leading: const Icon(Icons.explore),
          title: const Text('Tours'),
          selected: true,
          onTap: () {
            Navigator.pop(context);
          },
        ),
        ListTile(
          leading: const Icon(Icons.book),
          title: const Text('Bookings của tôi'),
          onTap: () {
            Navigator.pop(context);
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('Bookings (Coming soon)')),
            );
          },
        ),
        ListTile(
          leading: const Icon(Icons.person),
          title: const Text('Hồ sơ'),
          onTap: () {
            Navigator.pop(context);
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('Profile (Coming soon)')),
            );
          },
        ),
        const Divider(),
        ListTile(
          leading: const Icon(Icons.logout, color: Colors.red),
          title: const Text('Đăng xuất', style: TextStyle(color: Colors.red)),
          onTap: () async {
            final confirm = await showDialog<bool>(
              context: context,
              builder: (context) => AlertDialog(
                title: const Text('Xác nhận đăng xuất'),
                content: const Text('Bạn có chắc muốn đăng xuất?'),
                actions: [
                  TextButton(
                    onPressed: () => Navigator.pop(context, false),
                    child: const Text('Hủy'),
                  ),
                  TextButton(
                    onPressed: () => Navigator.pop(context, true),
                    style: TextButton.styleFrom(foregroundColor: Colors.red),
                    child: const Text('Đăng xuất'),
                  ),
                ],
              ),
            );

            if (confirm == true && context.mounted) {
              await ref.read(authStateProvider.notifier).signOut();
              if (context.mounted) {
                Navigator.of(context).pushReplacementNamed('/login');
              }
            }
          },
        ),
      ],
    ),
  );
}
