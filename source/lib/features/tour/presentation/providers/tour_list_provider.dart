import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../domain/entities/tour_entity.dart';
import '../../domain/usecases/get_tours_usecase.dart';
import '../../domain/usecases/search_tours_usecase.dart';
import 'tour_providers.dart';

/// Tour list state
class TourListState {
  final List<TourEntity> tours;
  final bool isLoading;
  final String? error;

  TourListState({this.tours = const [], this.isLoading = false, this.error});

  TourListState copyWith({
    List<TourEntity>? tours,
    bool? isLoading,
    String? error,
  }) {
    return TourListState(
      tours: tours ?? this.tours,
      isLoading: isLoading ?? this.isLoading,
      error: error,
    );
  }
}

/// Tour list notifier
class TourListNotifier extends StateNotifier<TourListState> {
  final GetToursUseCase getToursUseCase;
  final SearchToursUseCase searchToursUseCase;

  TourListNotifier({
    required this.getToursUseCase,
    required this.searchToursUseCase,
  }) : super(TourListState());

  /// Load all tours
  Future<void> loadTours() async {
    state = state.copyWith(isLoading: true, error: null);

    final result = await getToursUseCase();

    result.fold(
      (failure) {
        state = state.copyWith(isLoading: false, error: failure.message);
      },
      (tours) {
        state = state.copyWith(tours: tours, isLoading: false, error: null);
      },
    );
  }

  /// Search tours
  Future<void> searchTours(String query) async {
    if (query.trim().isEmpty) {
      await loadTours();
      return;
    }

    state = state.copyWith(isLoading: true, error: null);

    final result = await searchToursUseCase(query);

    result.fold(
      (failure) {
        state = state.copyWith(isLoading: false, error: failure.message);
      },
      (tours) {
        state = state.copyWith(tours: tours, isLoading: false, error: null);
      },
    );
  }

  /// Refresh tours
  Future<void> refresh() async {
    await loadTours();
  }
}

/// Tour list provider
final tourListProvider = StateNotifierProvider<TourListNotifier, TourListState>(
  (ref) {
    final getToursUseCase = ref.watch(getToursUseCaseProvider);
    final searchToursUseCase = ref.watch(searchToursUseCaseProvider);

    return TourListNotifier(
      getToursUseCase: getToursUseCase,
      searchToursUseCase: searchToursUseCase,
    );
  },
);
