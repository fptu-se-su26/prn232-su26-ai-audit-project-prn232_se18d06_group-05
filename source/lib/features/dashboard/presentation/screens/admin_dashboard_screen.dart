import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../auth/presentation/providers/auth_state_provider.dart';
import '../../../auth/presentation/screens/login_screen.dart';

class AdminDashboardScreen extends ConsumerStatefulWidget {
  const AdminDashboardScreen({super.key});
  @override
  ConsumerState<AdminDashboardScreen> createState() =>
      _AdminDashboardScreenState();
}

class _AdminDashboardScreenState extends ConsumerState<AdminDashboardScreen> {
  int _index = 0;

  final _pages = const [
    _OverviewTab(),
    _ApprovalsTab(),
    _ReportsTab(),
    _ProfileTab(),
  ];

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

class _BottomNav extends StatelessWidget {
  final int current;
  final ValueChanged<int> onTap;
  const _BottomNav({required this.current, required this.onTap});

  static const _items = [
    (
      icon: Icons.dashboard_outlined,
      activeIcon: Icons.dashboard,
      label: 'Tổng quan',
    ),
    (icon: Icons.approval_outlined, activeIcon: Icons.approval, label: 'Duyệt'),
    (icon: Icons.flag_outlined, activeIcon: Icons.flag, label: 'Báo cáo'),
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

// ── Tab: Overview ─────────────────────────────────────────────────────────────

class _OverviewTab extends StatelessWidget {
  const _OverviewTab();

  @override
  Widget build(BuildContext context) {
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
                    const Text(
                      'Platform Overview',
                      style: TextStyle(
                        fontSize: 28,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      'Chào mừng trở lại, Admin.',
                      style: TextStyle(
                        fontSize: 15,
                        color: Colors.grey.shade600,
                      ),
                    ),
                    const SizedBox(height: 24),

                    // Stats grid
                    Row(
                      children: [
                        Expanded(
                          child: _StatCard(
                            label: 'Tổng doanh thu',
                            value: fmt.format(124592000),
                            icon: Icons.trending_up,
                            iconColor: Colors.green,
                            chart: true,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: _StatCard(
                            label: 'Booking mới',
                            value: '482',
                            sub: '+12% tuần này',
                            icon: Icons.confirmation_number,
                            iconColor: const Color(0xFFE91E8C),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        Expanded(
                          child: _StatCard(
                            label: 'Người dùng',
                            value: '1,204',
                            icon: Icons.people,
                            iconColor: Colors.blue,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: _StatCard(
                            label: 'Tour đang hoạt động',
                            value: '87',
                            icon: Icons.explore,
                            iconColor: Colors.orange,
                          ),
                        ),
                      ],
                    ),

                    const SizedBox(height: 28),

                    // Recent activity
                    const Text(
                      'Hoạt động gần đây',
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 16),
                  ],
                ),
              ),
            ),

            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 20),
                child: Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(20),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withValues(alpha: 0.04),
                        blurRadius: 12,
                      ),
                    ],
                  ),
                  child: Column(
                    children: const [
                      _ActivityItem(
                        icon: Icons.bolt,
                        iconBg: Color(0xFFFFE4E6),
                        iconColor: Color(0xFFE91E8C),
                        title: 'Guide mới đăng ký',
                        sub: 'Nguyễn Văn A vừa tham gia nền tảng.',
                        time: '2 phút trước',
                      ),
                      _ActivityItem(
                        icon: Icons.payments,
                        iconBg: Color(0xFFD1FAE5),
                        iconColor: Colors.green,
                        title: 'Thanh toán đã xử lý',
                        sub: 'Payout tuần cho 42 guide đã hoàn tất.',
                        time: '45 phút trước',
                      ),
                      _ActivityItem(
                        icon: Icons.report,
                        iconBg: Color(0xFFFEF3C7),
                        iconColor: Colors.orange,
                        title: 'Báo cáo mới',
                        sub: 'Có 3 báo cáo lỗi UI cần xem xét.',
                        time: '3 giờ trước',
                      ),
                      _ActivityItem(
                        icon: Icons.reviews,
                        iconBg: Color(0xFFF3F4F6),
                        iconColor: Colors.grey,
                        title: 'Review bị gắn cờ',
                        sub: 'Cần kiểm duyệt nội dung Booking #8821.',
                        time: 'Hôm qua',
                        isLast: true,
                      ),
                    ],
                  ),
                ),
              ),
            ),
            const SliverToBoxAdapter(child: SizedBox(height: 32)),
          ],
        ),
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  final String label, value;
  final String? sub;
  final IconData icon;
  final Color iconColor;
  final bool chart;
  const _StatCard({
    required this.label,
    required this.value,
    this.sub,
    required this.icon,
    required this.iconColor,
    this.chart = false,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 10,
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                label,
                style: TextStyle(
                  fontSize: 11,
                  color: Colors.grey.shade600,
                  fontWeight: FontWeight.bold,
                ),
              ),
              Icon(icon, color: iconColor, size: 20),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            value,
            style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w900),
          ),
          if (sub != null) ...[
            const SizedBox(height: 4),
            Text(
              sub!,
              style: TextStyle(fontSize: 11, color: Colors.green.shade600),
            ),
          ],
          if (chart) ...[
            const SizedBox(height: 8),
            Row(
              children: List.generate(6, (i) {
                final heights = [0.4, 0.6, 0.45, 0.75, 0.9, 0.65];
                return Expanded(
                  child: Container(
                    margin: const EdgeInsets.symmetric(horizontal: 1),
                    height: 24 * heights[i],
                    decoration: BoxDecoration(
                      color: Colors.grey.shade200,
                      borderRadius: BorderRadius.circular(4),
                    ),
                  ),
                );
              }),
            ),
          ],
        ],
      ),
    );
  }
}

