(() => {
    if (window.TripMateChatCommon) return;

    const value = (source, camel, pascal) => source?.[camel] ?? source?.[pascal];
    const currentUserId = () => localStorage.getItem('userId') || '';
    const accessToken = () => localStorage.getItem('accessToken') || '';

    function normalizeMessage(raw) {
        if (!raw) return null;
        return {
            Id: value(raw, 'id', 'Id'),
            BookingId: value(raw, 'bookingId', 'BookingId'),
            SenderId: value(raw, 'senderId', 'SenderId') || '',
            ReceiverId: value(raw, 'receiverId', 'ReceiverId') || '',
            Text: value(raw, 'messageText', 'MessageText') ?? raw.Text ?? '',
            SentAt: value(raw, 'sentAt', 'SentAt'),
            IsRead: Boolean(value(raw, 'isRead', 'IsRead'))
        };
    }

    function toDisplayMessage(raw, locale = 'en-US') {
        const message = normalizeMessage(raw);
        if (!message) return null;
        return {
            ...message,
            IsMine: String(message.SenderId) === String(currentUserId()),
            Time: message.SentAt
                ? new Date(message.SentAt).toLocaleTimeString(locale, { hour: '2-digit', minute: '2-digit' })
                : ''
        };
    }

    function appendMessageIfMissing(thread, raw, locale = 'en-US') {
        const message = toDisplayMessage(raw, locale);
        if (!message || message.Id == null || !thread) return false;
        thread.Messages ||= [];
        const existing = thread.Messages.find(item => String(item.Id) === String(message.Id));
        if (existing) {
            Object.assign(existing, message);
            return false;
        }
        thread.Messages.push(message);
        return true;
    }

    function getParticipantId(thread) {
        return thread?.ParticipantId || thread?.OtherId || thread?.TravelerId || '';
    }

    function getMessageParticipantId(message) {
        return String(message?.SenderId) === String(currentUserId())
            ? message?.ReceiverId
            : message?.SenderId;
    }

    function messagesUrl(thread) {
        const participantId = getParticipantId(thread);
        return participantId
            ? `/api/chat/conversations/participants/${encodeURIComponent(participantId)}/messages`
            : `/api/chat/conversations/${encodeURIComponent(thread?.BookingId || '')}/messages`;
    }

    function markReadUrl(thread) {
        const participantId = getParticipantId(thread);
        return participantId
            ? `/api/chat/conversations/participants/${encodeURIComponent(participantId)}/mark-read`
            : `/api/chat/conversations/${encodeURIComponent(thread?.BookingId || '')}/mark-read`;
    }

    function isUrl(text) {
        return typeof text === 'string' && /^(https?):\/\//i.test(text);
    }

    function isImageUrl(text) {
        return isUrl(text) && /\.(jpg|jpeg|png|gif|webp)(\?|$)/i.test(text);
    }

    function fileType(url) {
        const clean = String(url || '').split('?')[0].toLowerCase();
        if (/\.(jpg|jpeg|png|gif|webp)$/.test(clean)) return 'image';
        if (clean.endsWith('.pdf')) return 'pdf';
        if (clean.endsWith('.mp4')) return 'video';
        if (clean.endsWith('.mp3')) return 'audio';
        if (/\.(doc|docx|xls|xlsx|txt)$/.test(clean)) return 'document';
        return 'file';
    }

    function formatFileSize(bytes) {
        if (!Number.isFinite(bytes) || bytes <= 0) return '0 B';
        const units = ['B', 'KB', 'MB', 'GB'];
        const unit = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
        const amount = bytes / Math.pow(1024, unit);
        return `${amount >= 10 || unit === 0 ? amount.toFixed(0) : amount.toFixed(1)} ${units[unit]}`;
    }

    function validateAttachment(file) {
        const allowed = new Set([
            'image/jpeg', 'image/png', 'image/gif', 'image/webp', 'application/pdf',
            'text/plain', 'application/msword',
            'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
            'application/vnd.ms-excel',
            'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
            'audio/mpeg', 'video/mp4'
        ]);
        if (file.size > 10 * 1024 * 1024) return 'Attachments cannot exceed 10 MB.';
        if (!allowed.has(file.type)) return 'Unsupported attachment type.';
        return '';
    }

    function createThreadFromConversation(conversation, options = {}) {
        const userId = options.userId || currentUserId();
        const locale = options.locale || 'en-US';
        const isGuide = Boolean(options.isGuide);
        const bookingId = value(conversation, 'bookingId', 'BookingId') || '';
        const travelerId = value(conversation, 'travelerId', 'TravelerId') || '';
        const guideId = value(conversation, 'guideId', 'GuideId') || '';
        const participantId = value(conversation, 'participantId', 'ParticipantId')
            || (String(travelerId) === String(userId) ? guideId : travelerId);
        const resolvedName = value(conversation, 'participantName', 'ParticipantName');
        const resolvedAvatar = value(conversation, 'participantAvatarUrl', 'ParticipantAvatarUrl');
        const participantName = resolvedName || participantId;
        const participantAvatar = resolvedAvatar || '/images/AVATAR.png';
        const lastMessage = value(conversation, 'lastMessage', 'LastMessage') || (isGuide ? 'Không có tin nhắn' : 'No messages yet');
        const lastMessageAt = value(conversation, 'lastMessageAt', 'LastMessageAt');

        return {
            Id: bookingId || `participant:${participantId}`,
            BookingId: bookingId,
            BookingIds: bookingId ? [bookingId] : [],
            TravelerId: travelerId,
            GuideId: guideId,
            ParticipantId: participantId,
            OtherId: participantId,
            OtherName: participantName,
            OtherAvatar: participantAvatar,
            TravelerName: participantName,
            TravelerAvatar: participantAvatar,
            HasParticipantProfile: Boolean(resolvedName || resolvedAvatar),
            Messages: [],
            LastMessage: lastMessage,
            LastMessageAt: lastMessageAt || null,
            TimeAgo: lastMessageAt && options.formatTimeAgo ? options.formatTimeAgo(lastMessageAt) : '',
            Date: lastMessageAt ? new Date(lastMessageAt).toLocaleDateString(locale) : '',
            UnreadCount: Number(value(conversation, 'unreadCount', 'UnreadCount')) || 0,
            IsLocked: true
        };
    }

    window.TripMateChatCommon = {
        accessToken,
        currentUserId,
        normalizeMessage,
        toDisplayMessage,
        appendMessageIfMissing,
        getParticipantId,
        getMessageParticipantId,
        messagesUrl,
        markReadUrl,
        isUrl,
        isImageUrl,
        fileType,
        formatFileSize,
        validateAttachment,
        createThreadFromConversation
    };
})();
