(() => {
    const page = document.getElementById('deliveryManagementPage');
    const searchInput = document.getElementById('deliverySearch');
    const tabs = [...document.querySelectorAll('[data-delivery-tab]')];

    let activeList = 'active';

    function createList(prefix) {
        return {
            prefix,
            panel: document.getElementById(`${prefix}DeliveryPanel`),
            rows: [...document.querySelectorAll(`.${prefix}-delivery-row`)],
            emptyRow: document.getElementById(`${prefix}DeliveryEmpty`),
            pageSize: document.getElementById(`${prefix}DeliveryPageSize`),
            pagination: document.getElementById(`${prefix}DeliveryPagination`),
            rangeText: document.getElementById(`${prefix}DeliveryRangeText`),
            rangeBar: document.getElementById(`${prefix}DeliveryRangeBar`),
            currentPage: 1
        };
    }

    const lists = {
        active: createList('active'),
        archive: createList('archive')
    };

    function filteredRows(list) {
        const searchTerm = searchInput.value.trim().toLowerCase();
        return list.rows.filter(row => !searchTerm || row.dataset.search.includes(searchTerm));
    }

    function addPageButton(list, label, targetPage, options = {}) {
        const item = document.createElement('li');
        const button = document.createElement('button');

        item.className = `page-item${options.disabled ? ' disabled' : ''}${options.active ? ' active' : ''}`;
        button.className = 'page-link';
        button.type = 'button';
        button.innerHTML = label;
        button.disabled = options.disabled ?? false;
        button.addEventListener('click', () => {
            list.currentPage = targetPage;
            renderList(list);
        });

        item.appendChild(button);
        list.pagination.appendChild(item);
    }

    function renderPagination(list, pageCount) {
        list.pagination.innerHTML = '';
        addPageButton(list, '<i class="bi bi-chevron-left"></i>', list.currentPage - 1, {
            disabled: list.currentPage === 1
        });

        for (let pageNumber = 1; pageNumber <= pageCount; pageNumber += 1) {
            addPageButton(list, String(pageNumber), pageNumber, {
                active: pageNumber === list.currentPage
            });
        }

        addPageButton(list, '<i class="bi bi-chevron-right"></i>', list.currentPage + 1, {
            disabled: list.currentPage === pageCount
        });
    }

    function renderList(list) {
        const matchingRows = filteredRows(list);
        const pageSize = Number(list.pageSize.value);
        const pageCount = Math.max(1, Math.ceil(matchingRows.length / pageSize));

        list.currentPage = Math.min(list.currentPage, pageCount);
        list.rows.forEach(row => row.classList.add('d-none'));

        const firstIndex = (list.currentPage - 1) * pageSize;
        matchingRows
            .slice(firstIndex, firstIndex + pageSize)
            .forEach(row => row.classList.remove('d-none'));

        list.emptyRow.classList.toggle('d-none', matchingRows.length > 0);

        const firstItem = matchingRows.length ? firstIndex + 1 : 0;
        const lastItem = Math.min(firstIndex + pageSize, matchingRows.length);
        const progress = matchingRows.length ? (lastItem / matchingRows.length) * 100 : 0;

        list.rangeText.textContent = `Showing ${firstItem}–${lastItem} of ${matchingRows.length} items`;
        list.rangeBar.style.width = `${progress}%`;
        renderPagination(list, pageCount);
    }

    function selectList(listName) {
        activeList = listName;

        tabs.forEach(tab => {
            const selected = tab.dataset.deliveryTab === listName;
            tab.classList.toggle('active', selected);
            tab.setAttribute('aria-selected', String(selected));
        });

        Object.entries(lists).forEach(([name, list]) => {
            list.panel.classList.toggle('d-none', name !== listName);
        });

        lists[listName].currentPage = 1;
        renderList(lists[listName]);
    }

    async function postDeliveryAction(url, deliveryId) {
        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ delivery_ID: deliveryId })
        });

        return response.json();
    }

    async function archiveDelivery(button) {
        if (!confirm(`Archive ${button.dataset.code}?`)) {
            return;
        }

        const result = await postDeliveryAction(page.dataset.archiveUrl, button.dataset.id);
        if (result.success) {
            window.reloadWithToast(result.message || 'Delivery archived.');
        }
    }

    async function deleteDelivery(button) {
        const confirmed = confirm(
            `Delete ${button.dataset.code}? Inventory quantities from this receipt will be rolled back.`
        );
        if (!confirmed) {
            return;
        }

        const result = await postDeliveryAction(page.dataset.deleteUrl, button.dataset.id);
        if (result.success) {
            window.reloadWithToast(result.message || 'Delivery receipt deleted.');
        }
    }

    function downloadCsv() {
        const list = lists[activeList];
        const csvRows = [['Delivery ID', 'Batch ID', 'Received By', 'Date', 'Products', 'Units', 'Status']];

        filteredRows(list).forEach(row => {
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
            download: activeList === 'archive' ? 'rjtech-delivery-archive.csv' : 'rjtech-deliveries.csv'
        });

        link.click();
        URL.revokeObjectURL(url);
    }

    searchInput.addEventListener('input', () => {
        lists[activeList].currentPage = 1;
        renderList(lists[activeList]);
    });

    Object.values(lists).forEach(list => {
        list.pageSize.addEventListener('change', () => {
            list.currentPage = 1;
            renderList(list);
        });
    });

    tabs.forEach(tab => {
        tab.addEventListener('click', () => selectList(tab.dataset.deliveryTab));
    });

    document.querySelectorAll('.delivery-row').forEach(row => {
        row.addEventListener('click', event => {
            if (!event.target.closest('a, button, .dropdown-menu')) {
                location.href = row.dataset.href;
            }
        });
    });

    document.querySelectorAll('.archive-delivery').forEach(button => {
        button.addEventListener('click', () => archiveDelivery(button));
    });
    document.querySelectorAll('.delete-delivery').forEach(button => {
        button.addEventListener('click', () => deleteDelivery(button));
    });
    document.getElementById('exportDeliveries').addEventListener('click', downloadCsv);

    renderList(lists.active);
    renderList(lists.archive);
})();
