(() => {
    const pageElement = document.getElementById('productManagementPage');
    const productRows = [...document.querySelectorAll('.product-row')];
    const searchInput = document.getElementById('productSearch');
    const emptyRow = document.getElementById('emptyProducts');
    const pageSizeSelect = document.getElementById('productPageSize');
    const pagination = document.getElementById('productPagination');
    const statusTabs = [...document.querySelectorAll('.product-status-tabs [data-status]')];

    const categoryPicker = document.getElementById('categoryPicker');
    const categoryInput = document.getElementById('categoryPickerInput');
    const categoryPanel = document.getElementById('categoryPickerPanel');
    const categorySearch = document.getElementById('categoryLiveSearch');
    const categoryCheckboxes = [...document.querySelectorAll('.category-checkbox')];

    let selectedStatus = 'all';
    let currentPage = 1;

    function getSelectedCategories() {
        return new Set(
            categoryCheckboxes
                .filter(checkbox => checkbox.checked)
                .map(checkbox => checkbox.value)
        );
    }

    function getFilteredRows() {
        const searchTerm = searchInput.value.trim().toLowerCase();
        const selectedCategories = getSelectedCategories();

        return productRows.filter(row => {
            const matchesSearch = !searchTerm || row.dataset.search.includes(searchTerm);
            const matchesStatus = selectedStatus === 'all' || row.dataset.status === selectedStatus;
            const matchesCategory = !selectedCategories.size || selectedCategories.has(row.dataset.category);

            return matchesSearch && matchesStatus && matchesCategory;
        });
    }

    function addPageButton(label, targetPage, options = {}) {
        const item = document.createElement('li');
        const button = document.createElement('button');

        item.className = `page-item${options.disabled ? ' disabled' : ''}${options.active ? ' active' : ''}`;
        button.className = 'page-link';
        button.type = 'button';
        button.innerHTML = label;
        button.disabled = options.disabled ?? false;
        button.addEventListener('click', () => {
            currentPage = targetPage;
            renderProducts();
        });

        item.appendChild(button);
        pagination.appendChild(item);
    }

    function renderPagination(pageCount) {
        pagination.innerHTML = '';
        addPageButton('<i class="bi bi-chevron-left"></i>', currentPage - 1, {
            disabled: currentPage === 1
        });

        for (let page = 1; page <= pageCount; page += 1) {
            addPageButton(String(page), page, { active: page === currentPage });
        }

        addPageButton('<i class="bi bi-chevron-right"></i>', currentPage + 1, {
            disabled: currentPage === pageCount
        });
    }

    function renderProducts() {
        const filteredRows = getFilteredRows();
        const pageSize = Number(pageSizeSelect.value);
        const pageCount = Math.max(1, Math.ceil(filteredRows.length / pageSize));

        currentPage = Math.min(currentPage, pageCount);
        productRows.forEach(row => row.classList.add('d-none'));

        const firstIndex = (currentPage - 1) * pageSize;
        filteredRows
            .slice(firstIndex, firstIndex + pageSize)
            .forEach(row => row.classList.remove('d-none'));

        emptyRow.classList.toggle('d-none', filteredRows.length > 0);

        const firstItem = filteredRows.length ? firstIndex + 1 : 0;
        const lastItem = Math.min(firstIndex + pageSize, filteredRows.length);
        const progress = filteredRows.length ? (lastItem / filteredRows.length) * 100 : 0;

        document.getElementById('productRangeText').textContent =
            `Showing ${firstItem}–${lastItem} of ${filteredRows.length} items`;
        document.getElementById('productRangeBar').style.width = `${progress}%`;
        renderPagination(pageCount);
    }

    function openCategoryPicker() {
        categoryPanel.classList.remove('d-none');
        categoryInput.setAttribute('aria-expanded', 'true');
        setTimeout(() => categorySearch.focus(), 0);
    }

    function updateCategorySelection() {
        const selectedCount = categoryCheckboxes.filter(checkbox => checkbox.checked).length;
        const selectionLabel = selectedCount === 1 ? '1 category selected' : `${selectedCount} categories selected`;

        categoryInput.value = selectedCount ? selectionLabel : '';
        document.getElementById('categorySelectedCount').textContent =
            selectedCount ? `${selectedCount} selected` : 'All categories';
        currentPage = 1;
        renderProducts();
    }

    async function deleteProduct(button, event) {
        event.stopPropagation();

        if (!confirm(`Delete ${button.dataset.name}? This cannot be undone.`)) {
            return;
        }

        const response = await fetch(pageElement.dataset.deleteUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ id: button.dataset.id })
        });
        const result = await response.json();

        if (result.success) {
            location.reload();
            return;
        }

        window.showToast(result.message || 'Unable to delete product.', 'error');
    }

    function downloadProductsCsv() {
        const csvRows = [[
            'Product Code', 'Product', 'Brand', 'Category',
            'Status', 'Stock', 'Reorder Level', 'Selling Price'
        ]];

        getFilteredRows().forEach(row => {
            const cells = row.querySelectorAll('td');
            csvRows.push([
                cells[0].innerText,
                cells[1].querySelector('.product-name').innerText,
                cells[1].querySelector('.product-meta').innerText,
                cells[2].innerText,
                cells[3].innerText,
                cells[4].innerText,
                cells[5].innerText,
                cells[6].innerText
            ]);
        });

        const csv = csvRows
            .map(row => row.map(value => `"${String(value).replaceAll('"', '""')}"`).join(','))
            .join('\n');
        const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }));
        const link = Object.assign(document.createElement('a'), {
            href: url,
            download: 'rjtech-products.csv'
        });

        link.click();
        URL.revokeObjectURL(url);
    }

    searchInput.addEventListener('input', () => {
        currentPage = 1;
        renderProducts();
    });
    pageSizeSelect.addEventListener('change', () => {
        currentPage = 1;
        renderProducts();
    });

    statusTabs.forEach(tab => {
        tab.addEventListener('click', () => {
            statusTabs.forEach(item => item.classList.remove('active'));
            tab.classList.add('active');
            selectedStatus = tab.dataset.status;
            currentPage = 1;
            renderProducts();
        });
    });

    categoryInput.addEventListener('click', openCategoryPicker);
    categoryInput.addEventListener('focus', openCategoryPicker);
    categorySearch.addEventListener('input', () => {
        const searchTerm = categorySearch.value.trim().toLowerCase();
        document.querySelectorAll('.category-option').forEach(option => {
            option.classList.toggle('d-none', !option.dataset.categoryName.includes(searchTerm));
        });
    });
    categoryCheckboxes.forEach(checkbox => checkbox.addEventListener('change', updateCategorySelection));
    document.getElementById('clearCategories').addEventListener('click', () => {
        categoryCheckboxes.forEach(checkbox => { checkbox.checked = false; });
        updateCategorySelection();
    });
    document.addEventListener('click', event => {
        if (!categoryPicker.contains(event.target)) {
            categoryPanel.classList.add('d-none');
            categoryInput.setAttribute('aria-expanded', 'false');
        }
    });

    productRows.forEach(row => {
        row.addEventListener('click', event => {
            if (!event.target.closest('a, button, .dropdown-menu')) {
                location.href = row.dataset.href;
            }
        });
    });
    document.querySelectorAll('.delete-product').forEach(button => {
        button.addEventListener('click', event => deleteProduct(button, event));
    });
    // document.getElementById('exportProducts').addEventListener('click', downloadProductsCsv);

    renderProducts();
})();
