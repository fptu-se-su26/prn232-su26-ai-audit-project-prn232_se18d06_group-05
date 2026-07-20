(() => {
    if (window.TripMateChat) return;

    const state = {
        connection: null,
        startPromise: null,
        retryTimer: null
    };

    function token() {
        return localStorage.getItem('accessToken') || '';
    }

    function userId() {
        return localStorage.getItem('userId') || '';
    }

    function dispatch(name, detail) {
        document.dispatchEvent(new CustomEvent(name, { detail }));
    }

    function createConnection() {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/chat', { accessTokenFactory: token })
            .withAutomaticReconnect()
            .build();

        connection.on('MessageCreated', message => {
            dispatch('tripmate:message-created', message);
            window.dispatchEvent(new CustomEvent('tripmate:messages-changed'));
        });

        connection.on('MessageUpdated', message => {
            dispatch('tripmate:message-updated', message);
        });

        connection.onreconnected(() => {
            dispatch('tripmate:chat-reconnected');
            window.TripMateMessages?.refreshUnreadThreads();
        });

        connection.onclose(() => {
            scheduleRetry();
        });

        return connection;
    }

    function scheduleRetry() {
        if (state.retryTimer !== null || !token() || !userId()) return;
        state.retryTimer = window.setTimeout(() => {
            state.retryTimer = null;
            start();
        }, 5000);
    }

    async function start() {
        if (!token() || !userId() || !window.signalR) return;

        if (!state.connection) state.connection = createConnection();
        if (state.connection.state !== signalR.HubConnectionState.Disconnected) return;
        if (state.startPromise) return state.startPromise;

        state.startPromise = state.connection.start()
            .catch(error => {
                console.warn('Chat realtime connection unavailable', error);
                scheduleRetry();
            })
            .finally(() => {
                state.startPromise = null;
            });

        return state.startPromise;
    }

    window.TripMateChat = {
        start,
        get connection() { return state.connection; }
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start, { once: true });
    } else {
        start();
    }

    document.addEventListener('turbo:load', start);
})();
