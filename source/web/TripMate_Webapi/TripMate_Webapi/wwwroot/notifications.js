(function () {
    const state = { unreadCount: 0, connection: null };

    function token() { return localStorage.getItem('accessToken') || ''; }
    function userId() { return localStorage.getItem('userId') || ''; }
    function headers() { return { Authorization: `Bearer ${token()}` }; }

    function renderCount(count) {
        state.unreadCount = Math.max(0, Number(count) || 0);
        document.querySelectorAll('[data-notification-count]').forEach(element => {
            element.textContent = state.unreadCount > 99 ? '99+' : String(state.unreadCount);
            element.classList.toggle('hidden', state.unreadCount === 0);
        });
        document.querySelectorAll('[data-notification-dot]').forEach(element => {
            element.classList.toggle('hidden', state.unreadCount === 0);
        });
    }

    async function refreshCount() {
        if (!token() || !userId()) return renderCount(0);
        try {
            const response = await fetch('/api/notifications/unread-count', { headers: headers() });
            if (!response.ok) return;
            const data = await response.json();
            renderCount(data.count);
        } catch (error) {
            console.warn('Unable to load notification count', error);
        }
    }

    async function connect() {
        if (!token() || !userId() || !window.signalR || state.connection) return;
        state.connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/notifications', { accessTokenFactory: token })
            .withAutomaticReconnect()
            .build();
        state.connection.on('NotificationReceived', notification => {
            renderCount(state.unreadCount + 1);
            document.dispatchEvent(new CustomEvent('tripmate:notification', { detail: notification }));
        });
        state.connection.onreconnected(refreshCount);
        try { await state.connection.start(); }
        catch (error) { console.warn('Notification realtime connection unavailable', error); }
    }

    window.TripMateNotifications = {
        headers,
        refreshCount,
        setUnreadCount: renderCount,
        decrement() { renderCount(state.unreadCount - 1); }
    };

    document.addEventListener('DOMContentLoaded', () => {
        refreshCount();
        connect();
    });
})();
