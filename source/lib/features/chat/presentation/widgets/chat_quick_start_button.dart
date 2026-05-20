import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/chat_provider.dart';
import '../screens/chat_screen.dart';
import '../screens/conversation_list_screen.dart';

/// Widget để bắt đầu chat nhanh với guide từ bất kỳ đâu trong app
class ChatQuickStartButton extends ConsumerWidget {
  final String guideId;
  final String bookingId;
  final String? buttonText;
  final IconData? icon;
  final Color? color;

  const ChatQuickStartButton({
    super.key,
    required this.guideId,
    required this.bookingId,
    this.buttonText,
    this.icon,
    this.color,
  });

  Future<void> _startChat(BuildContext context, WidgetRef ref) async {
    final conversationId = await ref
        .read(createConversationProvider.notifier)
        .createOrGet(guideId: guideId, bookingId: bookingId);

    if (!context.mounted) return;

    if (conversationId != null) {
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (_) => ChatScreen(
            conversationId: conversationId,
            otherUserName: 'Hướng dẫn viên',
          ),
        ),
      );
    } else {
      // Fallback
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Không thể tạo cuộc trò chuyện')),
      );
      Navigator.push(
        context,
        MaterialPageRoute(builder: (_) => const ConversationListScreen()),
      );
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(createConversationProvider);

    if (buttonText != null) {
      // Text button style
      return TextButton.icon(
        onPressed: state.isLoading ? null : () => _startChat(context, ref),
        icon: state.isLoading
            ? const SizedBox(
                width: 16,
                height: 16,
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            : Icon(icon ?? Icons.chat_bubble_outline, color: color),
        label: Text(buttonText!, style: TextStyle(color: color)),
      );
    } else {
      // Icon button style
      return IconButton(
        onPressed: state.isLoading ? null : () => _startChat(context, ref),
        icon: state.isLoading
            ? const SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            : Icon(icon ?? Icons.chat_bubble_outline, color: color),
      );
    }
  }
}
