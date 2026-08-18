(() => {
    const toggle = document.getElementById('notificationToggle');
    const flyout = document.getElementById('notificationFlyout');
    if (!toggle || !flyout) return;

    const list = document.getElementById('notificationFlyoutList');
    const badge = document.getElementById('notificationBadge');
    const dot = document.getElementById('notificationDot');
    const count = document.getElementById('flyoutUnreadCount');

    function iconFor(type) {
        return {
            'low-stock': 'bi-exclamation-triangle',
            'out-of-stock': 'bi-x-circle',
            calendar: 'bi-calendar-event'
        }[type] || 'bi-info-circle';
    }

    function relativeTime(value) {
        const seconds = Math.max(1, Math.floor((Date.now() - new Date(value).getTime()) / 1000));
        if (seconds < 60) return 'Just now';
        if (seconds < 3600) return `${Math.floor(seconds / 60)} min ago`;
        if (seconds < 86400) return `${Math.floor(seconds / 3600)} hr ago`;
        return `${Math.floor(seconds / 86400)} day(s) ago`;
    }

    function updateUnreadCount(unreadCount) {
        count.textContent = unreadCount;
        badge.textContent = unreadCount > 99 ? '99+' : unreadCount;
        badge.classList.toggle('d-none', unreadCount === 0);
        dot.classList.toggle('d-none', unreadCount === 0);
        const sidebarCount = document.querySelector('.sidebar .nav-count');
        if (sidebarCount) {
            sidebarCount.textContent = unreadCount;
            sidebarCount.classList.toggle('d-none', unreadCount === 0);
        }
    }

    function createNotificationItem(notification) {
        const link = document.createElement('a');
        link.className = `notification-flyout-item${notification.isRead ? '' : ' is-unread'}`;
        link.href = notification.url;
        link.dataset.id = notification.id;
        link.innerHTML = `
            <span class="notification-icon notification-icon-${notification.type}"><i class="bi ${iconFor(notification.type)}"></i></span>
            <span class="notification-copy">
                <span class="notification-title">${notification.title}${notification.isRead ? '' : '<span class="unread-dot"></span>'}</span>
                <span class="notification-message"></span>
                <time>${relativeTime(notification.createdAt)}</time>
            </span>`;
        link.querySelector('.notification-message').textContent = notification.message;
        link.addEventListener('click', async event => {
            if (notification.isRead) return;
            event.preventDefault();
            await fetch(flyout.dataset.readUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams({ id: notification.id })
            });
            location.href = notification.url;
        });
        return link;
    }

    async function loadNotifications() {
        try {
            const response = await fetch(flyout.dataset.listUrl);
            const result = await response.json();
            if (!result.success) throw new Error(result.message);

            list.innerHTML = '';
            result.notifications.forEach(item => list.appendChild(createNotificationItem(item)));
            if (!result.notifications.length) {
                list.innerHTML = '<div class="notification-loading">You are all caught up.</div>';
            }
            updateUnreadCount(result.unreadCount);
        } catch {
            list.innerHTML = '<div class="notification-loading text-danger">Unable to load notifications.</div>';
        }
    }

    toggle.addEventListener('click', event => {
        event.stopPropagation();
        const willOpen = flyout.classList.contains('d-none');
        flyout.classList.toggle('d-none', !willOpen);
        toggle.setAttribute('aria-expanded', String(willOpen));
        if (willOpen) loadNotifications();
    });
    flyout.addEventListener('click', event => event.stopPropagation());
    document.addEventListener('click', () => {
        flyout.classList.add('d-none');
        toggle.setAttribute('aria-expanded', 'false');
    });
    document.getElementById('flyoutMarkAllRead').addEventListener('click', async () => {
        await fetch(flyout.dataset.readAllUrl, { method: 'POST' });
        await loadNotifications();
    });

    loadNotifications();
})();
