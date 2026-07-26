(function () {
    const state = { unreadCount: 0, connection: null, startPromise: null, retryTimer: null, listenersAttached: false };

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

    function scheduleReconnect() {
        if (state.retryTimer !== null || !token() || !userId()) return;
        state.retryTimer = window.setTimeout(() => {
            state.retryTimer = null;
            connect();
        }, 5000);
    }

    async function connect() {
        if (!token() || !userId() || !window.signalR) return;
        if (state.startPromise) return state.startPromise;
        if (state.connection && state.connection.state !== signalR.HubConnectionState.Disconnected) return;

        if (!state.connection) state.connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/notifications', { accessTokenFactory: token })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .build();
        if (!state.listenersAttached) {
            state.connection.on('NotificationReceived', notification => {
                renderCount(state.unreadCount + 1);
                document.dispatchEvent(new CustomEvent('tripmate:notification', { detail: notification }));
            });
            state.connection.onreconnected(async () => {
                await refreshCount();
                document.dispatchEvent(new CustomEvent('tripmate:notifications-reconnected'));
            });
            state.connection.onclose(() => scheduleReconnect());
            state.listenersAttached = true;
        }

        state.startPromise = state.connection.start()
            .catch(error => {
                console.warn('Notification realtime connection unavailable', error);
                scheduleReconnect();
            })
            .finally(() => { state.startPromise = null; });
        return state.startPromise;
    }

    window.TripMateNotifications = {
        headers,
        refreshCount,
        connect,
        setUnreadCount: renderCount,
        decrement() { renderCount(state.unreadCount - 1); }
    };

    document.addEventListener('DOMContentLoaded', () => {
        refreshCount();
        connect();
    });
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') {
            refreshCount();
            connect();
        }
    });
    window.addEventListener('online', () => {
        refreshCount();
        connect();
    });
})();
