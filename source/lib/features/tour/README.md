# Tour Management Feature

## 📋 Tổng quan

Feature quản lý tours trong TripMate app, cho phép:
- Xem danh sách tours
- Tìm kiếm tours theo tên hoặc địa điểm
- Xem chi tiết tour
- Tạo/sửa/xóa tour (dành cho guide)

## 🏗️ Kiến trúc

Feature được xây dựng theo Clean Architecture với 3 layers:

```
tour/
├── domain/              # Business logic
│   ├── entities/        # Tour entity
│   ├── repositories/    # Repository interface
│   └── usecases/        # Use cases
├── data/                # Data layer
│   ├── models/          # Tour model
│   ├── datasources/     # Remote data source
│   └── repositories/    # Repository implementation
└── presentation/        # UI layer
    ├── screens/         # Tour screens
    ├── widgets/         # Tour widgets
    └── providers/       # State management
```

## 📦 Components

### Domain Layer

#### Entities
- `TourEntity`: Core tour entity với các thuộc tính:
  - id, guideId, title, description
  - location, price, durationHours
  - maxParticipants, images
  - rating, totalReviews, status

#### Use Cases
- `GetToursUseCase`: Lấy danh sách tất cả tours
- `GetTourByIdUseCase`: Lấy chi tiết tour theo ID
- `CreateTourUseCase`: Tạo tour mới (guide only)
- `SearchToursUseCase`: Tìm kiếm tours

### Data Layer

#### Models
- `TourModel`: Data model với JSON serialization

#### Data Sources
- `TourRemoteDataSource`: Giao tiếp với Supabase
  - getTours()
  - getTourById()
  - getToursByGuide()
  - searchTours()
  - createTour()
  - updateTour()
  - deleteTour()

### Presentation Layer

#### Screens
- `TourListScreen`: Màn hình danh sách tours với search

#### Widgets
- `TourCard`: Card hiển thị thông tin tour

#### Providers
- `tourListProvider`: State management cho danh sách tours

## 🚀 Sử dụng

### 1. Hiển thị danh sách tours

```dart
import 'package:flutter_tripmate_application/features/tour/presentation/screens/tour_list_screen.dart';

// Navigate to tour list
Navigator.push(
  context,
  MaterialPageRoute(builder: (context) => const TourListScreen()),
);
```

### 2. Tìm kiếm tours

```dart
// Search được tích hợp sẵn trong TourListScreen
// User nhập từ khóa và nhấn Enter
```

### 3. Tạo tour mới (Guide)

```dart
final result = await ref.read(createTourUseCaseProvider)(
  title: 'Khám phá Hà Nội',
  description: 'Tour tham quan phố cổ',
  location: 'Hà Nội',
  price: 500000,
  durationHours: 4,
  maxParticipants: 15,
  images: ['https://example.com/image.jpg'],
);

result.fold(
  (failure) => print('Error: ${failure.message}'),
  (tour) => print('Created: ${tour.title}'),
);
```

## 🔌 API Integration

### Supabase Table: `tours`

```sql
CREATE TABLE tours (
  id UUID PRIMARY KEY,
  guide_id UUID REFERENCES profiles(id),
  title TEXT NOT NULL,
  description TEXT,
  location TEXT NOT NULL,
  price DECIMAL(10, 2) NOT NULL,
  duration_hours INTEGER NOT NULL,
  max_participants INTEGER DEFAULT 10,
  images TEXT[],
  rating DECIMAL(3, 2) DEFAULT 0,
  total_reviews INTEGER DEFAULT 0,
  status TEXT DEFAULT 'active',
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

### Row Level Security (RLS)

- Tất cả user có thể xem tours active
- Chỉ guide có thể tạo/sửa/xóa tours của mình
- Admin có thể quản lý tất cả tours

## 📱 Screens

### Tour List Screen
- Hiển thị danh sách tours dạng card
- Search bar ở trên cùng
- Pull to refresh
- Loading và error states
- Empty state khi không có tours

### Tour Card
- Hình ảnh tour (hoặc placeholder)
- Tiêu đề và địa điểm
- Thời gian và số người tham gia
- Rating và số reviews
- Giá tour

## 🎨 UI Features

- Material Design 3
- Responsive layout
- Loading indicators
- Error handling với retry
- Empty states
- Pull to refresh
- Search functionality

## 🔄 State Management

Sử dụng Riverpod với StateNotifier:

```dart
// Load tours
ref.read(tourListProvider.notifier).loadTours();

// Search tours
ref.read(tourListProvider.notifier).searchTours('Hà Nội');

// Refresh
ref.read(tourListProvider.notifier).refresh();

// Watch state
final tourState = ref.watch(tourListProvider);
```

## 🧪 Testing

### Unit Tests
```dart
// Test use cases
test('GetToursUseCase returns list of tours', () async {
  // Arrange
  final mockRepo = MockTourRepository();
  final useCase = GetToursUseCase(mockRepo);
  
  // Act
  final result = await useCase();
  
  // Assert
  expect(result.isRight(), true);
});
```

### Widget Tests
```dart
testWidgets('TourListScreen displays tours', (tester) async {
  await tester.pumpWidget(
    ProviderScope(child: MaterialApp(home: TourListScreen())),
  );
  
  expect(find.byType(TourCard), findsWidgets);
});
```

## 📝 TODO

- [ ] Tour detail screen
- [ ] Create/Edit tour screen (for guides)
- [ ] Image upload functionality
- [ ] Filter tours by price, duration, rating
- [ ] Sort tours (newest, price, rating)
- [ ] Favorite tours
- [ ] Share tour
- [ ] Map view for tours

## 🐛 Known Issues

- Image loading chưa có caching
- Search chưa có debounce
- Chưa có pagination cho danh sách dài

## 📚 Dependencies

- `flutter_riverpod`: State management
- `dartz`: Functional programming (Either)
- `equatable`: Value equality
- `intl`: Number formatting (currency)
- `supabase_flutter`: Backend integration

## 🔗 Related Features

- Authentication (required for creating tours)
- Booking (coming next)
- Reviews (coming next)
- User Profile (for guide info)
