import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:supabase_flutter/supabase_flutter.dart';
import '../../../../core/config/supabase_config.dart';
import '../../../../core/services/token_storage.dart';
import '../../../../core/config/api_config.dart';
import 'package:dio/dio.dart';

class ChatScreen extends ConsumerStatefulWidget {
  final String conversationId;
  final String otherUserName;
  const ChatScreen({
    super.key,
    required this.conversationId,
    required this.otherUserName,
  });

  @override
  ConsumerState<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends ConsumerState<ChatScreen> {
  final _ctrl = TextEditingController();
  final _scroll = ScrollController();
  final List<_Msg> _messages = [];
  bool _loading = true;
  late final RealtimeChannel _channel;
  late final String _myId;

  @override
  void initState() {
    super.initState();
    _myId = SupabaseConfig.client.auth.currentUser?.id ?? '';
    _loadMessages();
    _subscribeRealtime();
  }

  @override
  void dispose() {
    _ctrl.dispose();
    _scroll.dispose();
    SupabaseConfig.client.removeChannel(_channel);
    super.dispose();
  }

  Future<void> _loadMessages() async {
    try {
      final token = await TokenStorage.getAccessToken();
      final dio = Dio(BaseOptions(baseUrl: ApiConfig.baseUrl));
      final res = await dio.get(
        '/api/chat/conversations/${widget.conversationId}/messages',
        options: Options(headers: {'Authorization': 'Bearer $token'}),
      );
      final list = (res.data['messages'] as List)
          .map(
            (m) => _Msg(
              id: m['id'],
              senderId: m['senderId'],
              content: m['content'],
              createdAt: DateTime.parse(m['createdAt']),
            ),
          )
          .toList();
      setState(() {
        _messages.addAll(list);
        _loading = false;
      });
      _scrollToBottom();
    } catch (_) {
      setState(() => _loading = false);
    }
  }

  void _subscribeRealtime() {
    _channel = SupabaseConfig.client
        .channel('messages:${widget.conversationId}')
        .onPostgresChanges(
          event: PostgresChangeEvent.insert,
          schema: 'public',
          table: 'messages',
          filter: PostgresChangeFilter(
            type: PostgresChangeFilterType.eq,
            column: 'conversation_id',
            value: widget.conversationId,
          ),
          callback: (payload) {
            final row = payload.newRecord;
            final msg = _Msg(
              id: row['id'],
              senderId: row['sender_id'],
              content: row['content'],
              createdAt: DateTime.parse(row['created_at']),
            );
            setState(() => _messages.add(msg));
            _scrollToBottom();
          },
        )
        .subscribe();
  }

  Future<void> _send() async {
    final text = _ctrl.text.trim();
    if (text.isEmpty) return;
    _ctrl.clear();

    try {
      final token = await TokenStorage.getAccessToken();
      final dio = Dio(BaseOptions(baseUrl: ApiConfig.baseUrl));
      await dio.post(
        '/api/chat/conversations/${widget.conversationId}/messages',
        data: {'content': text},
        options: Options(headers: {'Authorization': 'Bearer $token'}),
      );
    } catch (_) {}
  }

  void _scrollToBottom() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scroll.hasClients) {
        _scroll.animateTo(
          _scroll.position.maxScrollExtent,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOut,
        );
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        title: Row(
          children: [
            CircleAvatar(
              radius: 18,
              backgroundColor: const Color(0xFFFFE4E6),
              child: Text(
                widget.otherUserName.isNotEmpty
                    ? widget.otherUserName[0].toUpperCase()
                    : '?',
                style: const TextStyle(
                  color: Color(0xFFE91E8C),
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
            const SizedBox(width: 10),
            Text(
              widget.otherUserName,
              style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
            ),
          ],
        ),
        backgroundColor: Colors.white,
        foregroundColor: Colors.black,
        elevation: 0,
      ),
      body: Column(
        children: [
          Expanded(
            child: _loading
                ? const Center(child: CircularProgressIndicator())
                : _messages.isEmpty
                ? Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(
                          Icons.chat_bubble_outline,
                          size: 48,
                          color: Colors.grey.shade300,
                        ),
                        const SizedBox(height: 12),
                        const Text(
                          'Bắt đầu cuộc trò chuyện!',
                          style: TextStyle(color: Colors.grey),
                        ),
                      ],
                    ),
                  )
                : ListView.builder(
                    controller: _scroll,
                    padding: const EdgeInsets.all(16),
                    itemCount: _messages.length,
                    itemBuilder: (_, i) {
                      final msg = _messages[i];
                      final isMe = msg.senderId == _myId;
                      return _BubbleWidget(msg: msg, isMe: isMe);
                    },
                  ),
          ),

          // Input bar
          Container(
            padding: const EdgeInsets.fromLTRB(12, 8, 12, 16),
            decoration: BoxDecoration(
              color: Colors.white,
              border: Border(top: BorderSide(color: Colors.grey.shade200)),
            ),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _ctrl,
                    decoration: InputDecoration(
                      hintText: 'Nhập tin nhắn...',
                      filled: true,
                      fillColor: Colors.grey.shade100,
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(24),
                        borderSide: BorderSide.none,
                      ),
                      contentPadding: const EdgeInsets.symmetric(
                        horizontal: 16,
                        vertical: 10,
                      ),
                    ),
                    onSubmitted: (_) => _send(),
                  ),
                ),
                const SizedBox(width: 8),
                InkWell(
                  onTap: _send,
                  borderRadius: BorderRadius.circular(24),
                  child: Container(
                    width: 44,
                    height: 44,
                    decoration: const BoxDecoration(
                      color: Color(0xFFE91E8C),
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(
                      Icons.send,
                      color: Colors.white,
                      size: 20,
                    ),
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

class _Msg {
  final String id, senderId, content;
  final DateTime createdAt;
  const _Msg({
    required this.id,
    required this.senderId,
    required this.content,
    required this.createdAt,
  });
}

class _BubbleWidget extends StatelessWidget {
  final _Msg msg;
  final bool isMe;
  const _BubbleWidget({required this.msg, required this.isMe});

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: isMe ? Alignment.centerRight : Alignment.centerLeft,
      child: Container(
        margin: const EdgeInsets.only(bottom: 8),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
        constraints: BoxConstraints(
          maxWidth: MediaQuery.of(context).size.width * 0.72,
        ),
        decoration: BoxDecoration(
          color: isMe ? const Color(0xFFE91E8C) : Colors.grey.shade100,
          borderRadius: BorderRadius.only(
            topLeft: const Radius.circular(16),
            topRight: const Radius.circular(16),
            bottomLeft: Radius.circular(isMe ? 16 : 4),
            bottomRight: Radius.circular(isMe ? 4 : 16),
          ),
        ),
        child: Text(
          msg.content,
          style: TextStyle(
            fontSize: 14,
            color: isMe ? Colors.white : Colors.black87,
          ),
        ),
      ),
    );
  }
}
