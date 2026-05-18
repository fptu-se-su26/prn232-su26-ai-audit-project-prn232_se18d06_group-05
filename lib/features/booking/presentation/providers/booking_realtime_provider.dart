import 'dart:async';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:supabase_flutter/supabase_flutter.dart';
import '../../../../core/config/supabase_config.dart';
import '../../domain/entities/booking_entity.dart';
import '../../data/models/booking_model.dart';

// ── Realtime Booking Status ───────────────────────────────────────────────────

/// Realtime booking status change event
class BookingStatusUpdate {
  final String bookingId;
  final String oldStatus;
  final String newStatus;
  final DateTime updatedAt;

  const BookingStatusUpdate({
    required this.bookingId,
    required this.oldStatus,
    required this.newStatus,
    required this.updatedAt,
  });

  factory BookingStatusUpdate.fromMap(Map<String, dynamic> data) {
    return BookingStatusUpdate(
      bookingId: data['id'] as String,
      oldStatus: data['old_status'] as String? ?? 'unknown',
      newStatus: data['status'] as String,
      updatedAt: DateTime.parse(data['updated_at'] as String),
    );
  }
}

// ── Single Booking Realtime State ─────────────────────────────────────────────

class BookingRealtimeState {
  final BookingEntity? booking;
  final bool isLoading;
  final String? error;
  final bool isRealtimeConnected;

  const BookingRealtimeState({
    this.booking,
    this.isLoading = false,
    this.error,
    this.isRealtimeConnected = false,
  });

  BookingRealtimeState copyWith({
    BookingEntity? booking,
    bool? isLoading,
    String? error,
    bool? isRealtimeConnected,
  }) {
    return BookingRealtimeState(
      booking: booking ?? this.booking,
      isLoading: isLoading ?? this.isLoading,
      error: error,
      isRealtimeConnected: isRealtimeConnected ?? this.isRealtimeConnected,
    );
  }
}

class BookingRealtimeNotifier extends StateNotifier<BookingRealtimeState> {
  RealtimeChannel? _channel;
  StreamSubscription? _subscription;
  final String bookingId;

  BookingRealtimeNotifier(this.bookingId)
      : super(const BookingRealtimeState(isLoading: true));

  void connect() {
    if (!SupabaseConfig.isInitialized) {
      state = state.copyWith(
        isLoading: false,
        error: 'Supabase not initialized',
      );
      return;
    }

    state = state.copyWith(isLoading: true, error: null);

    try {
      // Subscribe to realtime changes for this booking
      _channel = SupabaseConfig.client.channel('booking:$bookingId');

      _channel!.onPostgresChanges(
        event: '*',
        schema: 'public',
        table: 'bookings',
        filter: 'id=eq.$bookingId',
        callback: (payload) {
          _handleRealtimeUpdate(payload);
        },
      ).subscribe(
        (status, [error]) {
          if (status == RealtimeSubscribeStatus.subscribed) {
            state = state.copyWith(isRealtimeConnected: true);
            // Fetch initial data once subscribed
            _fetchBooking();
          } else if (status == RealtimeSubscribeStatus.channelError) {
            state = state.copyWith(
              isRealtimeConnected: false,
              error: error?.message ?? 'Realtime connection error',
            );
          }
        },
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        isRealtimeConnected: false,
        error: 'Failed to connect realtime: ${e.toString()}',
      );
    }
  }

  void _handleRealtimeUpdate(PostgresChangesPayload payload) {
    if (payload.eventType == PostgresChangeEventType.update) {
      try {
        final newRecord = payload.newRecord;
        if (newRecord != null) {
          final updatedBooking = BookingModel.fromJson(newRecord);
          state = state.copyWith(
            booking: updatedBooking,
            isLoading: false,
          );

          // Notify listeners about the status change
          final oldStatus = payload.oldRecord?['status'] as String? ?? 'unknown';
          final newStatus = newRecord['status'] as String;
          if (oldStatus != newStatus) {
            // Could emit event or callback here if needed
            debugPrint(
              '[BookingRealtime] Booking $bookingId status changed: $oldStatus → $newStatus',
            );
          }
        }
      } catch (e) {
        state = state.copyWith(error: 'Failed to parse booking update: ${e.toString()}');
      }
    }
  }

