(() => {
    const pageElement = document.getElementById('productManagementPage');
    let productRows = [...document.querySelectorAll('.product-row')];
    const table = document.getElementById('productTable');
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

    const availabilityModalElement = document.getElementById('productAvailabilityModal');
    const availabilityModal = availabilityModalElement
        ? bootstrap.Modal.getOrCreateInstance(availabilityModalElement)
        : null;
    const availabilityTitle = document.getElementById('productAvailabilityTitle');
    const availabilitySubtitle = document.getElementById('productAvailabilitySubtitle');
    const availabilityMessage = document.getElementById('productAvailabilityMessage');
    const availabilityIcon = document.getElementById('productAvailabilityIcon');
    const availabilityConfirm = document.getElementById('confirmProductAvailability');

    let selectedStatus = 'all';
    let currentPage = 1;
    let pendingAvailabilityButton = null;

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

    function toggleProductAvailability(button, event) {
        event.stopPropagation();

        if (!availabilityModal) {
            return;
        }

        pendingAvailabilityButton = button;
        const isActivate = button.dataset.action === 'activate';

        availabilityTitle.textContent = isActivate ? 'Activate product' : 'Deactivate product';
        availabilitySubtitle.textContent = isActivate
            ? 'This product will be available for sales again.'
            : 'This product will be hidden from sales.';
        availabilityMessage.textContent = isActivate
            ? `Activate ${button.dataset.name}? It will become available for sales again.`
            : `Deactivate ${button.dataset.name}? It will remain in the system with the status "Unavailable" and will no longer appear in sales.`;
        availabilityIcon.className = isActivate ? 'bi bi-check-circle' : 'bi bi-exclamation-triangle';
        availabilityConfirm.textContent = isActivate ? 'Activate' : 'Deactivate';
        availabilityConfirm.classList.toggle('btn-danger', !isActivate);
        availabilityConfirm.classList.toggle('btn-success', isActivate);

        availabilityModal.show();
    }

    async function confirmProductAvailability() {
        const button = pendingAvailabilityButton;
        if (!button) {
            return;
        }

        const isActivate = button.dataset.action === 'activate';
        const verb = isActivate ? 'Activate' : 'Deactivate';
        const url = isActivate ? pageElement.dataset.activateUrl : pageElement.dataset.deactivateUrl;

        availabilityConfirm.disabled = true;

        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams({ id: button.dataset.id }),
                skipToast: true
            });
            const result = await response.json();

            if (result.success) {
                window.reloadWithToast(result.message || `Product ${verb.toLowerCase()}d.`);
                return;
            }

            availabilityModal.hide();
            window.showToast(result.message || `Unable to ${verb.toLowerCase()} product.`, 'error');
        } finally {
            availabilityConfirm.disabled = false;
        }
    }

    async function deactivateSelectedProducts(event) {
        event.preventDefault();
        const ids = event.detail?.ids ?? [];
        if (!ids.length || !confirm(`Deactivate ${ids.length} selected products? They will be unavailable for sales.`)) return;

        const response = await fetch(pageElement.dataset.bulkDeactivateUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(ids)
        });
        const result = await response.json();
        if (result.success) {
            window.reloadWithToast(result.message || 'Selected products deactivated.');
            return;
        }
        window.showToast(result.message || 'Unable to deactivate the selected products.', 'error');
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
    document.querySelectorAll('.deactivate-product, .activate-product').forEach(button => {
        button.addEventListener('click', event => toggleProductAvailability(button, event));
    });
    availabilityConfirm?.addEventListener('click', confirmProductAvailability);
    availabilityModalElement?.addEventListener('hidden.bs.modal', () => {
        pendingAvailabilityButton = null;
    });
    table?.addEventListener('table:bulk-delete', deactivateSelectedProducts);
    table?.addEventListener('table:sorted', () => {
        productRows = [...document.querySelectorAll('.product-row')];
        currentPage = 1;
        renderProducts();
    });
    document.getElementById('exportProducts')?.addEventListener('click', event => {
        const hasFilters = Boolean(searchInput.value.trim()) || selectedStatus !== 'all' || getSelectedCategories().size > 0;
        window.rjtechExcelExport.download(event.currentTarget, getFilteredRows(), hasFilters);
    });

    renderProducts();
})();
