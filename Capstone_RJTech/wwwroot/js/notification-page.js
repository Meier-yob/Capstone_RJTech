(() => {
    const page = document.getElementById('notificationPage');
    const rows = [...document.querySelectorAll('.notification-row')];
    const tabs = [...document.querySelectorAll('.notification-filter [data-filter]')];
    const unreadCount = document.getElementById('pageUnreadCount');
    const empty = document.getElementById('notificationEmpty');
    let selectedFilter = 'all';

    function render() {
        let visible = 0;
        rows.forEach(row => {
            const isRead = row.dataset.read === 'true';
            const show = selectedFilter === 'all' ||
                (selectedFilter === 'read' && isRead) ||
                (selectedFilter === 'unread' && !isRead);
            row.classList.toggle('d-none', !show);
            visible += show ? 1 : 0;
        });
        empty.classList.toggle('d-none', visible > 0);
        unreadCount.textContent = `${rows.filter(row => row.dataset.read !== 'true').length} unread`;
    }

    async function markRead(row, event) {
        if (row.dataset.read === 'true') return;
        event.preventDefault();
        const response = await fetch(page.dataset.readUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ id: row.dataset.id })
        });
        const result = await response.json();
        if (result.success) location.href = row.href;
    }

    tabs.forEach(tab => tab.addEventListener('click', () => {
        tabs.forEach(item => item.classList.remove('active'));
        tab.classList.add('active');
        selectedFilter = tab.dataset.filter;
        render();
    }));
    rows.forEach(row => row.addEventListener('click', event => markRead(row, event)));
    document.getElementById('markAllNotificationsRead').addEventListener('click', async () => {
        const response = await fetch(page.dataset.readAllUrl, { method: 'POST' });
        const result = await response.json();
        if (!result.success) return;
        rows.forEach(row => {
            row.dataset.read = 'true';
            row.classList.remove('is-unread');
            row.querySelector('.unread-dot')?.remove();
        });
        render();
    });
})();