  Future<void> _fetchBooking() async {
    try {
      final response = await SupabaseConfig.client
          .from('bookings')
          .select()
          .eq('id', bookingId)
          .single();

      final booking = BookingModel.fromJson(response);
      state = state.copyWith(booking: booking, isLoading: false);
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: 'Failed to fetch booking: ${e.toString()}',
      );
    }
  }

  void refresh() {
    _fetchBooking();
  }

  @override
  void dispose() {
    _subscription?.cancel();
    _channel?.unsubscribe();
    super.dispose();
  }
}

/// Provider for single booking realtime updates
final bookingRealtimeProvider =
    StateNotifierProvider.autoDispose<BookingRealtimeNotifier, BookingRealtimeState>(
  (ref, String bookingId) => BookingRealtimeNotifier(bookingId),
);

// ── My Bookings Realtime List State ───────────────────────────────────────────

class MyBookingsRealtimeState {
  final List<BookingEntity> bookings;
  final bool isLoading;
  final String? error;
  final bool isRealtimeConnected;
  final Set<String> pendingUpdates; // Booking IDs with pending updates

  const MyBookingsRealtimeState({
    this.bookings = const [],
    this.isLoading = false,
    this.error,
    this.isRealtimeConnected = false,
    this.pendingUpdates = const {},
  });

  MyBookingsRealtimeState copyWith({
    List<BookingEntity>? bookings,
    bool? isLoading,
    String? error,
    bool? isRealtimeConnected,
    Set<String>? pendingUpdates,
  }) {
    return MyBookingsRealtimeState(
      bookings: bookings ?? this.bookings,
      isLoading: isLoading ?? this.isLoading,
      error: error,
      isRealtimeConnected: isRealtimeConnected ?? this.isRealtimeConnected,
      pendingUpdates: pendingUpdates ?? this.pendingUpdates,
    );
  }

  MyBookingsRealtimeState addPendingUpdate(String bookingId) {
    return copyWith(pendingUpdates: {...pendingUpdates, bookingId});
  }

  MyBookingsRealtimeState clearPendingUpdate(String bookingId) {
    return copyWith(pendingUpdates: {...pendingUpdates}..remove(bookingId));
  }
}

class MyBookingsRealtimeNotifier extends StateNotifier<MyBookingsRealtimeState> {
  RealtimeChannel? _channel;
  final String travelerId;

  MyBookingsRealtimeNotifier(this.travelerId)
      : super(const MyBookingsRealtimeState(isLoading: true));

  void connect() {
    if (!SupabaseConfig.isInitialized) {
      state = state.copyWith(
        isLoading: false,
        error: 'Supabase not initialized',
      );
      return;
    }

    state = state.copyWith(isLoading: true, error: null);

    try {
      // Subscribe to realtime changes for all bookings of this traveler
      _channel = SupabaseConfig.client.channel('bookings:travelerId=eq.$travelerId');

      _channel!.onPostgresChanges(
        event: '*',
        schema: 'public',
        table: 'bookings',
        filter: 'travelerId=eq.$travelerId',
        callback: (payload) {
          _handleRealtimeUpdate(payload);
        },
      ).subscribe(
        (status, [error]) {
          if (status == RealtimeSubscribeStatus.subscribed) {
            state = state.copyWith(isRealtimeConnected: true);
            _fetchBookings();
          } else if (status == RealtimeSubscribeStatus.channelError) {
            state = state.copyWith(
              isRealtimeConnected: false,
              error: error?.message ?? 'Realtime connection error',
            );
          }
        },
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        isRealtimeConnected: false,
        error: 'Failed to connect realtime: ${e.toString()}',
      );
    }
  }

  void _handleRealtimeUpdate(PostgresChangesPayload payload) {
    switch (payload.eventType) {
      case PostgresChangeEventType.insert:
        _handleInsert(payload.newRecord);
        break;
      case PostgresChangeEventType.update:
        _handleUpdate(payload.newRecord, payload.oldRecord);
        break;
      case PostgresChangeEventType.delete:
        _handleDelete(payload.oldRecord);
        break;
    }
  }