class _ActivityItem extends StatelessWidget {
  final IconData icon;
  final Color iconBg, iconColor;
  final String title, sub, time;
  final bool isLast;
  const _ActivityItem({
    required this.icon,
    required this.iconBg,
    required this.iconColor,
    required this.title,
    required this.sub,
    required this.time,
    this.isLast = false,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(bottom: isLast ? 0 : 20),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 36,
            height: 36,
            decoration: BoxDecoration(color: iconBg, shape: BoxShape.circle),
            child: Icon(icon, color: iconColor, size: 18),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 13,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  sub,
                  style: TextStyle(fontSize: 12, color: Colors.grey.shade600),
                ),
                const SizedBox(height: 4),
                Text(
                  time,
                  style: TextStyle(
                    fontSize: 10,
                    color: Colors.grey.shade400,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── Tab: Approvals ────────────────────────────────────────────────────────────

class _ApprovalsTab extends StatefulWidget {
  const _ApprovalsTab();
  @override
  State<_ApprovalsTab> createState() => _ApprovalsTabState();
}

class _ApprovalsTabState extends State<_ApprovalsTab> {
  final _pending = [
    {
      'title': 'Khám phá Hang Sơn Đoòng',
      'location': 'Quảng Bình',
      'guide': 'Trần Văn B',
      'image':
          'https://images.unsplash.com/photo-1528127269322-539801943592?w=400',
    },
    {
      'title': 'Trekking Fansipan',
      'location': 'Lào Cai',
      'guide': 'Lê Thị C',
      'image':
          'https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=400',
    },
    {
      'title': 'Phố đêm Sài Gòn',
      'location': 'TP.HCM',
      'guide': 'Phạm Văn D',
      'image':
          'https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?w=400',
    },
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFFFF8F7),
      appBar: AppBar(
        title: Row(
          children: [
            const Text(
              'Chờ duyệt',
              style: TextStyle(fontWeight: FontWeight.bold),
            ),
            const SizedBox(width: 8),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
              decoration: BoxDecoration(
                color: const Color(0xFFFFE4E6),
                borderRadius: BorderRadius.circular(20),
              ),
              child: Text(
                '${_pending.length} mới',
                style: const TextStyle(
                  fontSize: 12,
                  color: Color(0xFFE91E8C),
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ],
        ),
        backgroundColor: Colors.white,
        foregroundColor: Colors.black,
        elevation: 0,
      ),
      body: _pending.isEmpty
          ? Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(
                    Icons.check_circle_outline,
                    size: 64,
                    color: Colors.grey.shade300,
                  ),
                  const SizedBox(height: 16),
                  const Text(
                    'Không có tour nào chờ duyệt',
                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                  ),
                ],
              ),
            )
          : ListView.separated(
              padding: const EdgeInsets.all(16),
              itemCount: _pending.length,
              separatorBuilder: (_, _) => const SizedBox(height: 12),
              itemBuilder: (_, i) {
                final item = _pending[i];
                return Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(20),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withValues(alpha: 0.04),
                        blurRadius: 10,
                      ),
                    ],
                  ),
                  child: Row(
                    children: [
                      ClipRRect(
                        borderRadius: BorderRadius.circular(12),
                        child: Image.network(
                          item['image']!,
                          width: 80,
                          height: 80,
                          fit: BoxFit.cover,
                          errorBuilder: (_, _, _) => Container(
                            width: 80,
                            height: 80,
                            color: Colors.grey.shade200,
                          ),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              item['title']!,
                              style: const TextStyle(
                                fontWeight: FontWeight.bold,
                                fontSize: 14,
                              ),
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                            const SizedBox(height: 2),
                            Text(
                              '${item['location']} • ${item['guide']}',
                              style: TextStyle(
                                fontSize: 12,
                                color: Colors.grey.shade600,
                              ),
                            ),
                            const SizedBox(height: 10),
                            Row(
                              children: [
                                Expanded(
                                  child: ElevatedButton(
                                    onPressed: () =>
                                        setState(() => _pending.removeAt(i)),
                                    style: ElevatedButton.styleFrom(
                                      backgroundColor: const Color(0xFFE91E8C),
                                      foregroundColor: Colors.white,
                                      padding: const EdgeInsets.symmetric(
                                        vertical: 8,
                                      ),
                                      shape: RoundedRectangleBorder(
                                        borderRadius: BorderRadius.circular(10),
                                      ),
                                      elevation: 0,
                                    ),
                                    child: const Text(
                                      'Duyệt',
                                      style: TextStyle(fontSize: 12),
                                    ),
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: OutlinedButton(
                                    onPressed: () =>
                                        setState(() => _pending.removeAt(i)),
                                    style: OutlinedButton.styleFrom(
                                      padding: const EdgeInsets.symmetric(
                                        vertical: 8,
                                      ),
                                      side: BorderSide(
                                        color: Colors.grey.shade300,
                                      ),
                                      shape: RoundedRectangleBorder(
                                        borderRadius: BorderRadius.circular(10),
                                      ),
                                    ),
                                    child: const Text(
                                      'Từ chối',
                                      style: TextStyle(fontSize: 12),
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
              },
            ),
    );
  }
}

// ── Tab: Reports ──────────────────────────────────────────────────────────────

class _ReportsTab extends StatelessWidget {
  const _ReportsTab();

  static const _reports = [
    {
      'type': 'UI Bug',
      'desc': 'Nút đặt tour bị ẩn trên màn hình nhỏ',
      'user': 'user_123',
      'time': '10 phút trước',
      'severity': 'high',
    },
    {
      'type': 'Thanh toán',
      'desc': 'Không nhận được xác nhận sau khi thanh toán',
      'user': 'user_456',
      'time': '1 giờ trước',
      'severity': 'high',
    },
    {
      'type': 'Nội dung',
      'desc': 'Ảnh tour không hiển thị đúng',
      'user': 'user_789',
      'time': '3 giờ trước',
      'severity': 'medium',
    },
    {
      'type': 'Tài khoản',
      'desc': 'Không thể đổi mật khẩu',
      'user': 'user_321',
      'time': 'Hôm qua',
      'severity': 'low',
    },
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFFFF8F7),
      appBar: AppBar(
        title: const Text(
          'Báo cáo lỗi',
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
        backgroundColor: Colors.white,
        foregroundColor: Colors.black,
        elevation: 0,
      ),
      body: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: _reports.length,
        separatorBuilder: (_, _) => const SizedBox(height: 10),
        itemBuilder: (_, i) {
          final r = _reports[i];
          final isHigh = r['severity'] == 'high';
          final isMed = r['severity'] == 'medium';
          final color = isHigh
              ? Colors.red
              : isMed
              ? Colors.orange
              : Colors.blue;
          return Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(16),
              border: Border(left: BorderSide(color: color, width: 4)),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.04),
                  blurRadius: 8,
                ),
              ],
            ),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 8,
                              vertical: 3,
                            ),
                            decoration: BoxDecoration(
                              color: color.withValues(alpha: 0.1),
                              borderRadius: BorderRadius.circular(8),
                            ),
                            child: Text(
                              r['type']!,
                              style: TextStyle(
                                fontSize: 11,
                                color: color,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ),
                          const SizedBox(width: 8),
                          Text(
                            r['time']!,
                            style: TextStyle(
                              fontSize: 11,
                              color: Colors.grey.shade500,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 6),
                      Text(
                        r['desc']!,
                        style: const TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        'Từ: ${r['user']}',
                        style: TextStyle(
                          fontSize: 12,
                          color: Colors.grey.shade500,
                        ),
                      ),
                    ],
                  ),
                ),
                IconButton(
                  icon: const Icon(
                    Icons.check_circle_outline,
                    color: Colors.green,
                  ),
                  onPressed: () {},
                  tooltip: 'Đánh dấu đã xử lý',
                ),
              ],
            ),
          );
        },
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
          'Hồ sơ Admin',
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
                        : 'A',
                    style: const TextStyle(
                      fontSize: 36,
                      fontWeight: FontWeight.bold,
                      color: Colors.black54,
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                Text(
                  user?.fullName ?? 'Admin',
                  style: const TextStyle(
                    fontSize: 22,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                Container(
                  margin: const EdgeInsets.only(top: 6),
                  padding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 4,
                  ),
                  decoration: BoxDecoration(
                    color: const Color(0xFFFFE4E6),
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: const Text(
                    'Quản trị viên',
                    style: TextStyle(
                      fontSize: 12,
                      color: Color(0xFFE91E8C),
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 32),
          const Divider(),
          ListTile(
            leading: const Icon(Icons.people_outline),
            title: const Text('Quản lý người dùng'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () {},
          ),
          ListTile(
            leading: const Icon(Icons.bar_chart),
            title: const Text('Báo cáo doanh thu'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () {},
          ),
          ListTile(
            leading: const Icon(Icons.settings_outlined),
            title: const Text('Cài đặt hệ thống'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () {},
          ),
          const Divider(),
          OutlinedButton.icon(
            onPressed: () async {
              await ref.read(authStateProvider.notifier).signOut();
              if (context.mounted) {
                Navigator.of(context).pushAndRemoveUntil(
                  MaterialPageRoute(builder: (_) => const LoginScreen()),
                  (_) => false,
                );
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
