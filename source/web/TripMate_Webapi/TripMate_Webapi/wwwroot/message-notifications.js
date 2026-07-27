(() => {
    if (window.TripMateMessageNotifications) return;

    const senderNames = new Map();

    function currentUserId() {
        return localStorage.getItem('userId') || '';
    }

    function accessToken() {
        return localStorage.getItem('accessToken') || '';
    }

    function isMessagesPage() {
        return /^\/(traveler|guide)\/messages\/?$/i.test(window.location.pathname);
    }

    async function getSenderName(senderId) {
        if (senderNames.has(senderId)) return senderNames.get(senderId);

        const token = accessToken();
        if (!token) return '';

        try {
            const response = await fetch(`/api/chat/profiles/${encodeURIComponent(senderId)}`, {
                headers: { Authorization: `Bearer ${token}` },
                cache: 'no-store'
            });
            if (!response.ok) return '';

            const profile = await response.json();
            const senderName = String(profile.fullName || profile.FullName || '').trim();
            if (senderName) senderNames.set(senderId, senderName);
            return senderName;
        } catch (error) {
            console.warn('Unable to load the message sender profile', error);
            return '';
        }
    }

    function getToastContainer() {
        let container = document.getElementById('message-toast-container');
        if (container) return container;

        container = document.createElement('div');
        container.id = 'message-toast-container';
        container.className = 'fixed top-6 right-6 z-[110] flex max-w-[calc(100vw-3rem)] flex-col gap-3 pointer-events-none';
        container.setAttribute('aria-live', 'polite');
        document.body.appendChild(container);
        return container;
    }

    function removeToast(toast) {
        if (!toast?.isConnected) return;
        toast.classList.add('translate-x-10', 'opacity-0');
        window.setTimeout(() => toast.remove(), 300);
    }

    function showSenderToast(senderName) {
        const toast = document.createElement('div');
        toast.className = 'relative flex min-h-16 w-80 max-w-full translate-x-10 items-center overflow-hidden rounded-lg border border-gray-100 bg-white opacity-0 shadow-[0_4px_12px_rgba(0,0,0,0.08)] transition-all duration-300 pointer-events-auto';

        const accent = document.createElement('div');
        accent.className = 'absolute inset-y-0 left-0 w-1.5 bg-[#2E77F2]';

        const content = document.createElement('div');
        content.className = 'flex-1 py-3 pl-6 pr-3';

        const title = document.createElement('h4');
        title.className = 'text-[15px] font-bold leading-tight text-gray-900';
        title.textContent = 'New message';

        const message = document.createElement('p');
        message.className = 'mt-1 text-[13px] leading-snug text-gray-500';
        message.textContent = `${senderName} just sent you a message`;

        content.append(title, message);

        const closeButton = document.createElement('button');
        closeButton.type = 'button';
        closeButton.className = 'self-stretch px-4 text-gray-400 transition-colors hover:text-gray-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500';
        closeButton.setAttribute('aria-label', 'Dismiss new message notification');
        closeButton.innerHTML = '<span class="material-symbols-outlined text-[20px]" aria-hidden="true">close</span>';
        closeButton.addEventListener('click', () => removeToast(toast));

        toast.append(accent, content, closeButton);
        getToastContainer().appendChild(toast);

        requestAnimationFrame(() => {
            requestAnimationFrame(() => toast.classList.remove('translate-x-10', 'opacity-0'));
        });
        window.setTimeout(() => removeToast(toast), 5000);
    }

    async function handleNewMessage(event) {
        if (isMessagesPage()) return;

        const message = event.detail || {};
        const senderId = String(message.senderId ?? message.SenderId ?? '');
        const receiverId = String(message.receiverId ?? message.ReceiverId ?? '');
        const userId = currentUserId();

        // The hub also broadcasts the event back to the sender.
        if (!senderId || !userId || senderId === userId || receiverId !== userId) return;

        const senderName = await getSenderName(senderId);
        if (senderName && !isMessagesPage()) showSenderToast(senderName);
    }

    document.addEventListener('tripmate:message-created', handleNewMessage);

    window.TripMateMessageNotifications = {
        showSenderToast
    };
})();
