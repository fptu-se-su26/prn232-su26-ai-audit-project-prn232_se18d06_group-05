import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../data/datasources/booking_datasource.dart';
import '../../domain/entities/booking_entity.dart';

// ── My Bookings ───────────────────────────────────────────────────────────────

class MyBookingsState {
  final List<BookingEntity> bookings;
  final bool isLoading;
  final String? error;
  const MyBookingsState({
    this.bookings = const [],
    this.isLoading = false,
    this.error,
  });
  MyBookingsState copyWith({
    List<BookingEntity>? bookings,
    bool? isLoading,
    String? error,
  }) => MyBookingsState(
    bookings: bookings ?? this.bookings,
    isLoading: isLoading ?? this.isLoading,
    error: error,
  );
}

class MyBookingsNotifier extends StateNotifier<MyBookingsState> {
  final BookingDataSource _ds;
  MyBookingsNotifier(this._ds) : super(const MyBookingsState());

  Future<void> load() async {
    state = state.copyWith(isLoading: true, error: null);
    try {
      final list = await _ds.getMyBookings();
      state = state.copyWith(bookings: list, isLoading: false);
    } catch (e) {
      state = state.copyWith(isLoading: false, error: e.toString());
    }
  }

  Future<void> cancel(String bookingId) async {
    try {
      await _ds.cancelBooking(bookingId);
      await load();
    } catch (e) {
      state = state.copyWith(error: e.toString());
    }
  }
}

final bookingDataSourceProvider = Provider((_) => BookingDataSource());

final myBookingsProvider =
    StateNotifierProvider<MyBookingsNotifier, MyBookingsState>((ref) {
      return MyBookingsNotifier(ref.watch(bookingDataSourceProvider));
    });

// ── Create Booking ────────────────────────────────────────────────────────────

class CreateBookingState {
  final bool isLoading;
  final BookingEntity? result;
  final String? error;
  const CreateBookingState({this.isLoading = false, this.result, this.error});
  CreateBookingState copyWith({
    bool? isLoading,
    BookingEntity? result,
    String? error,
  }) => CreateBookingState(
    isLoading: isLoading ?? this.isLoading,
    result: result ?? this.result,
    error: error,
  );
}

class CreateBookingNotifier extends StateNotifier<CreateBookingState> {
  final BookingDataSource _ds;
  CreateBookingNotifier(this._ds) : super(const CreateBookingState());

  Future<bool> create({
    required String tourId,
    required DateTime tourDate,
    required int guests,
    String? note,
  }) async {
    state = const CreateBookingState(isLoading: true);
    try {
      final booking = await _ds.createBooking(
        tourId: tourId,
        tourDate: tourDate,
        guests: guests,
        note: note,
      );
      state = CreateBookingState(result: booking);
      return true;
    } catch (e) {
      state = CreateBookingState(error: e.toString());
      return false;
    }
  }

  void reset() => state = const CreateBookingState();
}

final createBookingProvider =
    StateNotifierProvider.autoDispose<
      CreateBookingNotifier,
      CreateBookingState
    >((ref) => CreateBookingNotifier(ref.watch(bookingDataSourceProvider)));
