import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../auth/presentation/providers/auth_state_provider.dart';
import '../../../auth/presentation/screens/login_screen.dart';
import '../../../tour/presentation/screens/tour_list_screen.dart';
import '../../../tour/presentation/screens/create_tour_screen.dart';

class GuideDashboardScreen extends ConsumerStatefulWidget {
  const GuideDashboardScreen({super.key});
  @override
  ConsumerState<GuideDashboardScreen> createState() =>
      _GuideDashboardScreenState();
}

class _GuideDashboardScreenState extends ConsumerState<GuideDashboardScreen> {
  int _index = 0;

  late final List<Widget> _pages;

  @override
  void initState() {
    super.initState();
    _pages = const [
      _HomeTab(),
      _ToursTab(),
      _BookingsTab(),
      _MessagesTab(),
      _ProfileTab(),
    ];
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(index: _index, children: _pages),
      bottomNavigationBar: _BottomNav(
        current: _index,
        onTap: (i) => setState(() => _index = i),
      ),
    );
  }
}

// ── Bottom Nav ────────────────────────────────────────────────────────────────

class _BottomNav extends StatelessWidget {
  final int current;
  final ValueChanged<int> onTap;
  const _BottomNav({required this.current, required this.onTap});

  static const _items = [
    (icon: Icons.home_outlined, activeIcon: Icons.home, label: 'Trang chủ'),
    (icon: Icons.explore_outlined, activeIcon: Icons.explore, label: 'Tour'),
    (
      icon: Icons.confirmation_number_outlined,
      activeIcon: Icons.confirmation_number,
      label: 'Booking',
    ),
    (
      icon: Icons.chat_bubble_outline,
      activeIcon: Icons.chat_bubble,
      label: 'Tin nhắn',
    ),
    (icon: Icons.person_outline, activeIcon: Icons.person, label: 'Hồ sơ'),
  ];

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border(top: BorderSide(color: Colors.grey.shade200)),
      ),
      child: SafeArea(
        child: SizedBox(
          height: 58,
          child: Row(
            children: List.generate(_items.length, (i) {
              final item = _items[i];
              final sel = current == i;
              const pink = Color(0xFFE91E8C);
              final color = sel ? pink : Colors.grey.shade500;
              return Expanded(
                child: InkWell(
                  onTap: () => onTap(i),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(
                        sel ? item.activeIcon : item.icon,
                        color: color,
                        size: 22,
                      ),
                      const SizedBox(height: 2),
                      Text(
                        item.label,
                        style: TextStyle(
                          fontSize: 9.5,
                          color: color,
                          fontWeight: sel ? FontWeight.w600 : FontWeight.normal,
                        ),
                      ),
                    ],
                  ),
                ),
              );
            }),
          ),
        ),
      ),
    );
  }
}

// ── Tab: Home ─────────────────────────────────────────────────────────────────

class _HomeTab extends ConsumerWidget {
  const _HomeTab();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(authStateProvider).user;
    final fmt = NumberFormat.currency(
      locale: 'vi_VN',
      symbol: '₫',
      decimalDigits: 0,
    );

