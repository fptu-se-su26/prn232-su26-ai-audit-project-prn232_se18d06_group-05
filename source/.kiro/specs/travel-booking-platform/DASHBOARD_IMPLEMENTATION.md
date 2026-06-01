# 📊 Dashboard Implementation Guide

## ✅ Đã tạo

### 1. Traveler Dashboard
**File:** `lib/features/dashboard/presentation/screens/traveler_dashboard_screen.dart`

**Features:**
- ✅ Welcome banner với tên user
- ✅ Stats grid: Upcoming trips, Completed trips, Saved tours, Reviews
- ✅ Upcoming trip card với image, guide info, countdown
- ✅ Recently saved tours (horizontal scroll)
- ✅ Personalized suggestions

**Design highlights:**
- Clean card-based layout
- Image-rich content
- Call-to-action buttons
- Horizontal scrolling lists

### 2. Guide Dashboard
**File:** `lib/features/dashboard/presentation/screens/guide_dashboard_screen.dart`

**Features:**
- ✅ Welcome banner
- ✅ Stats grid: Active tours, Bookings, Rating, Earnings
- ✅ Earnings overview card (gradient design)
- ✅ Upcoming bookings list
- ✅ Tours performance metrics
- ✅ Recent reviews
- ✅ FAB để create tour

**Design highlights:**
- Business-focused metrics
- Earnings visualization
- Booking management
- Performance tracking

## 🔄 Cách tích hợp

### Bước 1: Update main.dart để route đến dashboard theo role

```dart
class AuthWrapper extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authStateProvider);

    if (authState.isLoading) {
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }

    if (authState.isAuthenticated) {
      final user = authState.user;
      final role = UserRole.fromString(user?.role ?? 'traveler');
      
      // Route theo role
      switch (role) {
        case UserRole.traveler:
          return const TravelerDashboardScreen();
        case UserRole.guide:
          return const GuideDashboardScreen();
        case UserRole.admin:
          return const AdminDashboardScreen();
      }
    }

    return const LoginScreen();
  }
}
```

### Bước 2: Update drawer menu để navigate giữa các screens

```dart
// Trong drawer
ListTile(
  leading: const Icon(Icons.dashboard),
  title: const Text('Dashboard'),
  onTap: () {
    Navigator.pop(context);
    // Navigate to dashboard based on role
  },
),
ListTile(
  leading: const Icon(Icons.explore),
  title: const Text('Tours'),
  onTap: () {
    Navigator.pop(context);
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => const TourListScreen(),
      ),
    );
  },
),
```

### Bước 3: Thêm bottom navigation (optional)

```dart
BottomNavigationBar(
  currentIndex: _selectedIndex,
  onTap: (index) {
    setState(() => _selectedIndex = index);
  },
  items: const [
    BottomNavigationBarItem(
      icon: Icon(Icons.dashboard),
      label: 'Dashboard',
    ),
    BottomNavigationBarItem(
      icon: Icon(Icons.explore),
      label: 'Tours',
    ),
    BottomNavigationBarItem(
      icon: Icon(Icons.book),
      label: 'Bookings',
    ),
    BottomNavigationBarItem(
      icon: Icon(Icons.person),
      label: 'Profile',
    ),
  ],
)
```

## 🎨 Design System

### Colors
```dart
// Primary
primary: Color(0xFF9C3F11)
primaryContainer: Color(0xFFBC5728)

// Secondary
secondary: Color(0xFF2A6B3C)
secondaryContainer: Color(0xFFADF3B7)

// Surface
surface: Color(0xFFFFF8F4)
surfaceContainer: Color(0xFFF5ECE6)
```

### Typography
```dart
// Headlines
fontFamily: 'Cormorant Garamond'
fontSize: 28-36
fontWeight: bold

// Body
fontFamily: 'Plus Jakarta Sans'
fontSize: 14-16
fontWeight: regular/medium
```

### Spacing
```dart
// Card padding
padding: 16-20

// Section spacing
gap: 24-32

// Grid spacing
gap: 12
```

## 📱 Responsive Design

### Mobile (< 600px)
- Single column layout
- Stack cards vertically
- Full-width components

### Tablet (600-900px)
- 2 column grid for stats
- Wider cards
- More horizontal space

### Desktop (> 900px)
- 4 column grid for stats
- Side-by-side layouts
- Max width container

## 🔌 Data Integration

### Traveler Dashboard

```dart
// Fetch user stats
final upcomingTrips = await getUpcomingTrips(userId);
final completedTrips = await getCompletedTrips(userId);
final savedTours = await getSavedTours(userId);
final reviews = await getUserReviews(userId);

// Fetch upcoming trip
final nextTrip = await getNextTrip(userId);

// Fetch saved tours
final recentlySaved = await getRecentlySavedTours(userId, limit: 5);

// Fetch suggestions
final suggestions = await getPersonalizedSuggestions(userId);
```

### Guide Dashboard

```dart
// Fetch guide stats
final activeTours = await getGuideTours(guideId, status: 'active');
final totalBookings = await getGuideBookings(guideId);
final avgRating = await getGuideRating(guideId);
final earnings = await getGuideEarnings(guideId, month: currentMonth);

// Fetch upcoming bookings
final upcomingBookings = await getUpcomingBookings(guideId);

// Fetch tour performance
final tourPerformance = await getTourPerformance(guideId);

// Fetch recent reviews
final recentReviews = await getGuideReviews(guideId, limit: 5);
```

## 🧪 Testing

### Test Scenarios

**Traveler Dashboard:**
1. Login as traveler → See traveler dashboard
2. Check stats display correctly
3. Click "View Itinerary" → Navigate to trip detail
4. Scroll saved tours → Horizontal scroll works
5. Click "View All" → Navigate to tour list

**Guide Dashboard:**
1. Login as guide → See guide dashboard
2. Check earnings display correctly
3. Click booking → View booking detail
4. Click FAB → Create tour form
5. Check tour performance metrics

**Admin Dashboard:**
1. Login as admin → See admin dashboard
2. Check system stats
3. Manage users
4. View analytics

## 📊 Metrics to Track

### Traveler
- Total trips (upcoming + completed)
- Saved tours count
- Reviews given
- Favorite destinations
- Spending total

### Guide
- Active tours count
- Total bookings
- Average rating
- Monthly earnings
- Response rate
- Cancellation rate

### Admin
- Total users (by role)
- Total tours
- Total bookings
- Platform revenue
- Active guides
- User growth rate

## 🎯 Next Steps

1. **Create Admin Dashboard**
   - System overview
   - User management
   - Tour moderation
   - Analytics charts

2. **Add Real Data**
   - Connect to Supabase
   - Fetch actual stats
   - Real-time updates

3. **Add Interactions**
   - Click handlers
   - Navigation
   - Forms
   - Modals

4. **Add Charts**
   - Earnings chart (guide)
   - Bookings trend
   - User growth (admin)

5. **Add Notifications**
   - New bookings
   - Reviews
   - Messages
   - System alerts

## 🔗 Related Files

- `lib/features/auth/presentation/providers/auth_state_provider.dart`
- `lib/core/enums/user_role.dart`
- `lib/core/services/permission_service.dart`
- `lib/features/tour/presentation/screens/tour_list_screen.dart`

## 💡 Tips

1. **Use StatefulWidget** nếu cần refresh data
2. **Add pull-to-refresh** cho better UX
3. **Cache data** để giảm API calls
4. **Show loading states** khi fetch data
5. **Handle empty states** khi không có data
6. **Add error handling** cho network issues

Dashboard đã sẵn sàng! Chỉ cần tích hợp data thật và navigation. 🎉
