(() => {
    const page = document.getElementById('customerPage');
    if (!page) return;

    let rows = [...document.querySelectorAll('.customer-row')];
    const table = document.getElementById('customerTable');
    const search = document.getElementById('customerSearch');
    const pageSize = document.getElementById('customerPageSize');
    const pagination = document.getElementById('customerPagination');
    const rangeText = document.getElementById('customerRangeText');
    const emptyRow = document.getElementById('emptyCustomers');
    let currentPage = 1;

    function matchingRows() {
        const term = search.value.trim().toLowerCase();
        return rows.filter(row => !term || row.dataset.search.includes(term));
    }

    function addPageButton(label, page, disabled = false, active = false) {
        const item = document.createElement('li');
        const button = document.createElement('button');
        item.className = `page-item${disabled ? ' disabled' : ''}${active ? ' active' : ''}`;
        button.className = 'page-link';
        button.type = 'button';
        button.textContent = label;
        button.disabled = disabled;
        button.addEventListener('click', () => { currentPage = page; render(); });
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
        addPageButton('Previous', currentPage - 1, currentPage === 1);
        for (let page = 1; page <= pageCount; page += 1) addPageButton(String(page), page, false, page === currentPage);
        addPageButton('Next', currentPage + 1, currentPage === pageCount);
    }

    async function deleteSelectedCustomers(event) {
        event.preventDefault();
        const ids = event.detail?.ids ?? [];
        if (!ids.length || !confirm(`Delete ${ids.length} selected customers? Customers with sales history cannot be deleted.`)) return;

        try {
            const response = await fetch(page.dataset.bulkDeleteUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(ids)
            });
            const result = await response.json();
            if (result.success) {
                window.reloadWithToast(result.message || 'Selected customers deleted successfully.');
                return;
            }
            window.showToast(result.message || 'Unable to delete the selected customers.', 'error');
        } catch {
            window.showToast('Unable to delete the selected customers.', 'error');
        }
    }

    search.addEventListener('input', () => { currentPage = 1; render(); });
    pageSize.addEventListener('change', () => { currentPage = 1; render(); });
    table?.addEventListener('table:bulk-delete', deleteSelectedCustomers);
    table?.addEventListener('table:sorted', () => {
        rows = [...document.querySelectorAll('.customer-row')];
        currentPage = 1;
        render();
    });
    document.getElementById('exportCustomers')?.addEventListener('click', event => {
        window.rjtechExcelExport.download(event.currentTarget, matchingRows(), Boolean(search.value.trim()));
    });
    render();
})();
