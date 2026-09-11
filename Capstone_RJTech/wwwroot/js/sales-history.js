(() => {
    const page = document.getElementById('salesHistoryPage');
    if (!page) return;
    const table = document.getElementById('salesHistoryTable');
    const search = document.getElementById('historySearch');
    const dateFilter = document.getElementById('historyDateFilter');
    const methodFilter = document.getElementById('historyMethodFilter');
    const dateFrom = document.getElementById('historyDateFrom');
    const dateTo = document.getElementById('historyDateTo');
    const pageSize = document.getElementById('historyPageSize');
    const pagination = document.getElementById('historyPagination');
    const emptyRow = document.getElementById('emptySalesHistory');
    const exportButton = document.getElementById('exportSalesHistory');
    let currentPage = 1;

    function dateKey(date) {
        return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
    }

    function matchingRows() {
        const term = search.value.trim().toLowerCase();
        // Payment dates and preset filters use the store's Philippine calendar date.
        const parts = new Intl.DateTimeFormat('en-US', {
            timeZone: 'Asia/Manila', year: 'numeric', month: '2-digit', day: '2-digit'
        }).formatToParts(new Date());
        const part = type => parts.find(item => item.type === type).value;
        const today = new Date(Number(part('year')), Number(part('month')) - 1, Number(part('day')));
        const end = dateKey(today);
        let from = '';
        let to = '';
        if (dateFilter.value === 'today') from = to = end;
        if (dateFilter.value === 'week') {
            today.setDate(today.getDate() - 6);
            from = dateKey(today);
            to = end;
        }
        if (dateFilter.value === 'month') {
            today.setDate(1);
            from = dateKey(today);
            to = end;
        }
        if (dateFilter.value === 'custom') {
            from = dateFrom.value;
            to = dateTo.value;
        }
        return [...table.querySelectorAll('.sales-history-row')].filter(row =>
            (!term || row.dataset.search.includes(term)) &&
            (methodFilter.value === 'all' || row.dataset.method === methodFilter.value) &&
            (!from || row.dataset.date >= from) && (!to || row.dataset.date <= to));
    }

    function pageButton(label, number, disabled = false) {
        const item = document.createElement('li');
        const button = document.createElement('button');
        const active = number === currentPage && /^\d+$/.test(label);
        item.className = `page-item${disabled ? ' disabled' : ''}${active ? ' active' : ''}`;
        button.type = 'button';
        button.className = 'page-link';
        button.textContent = label;
        button.disabled = disabled;
        if (active) button.setAttribute('aria-current', 'page');
        button.addEventListener('click', () => { currentPage = number; render(); });
        item.appendChild(button);
        pagination.appendChild(item);
    }

    function render() {
        const custom = dateFilter.value === 'custom';
        document.getElementById('historyCustomDates').classList.toggle('d-none', !custom);
        const invalidRange = custom && dateFrom.value && dateTo.value && dateFrom.value > dateTo.value;
        document.getElementById('historyDateError').textContent = invalidRange ? 'The end date must be on or after the start date.' : '';
        const filtered = matchingRows();
        const size = Number(pageSize.value);
        const count = Math.max(1, Math.ceil(filtered.length / size));
        currentPage = Math.min(currentPage, count);
        const start = (currentPage - 1) * size;
        table.querySelectorAll('.sales-history-row').forEach(row => row.classList.add('d-none'));
        filtered.slice(start, start + size).forEach(row => row.classList.remove('d-none'));
        emptyRow.classList.toggle('d-none', filtered.length > 0);
        emptyRow.querySelector('span').textContent = table.querySelector('.sales-history-row')
            ? 'No payments match your filters.' : 'No payments recorded yet.';
        document.getElementById('historyRangeText').textContent = filtered.length
            ? `Showing ${start + 1}–${Math.min(start + size, filtered.length)} of ${filtered.length} payments`
            : 'Showing 0 payments';
        exportButton.disabled = filtered.length === 0 || Boolean(invalidRange);
        pagination.replaceChildren();
        pageButton('Previous', currentPage - 1, currentPage === 1);
        // Bound the control width even when history spans hundreds of pages.
        const firstPage = Math.max(1, Math.min(currentPage - 2, count - 4));
        for (let number = firstPage; number <= Math.min(count, firstPage + 4); number++) {
            pageButton(String(number), number);
        }
        pageButton('Next', currentPage + 1, currentPage === count);
    }

    search.addEventListener('input', () => { currentPage = 1; render(); });
    [dateFilter, methodFilter, dateFrom, dateTo, pageSize].forEach(control =>
        control.addEventListener('change', () => { currentPage = 1; render(); }));
    table.addEventListener('table:sorted', () => { currentPage = 1; render(); });
    exportButton.addEventListener('click', () => {
        const filtered = Boolean(search.value.trim()) || dateFilter.value !== 'all' || methodFilter.value !== 'all';
        window.rjtechExcelExport.download(exportButton, matchingRows(), filtered);
    });
    render();
})();
