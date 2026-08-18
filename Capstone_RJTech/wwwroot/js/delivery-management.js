(() => {
    const page = document.getElementById('deliveryManagementPage');
    const rows = [...document.querySelectorAll('.delivery-row')];
    const searchInput = document.getElementById('deliverySearch');
    const emptyRow = document.getElementById('emptyDeliveries');
    const statusTabs = [...document.querySelectorAll('.filter-tabs [data-status]')];
    let selectedStatus = 'all';

    function applyFilters() {
        const searchTerm = searchInput.value.trim().toLowerCase();
        let visibleCount = 0;

        rows.forEach(row => {
            const matchesSearch = !searchTerm || row.dataset.search.includes(searchTerm);
            const matchesStatus = selectedStatus === 'all' || row.dataset.status === selectedStatus;
            const isVisible = matchesSearch && matchesStatus;

            row.classList.toggle('d-none', !isVisible);
            visibleCount += isVisible ? 1 : 0;
        });

        emptyRow.classList.toggle('d-none', visibleCount > 0);
    }

    async function deleteDelivery(button) {
        const confirmed = confirm(
            `Delete ${button.dataset.code}? Inventory quantities from this receipt will be rolled back.`
        );
        if (!confirmed) {
            return;
        }

        const response = await fetch(page.dataset.deleteUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ delivery_ID: button.dataset.id })
        });
        const result = await response.json();

        if (result.success) {
            location.reload();
            return;
        }

        window.showToast(result.message || 'Unable to delete delivery.', 'error');
    }

    function downloadCsv() {
        const csvRows = [['Delivery ID', 'Batch ID', 'Received By', 'Date', 'Products', 'Units', 'Status']];

        rows.filter(row => !row.classList.contains('d-none')).forEach(row => {
            const values = [...row.querySelectorAll('td')]
                .slice(0, 7)
                .map(cell => cell.innerText.trim());
            csvRows.push(values);
        });

        const csv = csvRows
            .map(row => row.map(value => `"${value.replaceAll('"', '""')}"`).join(','))
            .join('\n');
        const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }));
        const link = Object.assign(document.createElement('a'), {
            href: url,
            download: 'rjtech-deliveries.csv'
        });

        link.click();
        URL.revokeObjectURL(url);
    }

    searchInput.addEventListener('input', applyFilters);
    document.getElementById('exportDeliveries').addEventListener('click', downloadCsv);

    statusTabs.forEach(tab => {
        tab.addEventListener('click', () => {
            statusTabs.forEach(item => item.classList.remove('active'));
            tab.classList.add('active');
            selectedStatus = tab.dataset.status;
            applyFilters();
        });
    });

    document.querySelectorAll('.delete-delivery').forEach(button => {
        button.addEventListener('click', () => deleteDelivery(button));
    });
})();
