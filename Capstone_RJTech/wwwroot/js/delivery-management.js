(() => {
    const page = document.getElementById('deliveryManagementPage');
    let rows = [...document.querySelectorAll('.delivery-row')];
    const table = document.getElementById('deliveryTable');
    const search = document.getElementById('deliverySearch');
    const pageSize = document.getElementById('deliveryPageSize');
    const pagination = document.getElementById('deliveryPagination');
    const emptyRow = document.getElementById('deliveryEmpty');
    let currentPage = 1;

    function filteredRows() {
        const term = search.value.trim().toLowerCase();
        return rows.filter(row => !term || row.dataset.search.includes(term));
    }

    function pageButton(label, target, options = {}) {
        const item = document.createElement('li');
        const button = document.createElement('button');
        item.className = `page-item${options.disabled ? ' disabled' : ''}${options.active ? ' active' : ''}`;
        button.className = 'page-link';
        button.type = 'button';
        button.innerHTML = label;
        button.disabled = options.disabled ?? false;
        button.addEventListener('click', () => { currentPage = target; render(); });
        item.appendChild(button);
        pagination.appendChild(item);
    }

    function render() {
        const filtered = filteredRows();
        const size = Number(pageSize.value);
        const pages = Math.max(1, Math.ceil(filtered.length / size));
        currentPage = Math.min(currentPage, pages);
        rows.forEach(row => row.classList.add('d-none'));
        const start = (currentPage - 1) * size;
        filtered.slice(start, start + size).forEach(row => row.classList.remove('d-none'));
        emptyRow.classList.toggle('d-none', filtered.length > 0);
        const end = Math.min(start + size, filtered.length);
        document.getElementById('deliveryRangeText').textContent = `Showing ${filtered.length ? start + 1 : 0}–${end} of ${filtered.length} items`;
        document.getElementById('deliveryRangeBar').style.width = `${filtered.length ? end / filtered.length * 100 : 0}%`;
        pagination.innerHTML = '';
        pageButton('<i class="bi bi-chevron-left"></i>', currentPage - 1, { disabled: currentPage === 1 });
        for (let number = 1; number <= pages; number += 1) pageButton(String(number), number, { active: number === currentPage });
        pageButton('<i class="bi bi-chevron-right"></i>', currentPage + 1, { disabled: currentPage === pages });
    }

    async function deleteDelivery(button) {
        if (!confirm(`Delete the details for ${button.dataset.code}? Received product quantities will remain unchanged.`)) return;
        const response = await fetch(page.dataset.deleteUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ delivery_ID: button.dataset.id })
        });
        const result = await response.json();
        if (result.success) window.reloadWithToast(result.message || 'Delivery details deleted.');
        else window.showToast(result.message || 'Unable to delete delivery details.', 'error');
    }

    async function deleteSelectedDeliveries(event) {
        event.preventDefault();
        const ids = event.detail?.ids ?? [];
        if (!ids.length || !confirm(`Delete ${ids.length} selected delivery details? Received product quantities will remain unchanged.`)) return;

        const response = await fetch(page.dataset.bulkDeleteUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(ids)
        });
        const result = await response.json();
        if (result.success) window.reloadWithToast(result.message || 'Selected delivery details deleted.');
        else window.showToast(result.message || 'Unable to delete the selected delivery details.', 'error');
    }

    search.addEventListener('input', () => { currentPage = 1; render(); });
    pageSize.addEventListener('change', () => { currentPage = 1; render(); });
    rows.forEach(row => row.addEventListener('click', event => {
        if (!event.target.closest('a, button, input, label, .dropdown-menu')) window.location.href = row.dataset.href;
    }));
    document.querySelectorAll('.delete-delivery').forEach(button => button.addEventListener('click', () => deleteDelivery(button)));
    table?.addEventListener('table:bulk-delete', deleteSelectedDeliveries);
    table?.addEventListener('table:sorted', () => {
        rows = [...document.querySelectorAll('.delivery-row')];
        currentPage = 1;
        render();
    });
    document.getElementById('exportDeliveries')?.addEventListener('click', event => {
        window.rjtechExcelExport.download(event.currentTarget, filteredRows(), Boolean(search.value.trim()));
    });
    render();
})();