  void _handleInsert(Map<String, dynamic>? newRecord) {
    if (newRecord == null) return;
    try {
      final newBooking = BookingModel.fromJson(newRecord);
      state = state.copyWith(
        bookings: [...state.bookings, newBooking],
      );
      debugPrint('[BookingRealtime] New booking added: ${newBooking.id}');
    } catch (e) {
      state = state.copyWith(error: 'Failed to process new booking: ${e.toString()}');
    }
  }

  void _handleUpdate(Map<String, dynamic>? newRecord, Map<String, dynamic>? oldRecord) {
    if (newRecord == null) return;
    try {
      final updatedBooking = BookingModel.fromJson(newRecord);
      final bookingId = updatedBooking.id;

      // Mark as pending update, then refresh
      state = state.addPendingUpdate(bookingId);

      final index = state.bookings.indexWhere((b) => b.id == bookingId);
      if (index != -1) {
        final updatedList = List<BookingEntity>.from(state.bookings);
        updatedList[index] = updatedBooking;
        state = state.copyWith(bookings: updatedList);
        state = state.clearPendingUpdate(bookingId);
      } else {
        // New booking not in current list
        state = state.copyWith(bookings: [...state.bookings, updatedBooking]);
        state = state.clearPendingUpdate(bookingId);
      }

      // Log status change
      final oldStatus = oldRecord?['status'] as String?;
      final newStatus = newRecord['status'] as String;
      if (oldStatus != null && oldStatus != newStatus) {
        debugPrint(
          '[BookingRealtime] Booking status changed: $bookingId ($oldStatus → $newStatus)',
        );
      }
    } catch (e) {
      state = state.copyWith(error: 'Failed to process booking update: ${e.toString()}');
    }
  }

  void _handleDelete(Map<String, dynamic>? oldRecord) {
    if (oldRecord == null) return;
    try {
      final bookingId = oldRecord['id'] as String;
      state = state.copyWith(
        bookings: state.bookings.where((b) => b.id != bookingId).toList(),
      );
      debugPrint('[BookingRealtime] Booking removed: $bookingId');
    } catch (e) {
      state = state.copyWith(error: 'Failed to process booking deletion: ${e.toString()}');
    }
  }

  Future<void> _fetchBookings() async {
    try {
      final response = await SupabaseConfig.client
          .from('bookings')
          .select()
          .eq('travelerId', travelerId)
          .order('created_at', ascending: false);

      final bookings = (response as List)
          .map((e) => BookingModel.fromJson(e as Map<String, dynamic>))
          .toList();

      state = state.copyWith(bookings: bookings, isLoading: false);
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: 'Failed to fetch bookings: ${e.toString()}',
      );
    }
  }

  void refresh() {
    _fetchBookings();
  }

  @override
  void dispose() {
    _channel?.unsubscribe();
    super.dispose();
  }
}

/// Provider for my bookings list with realtime updates
final myBookingsRealtimeProvider =
    StateNotifierProvider.autoDispose<MyBookingsRealtimeNotifier, MyBookingsRealtimeState>(
  (ref, String travelerId) => MyBookingsRealtimeNotifier(travelerId),
);

// ── Realtime Connection Status Provider ───────────────────────────────────────

/// Global realtime connection status
class RealtimeConnectionState {
  final bool isConnected;
  final String? error;
  final DateTime? lastConnectedAt;

  const RealtimeConnectionState({
    this.isConnected = false,
    this.error,
    this.lastConnectedAt,
  });

  RealtimeConnectionState copyWith({
    bool? isConnected,
    String? error,
    DateTime? lastConnectedAt,
  }) {
    return RealtimeConnectionState(
      isConnected: isConnected ?? this.isConnected,
      error: error,
      lastConnectedAt: lastConnectedAt ?? this.lastConnectedAt,
    );
  }
}

class RealtimeConnectionNotifier extends StateNotifier<RealtimeConnectionState> {
  RealtimeConnectionNotifier() : super(const RealtimeConnectionState());

  void setConnected() {
    state = state.copyWith(
      isConnected: true,
      error: null,
      lastConnectedAt: DateTime.now(),
    );
  }

  void setError(String error) {
    state = state.copyWith(isConnected: false, error: error);
  }

  void reset() {
    state = const RealtimeConnectionState();
  }
}

final realtimeConnectionProvider =
    StateNotifierProvider<RealtimeConnectionNotifier, RealtimeConnectionState>(
  (ref) => RealtimeConnectionNotifier(),
);
