import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/presentation/providers/auth_state_provider.dart';

class GuideDashboardScreen extends ConsumerWidget {
  const GuideDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authStateProvider);
    final user = authState.user;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Guide Dashboard'),
        elevation: 0,
        actions: [
          IconButton(
            icon: const Icon(Icons.notifications_outlined),
            onPressed: () {},
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Create Tour (Coming soon)')),
          );
        },
        icon: const Icon(Icons.add),
        label: const Text('Create Tour'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Welcome Banner
            _buildWelcomeBanner(user?.fullName ?? 'Guide'),
            const SizedBox(height: 24),

            // Stats Grid
            _buildStatsGrid(),
            const SizedBox(height: 32),

            // Earnings Overview
            _buildEarningsCard(),
            const SizedBox(height: 32),

            // Upcoming Bookings
            _buildUpcomingBookings(context),
            const SizedBox(height: 32),

            // Your Tours Performance
            _buildToursPerformance(),
            const SizedBox(height: 32),

            // Recent Reviews
            _buildRecentReviews(),
            const SizedBox(height: 80),
          ],
        ),
      ),
    );
  }

  Widget _buildWelcomeBanner(String name) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Welcome, $name! 🎯',
          style: const TextStyle(
            fontSize: 28,
            fontWeight: FontWeight.bold,
            height: 1.2,
          ),
        ),
        const SizedBox(height: 8),
        Text(
          'You have 5 upcoming bookings this week.',
          style: TextStyle(fontSize: 16, color: Colors.grey[600]),
        ),
      ],
    );
  }

  Widget _buildStatsGrid() {
    return GridView.count(
      crossAxisCount: 2,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      mainAxisSpacing: 12,
      crossAxisSpacing: 12,
      childAspectRatio: 1.5,
      children: [
        _buildStatCard('5', 'Active Tours', Icons.tour, Colors.blue),
        _buildStatCard('23', 'Total Bookings', Icons.book, Colors.green),
        _buildStatCard('4.8', 'Avg Rating', Icons.star, Colors.amber),
        _buildStatCard('₫15M', 'This Month', Icons.attach_money, Colors.purple),
      ],
    );
  }

  Widget _buildStatCard(
    String value,
    String label,
    IconData icon,
    Color color,
  ) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, size: 32, color: color),
          const SizedBox(height: 8),
          Text(
            value,
            style: TextStyle(
              fontSize: 28,
              fontWeight: FontWeight.bold,
              color: color,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            label,
            style: TextStyle(
              fontSize: 11,
              color: Colors.grey[600],
              fontWeight: FontWeight.w600,
            ),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }

  Widget _buildEarningsCard() {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [Colors.blue[700]!, Colors.blue[500]!],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.blue.withValues(alpha: 0.3),
            blurRadius: 20,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'EARNINGS OVERVIEW',
            style: TextStyle(
              fontSize: 10,
              fontWeight: FontWeight.bold,
              color: Colors.white70,
              letterSpacing: 2,
            ),
          ),
          const SizedBox(height: 16),
          const Text(
            '₫15,450,000',
            style: TextStyle(
              fontSize: 36,
              fontWeight: FontWeight.bold,
              color: Colors.white,
            ),
          ),
          const SizedBox(height: 4),
          const Text(
            'Total earnings this month',
            style: TextStyle(fontSize: 14, color: Colors.white70),
          ),
          const SizedBox(height: 20),
          Row(
            children: [
              Expanded(child: _buildEarningsStat('₫8.2M', 'Confirmed')),
              const SizedBox(width: 16),
              Expanded(child: _buildEarningsStat('₫7.2M', 'Pending')),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildEarningsStat(String value, String label) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.2),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        children: [
          Text(
            value,
            style: const TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.bold,
              color: Colors.white,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            label,
            style: const TextStyle(fontSize: 12, color: Colors.white70),
          ),
        ],
      ),
    );
  }

  Widget _buildUpcomingBookings(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            const Text(
              'Upcoming Bookings',
              style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
            ),
            TextButton(onPressed: () {}, child: const Text('View All')),
          ],
        ),
        const SizedBox(height: 16),
        _buildBookingCard(
          'Hanoi Street Food Tour',
          'March 15, 2025',
          '4 guests',
          '₫2,000,000',
          Colors.green,
          'Confirmed',
        ),
        const SizedBox(height: 12),
        _buildBookingCard(
          'Ha Long Bay Adventure',
          'March 18, 2025',
          '6 guests',
          '₫9,000,000',
          Colors.orange,
          'Pending',
        ),
        const SizedBox(height: 12),
        _buildBookingCard(
          'Sapa Trekking Experience',
          'March 22, 2025',
          '8 guests',
          '₫4,800,000',
          Colors.green,
          'Confirmed',
        ),
      ],
    );
  }

  Widget _buildBookingCard(
    String title,
    String date,
    String guests,
    String price,
    Color statusColor,
    String status,
  ) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.grey[200]!),
      ),
      child: Row(
        children: [
          Container(
            width: 4,
            height: 60,
            decoration: BoxDecoration(
              color: statusColor,
              borderRadius: BorderRadius.circular(2),
            ),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Icon(
                      Icons.calendar_today,
                      size: 14,
                      color: Colors.grey[600],
                    ),
                    const SizedBox(width: 4),
                    Text(
                      date,
                      style: TextStyle(fontSize: 12, color: Colors.grey[600]),
                    ),
                    const SizedBox(width: 16),
                    Icon(Icons.people, size: 14, color: Colors.grey[600]),
                    const SizedBox(width: 4),
                    Text(
                      guests,
                      style: TextStyle(fontSize: 12, color: Colors.grey[600]),
                    ),
                  ],
                ),
              ],
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: statusColor.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Text(
                  status,
                  style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.bold,
                    color: statusColor,
                  ),
                ),
              ),
              const SizedBox(height: 8),
              Text(
                price,
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: Colors.blue,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildToursPerformance() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Your Tours Performance',
          style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 16),
        _buildTourPerformanceCard(
          'Hanoi Street Food Tour',
          '4.9',
          '45',
          '₫22.5M',
          85,
        ),
        const SizedBox(height: 12),
        _buildTourPerformanceCard(
          'Ha Long Bay Adventure',
          '5.0',
          '32',
          '₫48M',
          95,
        ),
      ],
    );
  }

  Widget _buildTourPerformanceCard(
    String title,
    String rating,
    String bookings,
    String revenue,
    int bookingRate,
  ) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              _buildMetric(Icons.star, rating, 'Rating'),
              const SizedBox(width: 24),
              _buildMetric(Icons.book, bookings, 'Bookings'),
              const SizedBox(width: 24),
              _buildMetric(Icons.attach_money, revenue, 'Revenue'),
            ],
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: LinearProgressIndicator(
                  value: bookingRate / 100,
                  backgroundColor: Colors.grey[200],
                  valueColor: AlwaysStoppedAnimation<Color>(
                    bookingRate > 80 ? Colors.green : Colors.orange,
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Text(
                '$bookingRate% booked',
                style: const TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildMetric(IconData icon, String value, String label) {
    return Row(
      children: [
        Icon(icon, size: 16, color: Colors.grey[600]),
        const SizedBox(width: 4),
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              value,
              style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold),
            ),
            Text(
              label,
              style: TextStyle(fontSize: 10, color: Colors.grey[600]),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildRecentReviews() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Recent Reviews',
          style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 16),
        _buildReviewCard(
          'Emma Wilson',
          5,
          'Amazing experience! The guide was knowledgeable and friendly.',
          '2 days ago',
        ),
        const SizedBox(height: 12),
        _buildReviewCard(
          'John Smith',
          5,
          'Best tour in Hanoi! Highly recommended.',
          '5 days ago',
        ),
      ],
    );
  }

  Widget _buildReviewCard(
    String name,
    int rating,
    String comment,
    String time,
  ) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.grey[200]!),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 20,
                backgroundColor: Colors.grey[300],
                child: Text(
                  name[0],
                  style: const TextStyle(fontWeight: FontWeight.bold),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      name,
                      style: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    Row(
                      children: List.generate(
                        5,
                        (index) => Icon(
                          index < rating ? Icons.star : Icons.star_border,
                          size: 14,
                          color: Colors.amber,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              Text(
                time,
                style: TextStyle(fontSize: 12, color: Colors.grey[600]),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            comment,
            style: TextStyle(fontSize: 14, color: Colors.grey[700]),
          ),
        ],
      ),
    );
  }
}
