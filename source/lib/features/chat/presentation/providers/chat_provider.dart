import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../data/datasources/chat_datasource.dart';

// ── Chat DataSource Provider ──────────────────────────────────────────────────

final chatDataSourceProvider = Provider((_) => ChatDataSource());

// ── Create Conversation Provider ──────────────────────────────────────────────

class CreateConversationState {
  final bool isLoading;
  final String? conversationId;
  final String? error;

  const CreateConversationState({
    this.isLoading = false,
    this.conversationId,
    this.error,
  });

  CreateConversationState copyWith({
    bool? isLoading,
    String? conversationId,
    String? error,
  }) => CreateConversationState(
    isLoading: isLoading ?? this.isLoading,
    conversationId: conversationId ?? this.conversationId,
    error: error,
  );
}

class CreateConversationNotifier
    extends StateNotifier<CreateConversationState> {
  final ChatDataSource _ds;
  CreateConversationNotifier(this._ds) : super(const CreateConversationState());

  Future<String?> createOrGet({
    required String guideId,
    required String bookingId,
  }) async {
    state = const CreateConversationState(isLoading: true);
    try {
      final conversationId = await _ds.createOrGetConversation(
        guideId: guideId,
        bookingId: bookingId,
      );
      state = CreateConversationState(conversationId: conversationId);
      return conversationId;
    } catch (e) {
      state = CreateConversationState(error: e.toString());
      return null;
    }
  }

  void reset() => state = const CreateConversationState();
}

final createConversationProvider =
    StateNotifierProvider.autoDispose<
      CreateConversationNotifier,
      CreateConversationState
    >((ref) => CreateConversationNotifier(ref.watch(chatDataSourceProvider)));

// ── Conversations List Provider ───────────────────────────────────────────────

class ConversationsState {
  final List<ConversationItem> conversations;
  final bool isLoading;
  final String? error;

  const ConversationsState({
    this.conversations = const [],
    this.isLoading = false,
    this.error,
  });

  ConversationsState copyWith({
    List<ConversationItem>? conversations,
    bool? isLoading,
    String? error,
  }) => ConversationsState(
    conversations: conversations ?? this.conversations,
    isLoading: isLoading ?? this.isLoading,
    error: error,
  );
}

class ConversationsNotifier extends StateNotifier<ConversationsState> {
  final ChatDataSource _ds;
  ConversationsNotifier(this._ds) : super(const ConversationsState());

  Future<void> load() async {
    state = state.copyWith(isLoading: true, error: null);
    try {
      final list = await _ds.getMyConversations();
      state = state.copyWith(conversations: list, isLoading: false);
    } catch (e) {
      state = state.copyWith(isLoading: false, error: e.toString());
    }
  }
}

final conversationsProvider =
    StateNotifierProvider<ConversationsNotifier, ConversationsState>(
      (ref) => ConversationsNotifier(ref.watch(chatDataSourceProvider)),
    );
