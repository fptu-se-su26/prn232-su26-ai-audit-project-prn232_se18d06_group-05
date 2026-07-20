(() => {
    if (window.TripMateMessages) return;

    let unreadThreadCount = 0;
    let refreshTimer = null;

    function render(count) {
        unreadThreadCount = Math.max(0, Number(count) || 0);
        document.querySelectorAll('[data-message-thread-count]').forEach(element => {
            element.textContent = unreadThreadCount > 99 ? '99+' : String(unreadThreadCount);
            element.classList.toggle('hidden', unreadThreadCount === 0);
            element.classList.toggle('inline-flex', unreadThreadCount > 0);
        });
    }

    async function refreshUnreadThreads() {
        const token = localStorage.getItem('accessToken');
        if (!token) {
            render(0);
            return;
        }

        try {
            const response = await fetch('/api/chat/unread-conversation-count', {
                headers: { Authorization: `Bearer ${token}` },
                cache: 'no-store'
            });
            if (!response.ok) return;
            const data = await response.json();
            render(data.count || 0);
        } catch (error) {
            console.warn('Unable to refresh unread message threads', error);
        }
    }

    function start() {
        refreshUnreadThreads();
        if (refreshTimer === null) {
            refreshTimer = window.setInterval(refreshUnreadThreads, 15000);
        }
    }

    window.TripMateMessages = {
        refreshUnreadThreads,
        setUnreadThreadCount: render,
        getUnreadThreadCount: () => unreadThreadCount
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start, { once: true });
    } else {
        start();
    }

    document.addEventListener('turbo:load', refreshUnreadThreads);
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') refreshUnreadThreads();
    });
    window.addEventListener('tripmate:messages-changed', refreshUnreadThreads);
})();
