(() => {
    const page = document.getElementById('salesOrdersPage');
    let rows = [...document.querySelectorAll('.sales-order-row')];
    const table = document.getElementById('salesOrdersTable');
    const search = document.getElementById('salesOrderSearch');
    const pageSize = document.getElementById('salesOrderPageSize');
    const pagination = document.getElementById('salesOrderPagination');
    const rangeText = document.getElementById('salesOrderRangeText');
    const emptyRow = document.getElementById('emptySalesOrders');
    const statusTabs = [...document.querySelectorAll('.sales-status-tabs [data-status]')];
    let selectedStatus = 'all';
    let currentPage = 1;

    function matchingRows() {
        const term = search.value.trim().toLowerCase();
        return rows.filter(row => {
            const matchesSearch = !term || row.dataset.search.includes(term);
            const matchesStatus = selectedStatus === 'all' || row.dataset.status === selectedStatus;
            return matchesSearch && matchesStatus;
        });
    }

    function addPageButton(label, pageNumber, options = {}) {
        const item = document.createElement('li');
        const button = document.createElement('button');
        item.className = `page-item${options.disabled ? ' disabled' : ''}${options.active ? ' active' : ''}`;
        button.className = 'page-link';
        button.type = 'button';
        button.textContent = label;
        button.disabled = options.disabled ?? false;
        button.addEventListener('click', () => { currentPage = pageNumber; render(); });
        item.appendChild(button);
        pagination.appendChild(item);
    }

    function render() {
        const filtered = matchingRows();
        const size = Number(pageSize.value);
        const pageCount = Math.max(1, Math.ceil(filtered.length / size));
        currentPage = Math.min(currentPage, pageCount);
        rows.forEach(row => row.classList.add('d-none'));

        const start = (currentPage - 1) * size;
        filtered.slice(start, start + size).forEach(row => row.classList.remove('d-none'));
        emptyRow.classList.toggle('d-none', filtered.length > 0);
        rangeText.textContent = filtered.length
            ? `Showing ${start + 1}–${Math.min(start + size, filtered.length)} of ${filtered.length}`
            : 'Showing 0 items';

        pagination.innerHTML = '';
        addPageButton('Previous', currentPage - 1, { disabled: currentPage === 1 });
        for (let number = 1; number <= pageCount; number += 1) {
            addPageButton(String(number), number, { active: number === currentPage });
        }
        addPageButton('Next', currentPage + 1, { disabled: currentPage === pageCount });
    }

    async function refundCheckout(button) {
        if (!confirm(`Refund ${button.dataset.code}? Product inventory will be restored.`)) return;

        const response = await fetch(page.dataset.refundUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ id: button.dataset.id })
        });
        const result = await response.json();

        if (result.success) {
            window.reloadWithToast(result.message || 'Sales transaction updated.');
            return;
        }
        window.showToast(result.message || 'Unable to update the sales transaction.', 'error');
    }

    async function deleteCheckout(button) {
        if (!confirm(`Delete the sales details for ${button.dataset.code}? Product inventory will remain unchanged.`)) return;
        const response = await fetch(page.dataset.deleteUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ id: button.dataset.id })
        });
        const result = await response.json();
        if (result.success) {
            window.reloadWithToast(result.message || 'Sales details deleted.');
            return;
        }
        window.showToast(result.message || 'Unable to delete the sales details.', 'error');
    }

    async function deleteSelectedCheckouts(event) {
        event.preventDefault();
        const ids = event.detail?.ids ?? [];
        if (!ids.length || !confirm(`Delete ${ids.length} selected sales details? Product inventory will remain unchanged.`)) return;

        const response = await fetch(page.dataset.bulkDeleteUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(ids)
        });
        const result = await response.json();
        if (result.success) {
            window.reloadWithToast(result.message || 'Selected sales details deleted.');
            return;
        }
        window.showToast(result.message || 'Unable to delete the selected sales details.', 'error');
    }

    search.addEventListener('input', () => { currentPage = 1; render(); });
    pageSize.addEventListener('change', () => { currentPage = 1; render(); });
    statusTabs.forEach(tab => tab.addEventListener('click', () => {
        selectedStatus = tab.dataset.status;
        currentPage = 1;

        statusTabs.forEach(item => {
            const isActive = item === tab;
            item.classList.toggle('active', isActive);
            item.setAttribute('aria-pressed', String(isActive));
        });

        render();
    }));
    rows.forEach(row => row.addEventListener('click', event => {
        if (!event.target.closest('a, button, input, label, .dropdown-menu')) window.location.href = row.dataset.href;
    }));
    document.querySelectorAll('.refund-checkout').forEach(button => button.addEventListener('click', () => refundCheckout(button)));
    document.querySelectorAll('.delete-checkout').forEach(button => button.addEventListener('click', () => deleteCheckout(button)));
    table?.addEventListener('table:bulk-delete', deleteSelectedCheckouts);
    table?.addEventListener('table:sorted', () => {
        rows = [...document.querySelectorAll('.sales-order-row')];
        currentPage = 1;
        render();
    });
    document.getElementById('exportSalesOrders')?.addEventListener('click', event => {
        const hasFilters = Boolean(search.value.trim()) || selectedStatus !== 'all';
        window.rjtechExcelExport.download(event.currentTarget, matchingRows(), hasFilters);
    });
    render();
})();
