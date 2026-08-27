(() => {
    const page = document.getElementById('salesOrdersPage');
    const rows = [...document.querySelectorAll('.sales-order-row')];
    const search = document.getElementById('salesOrderSearch');
    const pageSize = document.getElementById('salesOrderPageSize');
    const pagination = document.getElementById('salesOrderPagination');
    const rangeText = document.getElementById('salesOrderRangeText');
    const emptyRow = document.getElementById('emptySalesOrders');
    let currentPage = 1;

    function matchingRows() {
        const term = search.value.trim().toLowerCase();
        return rows.filter(row => !term || row.dataset.search.includes(term));
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

    async function deleteCheckout(button) {
        if (!confirm('Are you sure you want to delete this sales transaction?')) return;

        const response = await fetch(page.dataset.deleteUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ id: button.dataset.id })
        });
        const result = await response.json();

        if (result.success) {
            window.reloadWithToast(result.message || 'Sales transaction deleted.');
        }
    }

    function exportCsv() {
        const data = [['Checkout ID', 'Customer', 'Payment Method', 'Date Purchased', 'Status', 'Total Amount']];
        matchingRows().forEach(row => {
            data.push([...row.querySelectorAll('td')].slice(0, 6).map(cell => cell.innerText.trim().replace(/\s+/g, ' ')));
        });
        const csv = data.map(values => values.map(value => `"${value.replaceAll('"', '""')}"`).join(',')).join('\n');
        const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }));
        const link = Object.assign(document.createElement('a'), { href: url, download: 'rjtech-sales-orders.csv' });
        link.click();
        URL.revokeObjectURL(url);
    }

    search.addEventListener('input', () => { currentPage = 1; render(); });
    pageSize.addEventListener('change', () => { currentPage = 1; render(); });
    rows.forEach(row => row.addEventListener('click', event => {
        if (!event.target.closest('a, button, .dropdown-menu')) window.location.href = row.dataset.href;
    }));
    document.querySelectorAll('.delete-checkout').forEach(button => button.addEventListener('click', () => deleteCheckout(button)));
    document.getElementById('exportSalesOrders').addEventListener('click', exportCsv);
    render();
})();