    return Scaffold(
      backgroundColor: const Color(0xFFFFF8F7),
      body: SafeArea(
        child: CustomScrollView(
          slivers: [
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(20, 20, 20, 0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Welcome
                    Text(
                      'Xin chào, ${user?.fullName?.split(' ').last ?? 'Guide'}!',
                      style: const TextStyle(
                        fontSize: 28,
                        fontWeight: FontWeight.bold,
                        height: 1.2,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      'Bạn có 3 tour hôm nay',
                      style: TextStyle(
                        fontSize: 16,
                        color: Colors.grey.shade600,
                      ),
                    ),
                    const SizedBox(height: 24),

                    // Earnings card
                    Container(
                      padding: const EdgeInsets.all(24),
                      decoration: BoxDecoration(
                        color: const Color(0xFFFFF0EF),
                        borderRadius: BorderRadius.circular(24),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              const Text(
                                'Thu nhập tháng này',
                                style: TextStyle(
                                  fontWeight: FontWeight.bold,
                                  fontSize: 15,
                                ),
                              ),
                              Container(
                                padding: const EdgeInsets.all(8),
                                decoration: BoxDecoration(
                                  color: Colors.white,
                                  borderRadius: BorderRadius.circular(12),
                                ),
                                child: const Icon(
                                  Icons.trending_up,
                                  color: Color(0xFFE91E8C),
                                  size: 20,
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 12),
                          Text(
                            fmt.format(4852000),
                            style: const TextStyle(
                              fontSize: 32,
                              fontWeight: FontWeight.w900,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Row(
                            children: [
                              const Icon(
                                Icons.arrow_upward,
                                size: 14,
                                color: Colors.green,
                              ),
                              const SizedBox(width: 4),
                              Text(
                                '12% so với tháng trước',
                                style: TextStyle(
                                  fontSize: 13,
                                  color: Colors.green.shade700,
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 16),
                          SizedBox(
                            width: double.infinity,
                            child: ElevatedButton.icon(
                              onPressed: () {},
                              icon: const Icon(
                                Icons.account_balance_wallet,
                                size: 18,
                              ),
                              label: const Text('Rút tiền'),
                              style: ElevatedButton.styleFrom(
                                backgroundColor: const Color(0xFFE91E8C),
                                foregroundColor: Colors.white,
                                padding: const EdgeInsets.symmetric(
                                  vertical: 14,
                                ),
                                shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(14),
                                ),
                                elevation: 0,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),

                    const SizedBox(height: 28),

                    // Upcoming tours
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Tour sắp tới',
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        TextButton(
                          onPressed: () {},
                          child: const Text('Xem tất cả'),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                  ],
                ),
              ),
            ),

            // Horizontal tour cards
            SliverToBoxAdapter(
              child: SizedBox(
                height: 240,
                child: ListView(
                  scrollDirection: Axis.horizontal,
                  padding: const EdgeInsets.symmetric(horizontal: 20),
                  children: const [
                    _UpcomingTourCard(
                      title: 'Phố Cổ Hà Nội',
                      time: '08:00 SA',
                      guests: 6,
                      imageUrl:
                          'https://images.unsplash.com/photo-1555400038-63f5ba517a47?w=400',
                      status: 'confirmed',
                    ),
                    _UpcomingTourCard(
                      title: 'Vịnh Hạ Long',
                      time: '01:30 CH',
                      guests: 4,
                      imageUrl:
                          'https://images.unsplash.com/photo-1528127269322-539801943592?w=400',
                      status: 'confirmed',
                    ),
                    _UpcomingTourCard(
                      title: 'Ruộng bậc thang Sapa',
                      time: '06:00 CH',
                      guests: 8,
                      imageUrl:
                          'https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=400',
                      status: 'pending',
                    ),
                  ],
                ),
              ),
            ),

            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(20, 28, 20, 0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Thao tác nhanh',
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        Expanded(
                          child: _QuickAction(
                            icon: Icons.add_circle,
                            color: const Color(0xFFFFE4E6),
                            iconColor: const Color(0xFFE91E8C),
                            label: 'Tạo tour',
                            sub: 'Thiết kế hành trình',
                            onTap: () => Navigator.push(
                              context,
                              MaterialPageRoute(
                                builder: (_) => const CreateTourScreen(),
                              ),
                            ),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: _QuickAction(
                            icon: Icons.event_available,
                            color: const Color(0xFFDBEAFE),
                            iconColor: Colors.blue,
                            label: 'Lịch trình',
                            sub: 'Quản lý lịch',
                            onTap: () {},
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    _QuickAction(
                      icon: Icons.mail,
                      color: const Color(0xFFD1FAE5),
                      iconColor: Colors.green,
                      label: 'Yêu cầu khách',
                      sub: '4 yêu cầu mới',
                      onTap: () {},
                      fullWidth: true,
                    ),
                    const SizedBox(height: 32),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _UpcomingTourCard extends StatelessWidget {
  final String title, time, imageUrl, status;
  final int guests;
  const _UpcomingTourCard({
    required this.title,
    required this.time,
    required this.guests,
    required this.imageUrl,
    required this.status,
  });

  @override
  Widget build(BuildContext context) {
    final isConfirmed = status == 'confirmed';
    return Container(
      width: 220,
      margin: const EdgeInsets.only(right: 16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.06),
            blurRadius: 12,
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Stack(
            children: [
              ClipRRect(
                borderRadius: const BorderRadius.vertical(
                  top: Radius.circular(20),
                ),
                child: Image.network(
                  imageUrl,
                  height: 130,
                  width: double.infinity,
                  fit: BoxFit.cover,
                  errorBuilder: (_, __, ___) =>
                      Container(height: 130, color: Colors.grey.shade200),
                ),
              ),
              Positioned(
                top: 10,
                left: 10,
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 4,
                  ),
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: 0.9),
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Row(
                    children: [
                      const Icon(
                        Icons.schedule,
                        size: 14,
                        color: Color(0xFFE91E8C),
                      ),
                      const SizedBox(width: 4),
                      Text(
                        time,
                        style: const TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
          Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 14,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                const SizedBox(height: 6),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Row(
                      children: [
                        const Icon(Icons.group, size: 14, color: Colors.grey),
                        const SizedBox(width: 4),
                        Text(
                          '$guests khách',
                          style: const TextStyle(
                            fontSize: 12,
                            color: Colors.grey,
                          ),
                        ),
                      ],
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 8,
                        vertical: 3,
                      ),
                      decoration: BoxDecoration(
                        color: isConfirmed
                            ? Colors.green.withValues(alpha: 0.1)
                            : Colors.orange.withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Text(
                        isConfirmed ? 'Xác nhận' : 'Chờ duyệt',
                        style: TextStyle(
                          fontSize: 10,
                          fontWeight: FontWeight.bold,
                          color: isConfirmed ? Colors.green : Colors.orange,
                        ),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _QuickAction extends StatelessWidget {
  final IconData icon;
  final Color color, iconColor;
  final String label, sub;
  final VoidCallback onTap;
  final bool fullWidth;
  const _QuickAction({
    required this.icon,
    required this.color,
    required this.iconColor,
    required this.label,
    required this.sub,
    required this.onTap,
    this.fullWidth = false,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(16),
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.04),
              blurRadius: 8,
            ),
          ],
        ),
        child: Row(
          children: [
            Container(
              width: 48,
              height: 48,
              decoration: BoxDecoration(
                color: color,
                borderRadius: BorderRadius.circular(14),
              ),
              child: Icon(icon, color: iconColor, size: 26),
            ),
            const SizedBox(width: 12),
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 14,
                  ),
                ),
                Text(
                  sub,
                  style: TextStyle(fontSize: 12, color: Colors.grey.shade600),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

// ── Tab: Tours ────────────────────────────────────────────────────────────────

class _ToursTab extends StatelessWidget {
  const _ToursTab();
  @override
  Widget build(BuildContext context) => const TourListScreen();
}

// ── Tab: Bookings ─────────────────────────────────────────────────────────────

class _BookingsTab extends StatelessWidget {
  const _BookingsTab();
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text(
          'Booking',
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
        backgroundColor: Colors.white,
        foregroundColor: Colors.black,
        elevation: 0,
      ),
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.confirmation_number_outlined,
              size: 64,
              color: Colors.grey.shade300,
            ),
            const SizedBox(height: 16),
            const Text(
              'Chưa có booking',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Tab: Messages ─────────────────────────────────────────────────────────────

class _MessagesTab extends StatelessWidget {
  const _MessagesTab();
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text(
          'Tin nhắn',
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
        backgroundColor: Colors.white,
        foregroundColor: Colors.black,
        elevation: 0,
      ),
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.chat_bubble_outline,
              size: 64,
              color: Colors.grey.shade300,
            ),
            const SizedBox(height: 16),
            const Text(
              'Chưa có tin nhắn',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Tab: Profile ──────────────────────────────────────────────────────────────

class _ProfileTab extends ConsumerWidget {
  const _ProfileTab();
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(authStateProvider).user;
    return Scaffold(
      appBar: AppBar(
        title: const Text(
          'Hồ sơ',
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
        backgroundColor: Colors.white,
        foregroundColor: Colors.black,
        elevation: 0,
      ),
      body: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          Center(
            child: Column(
              children: [
                CircleAvatar(
                  radius: 48,
                  backgroundColor: Colors.grey.shade200,
                  child: Text(
                    user?.fullName?.isNotEmpty == true
                        ? user!.fullName![0].toUpperCase()
                        : '?',
                    style: const TextStyle(
                      fontSize: 36,
                      fontWeight: FontWeight.bold,
                      color: Colors.black54,
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                Text(
                  user?.fullName ?? 'Guide',
                  style: const TextStyle(
                    fontSize: 22,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                Text(
                  user?.email ?? '',
                  style: TextStyle(color: Colors.grey.shade600),
                ),
              ],
            ),
          ),
          const SizedBox(height: 32),
          const Divider(),
          ListTile(
            leading: const Icon(Icons.person_outline),
            title: const Text('Thông tin cá nhân'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () {},
          ),
          ListTile(
            leading: const Icon(Icons.settings_outlined),
            title: const Text('Cài đặt'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () {},
          ),
          const Divider(),
          OutlinedButton.icon(
            onPressed: () async {
              final ok = await showDialog<bool>(
                context: context,
                builder: (_) => AlertDialog(
                  title: const Text('Đăng xuất'),
                  content: const Text('Bạn có chắc muốn đăng xuất?'),
                  actions: [
                    TextButton(
                      onPressed: () => Navigator.pop(context, false),
                      child: const Text('Huỷ'),
                    ),
                    TextButton(
                      onPressed: () => Navigator.pop(context, true),
                      child: const Text(
                        'Đăng xuất',
                        style: TextStyle(color: Colors.red),
                      ),
                    ),
                  ],
                ),
              );
              if (ok == true && context.mounted) {
                await ref.read(authStateProvider.notifier).signOut();
                if (context.mounted) {
                  Navigator.of(context).pushAndRemoveUntil(
                    MaterialPageRoute(builder: (_) => const LoginScreen()),
                    (_) => false,
                  );
                }
              }
            },
            icon: const Icon(Icons.logout, color: Colors.red),
            label: const Text(
              'Đăng xuất',
              style: TextStyle(color: Colors.red, fontSize: 15),
            ),
            style: OutlinedButton.styleFrom(
              side: const BorderSide(color: Colors.red),
              padding: const EdgeInsets.symmetric(vertical: 14),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(10),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
