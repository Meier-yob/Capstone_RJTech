(() => {
    const page = document.getElementById('newProductPage');
    const createForm = document.getElementById('createProductForm');
    const submitButton = document.getElementById('submitProduct');
    const bulkModal = document.getElementById('bulkAddModal');
    const bulkRows = document.getElementById('bulkRows');
    const bulkRowTemplate = document.getElementById('bulkRowTemplate');
    const bulkSaveButton = document.getElementById('saveBulkProducts');
    const bulkErrors = document.getElementById('bulkErrors');
    const exitModal = new bootstrap.Modal(document.getElementById('confirmExitModal'));

    const categoryIdHidden = document.getElementById('categoryIdHidden');
    const categoryTrigger = document.getElementById('categorySearchTrigger');
    const categoryPanel = document.getElementById('categorySearchPanel');
    const categoryInput = document.getElementById('categorySearchInput');
    const categoryList = document.getElementById('categorySearchList');
    const categoryText = document.getElementById('categorySelectedText');
    const categoryEmpty = document.getElementById('categorySearchEmpty');

    let hasUnsavedChanges = false;
    let requestedExitUrl = '';
    let categoryHighlightIndex = -1;

    function markAsChanged() {
        hasUnsavedChanges = true;
    }

    function setLoading(button, isLoading) {
        button.disabled = isLoading;
        button.querySelector('.spinner-border').classList.toggle('d-none', !isLoading);
    }

    function showBulkError(message) {
        bulkErrors.textContent = message;
        bulkErrors.classList.remove('d-none');
    }

    function renumberBulkRows() {
        [...bulkRows.querySelectorAll('.bulk-row')].forEach((row, index) => {
            const number = row.querySelector('.bulk-row-number');
            if (number) number.textContent = String(index + 1);
        });
    }

    function addBulkRow(options = {}) {
        const fragment = bulkRowTemplate.content.cloneNode(true);
        const row = fragment.querySelector('.bulk-row');
        if (options.after) {
            options.after.after(fragment);
        } else {
            bulkRows.appendChild(fragment);
        }
        renumberBulkRows();
        markAsChanged();
        return row;
    }

    function removeBulkRow(event) {
        const removeButton = event.target.closest('.remove-bulk-row');
        if (!removeButton) {
            return;
        }

        removeButton.closest('tr').remove();
        if (!bulkRows.children.length) {
            addBulkRow();
        } else {
            renumberBulkRows();
        }
    }

    function duplicateBulkRow(event) {
        const duplicateButton = event.target.closest('.duplicate-bulk-row');
        if (!duplicateButton) {
            return;
        }

        const source = duplicateButton.closest('.bulk-row');
        const newRow = addBulkRow({ after: source });
        newRow.querySelector('.bulk-category').value = source.querySelector('.bulk-category').value;
        newRow.querySelector('.bulk-brand').value = source.querySelector('.bulk-brand').value;
        newRow.querySelector('.bulk-reorder').value = source.querySelector('.bulk-reorder').value;
        newRow.querySelector('.bulk-name').focus();
    }

    function isLastFieldOfLastRow(target) {
        const rows = [...bulkRows.querySelectorAll('.bulk-row')];
        const row = target.closest('.bulk-row');
        if (!row || rows[rows.length - 1] !== row) {
            return false;
        }
        const fields = [...row.querySelectorAll('input, select')];
        return fields[fields.length - 1] === target;
    }

    function advanceBulkRow(event) {
        const isTab = event.key === 'Tab' && !event.shiftKey;
        const isEnter = event.key === 'Enter';
        if ((isTab || isEnter) && isLastFieldOfLastRow(event.target)) {
            event.preventDefault();
            const newRow = addBulkRow();
            newRow.querySelector('.bulk-category').focus();
        }
    }

    function findInvalidBulkInput(rows) {
        return rows
            .flatMap(row => [...row.querySelectorAll('[required]')])
            .find(input => !input.checkValidity());
    }

    function readBulkProduct(row) {
        return {
            category_ID: Number(row.querySelector('.bulk-category').value),
            product_name: row.querySelector('.bulk-name').value,
            product_brand: row.querySelector('.bulk-brand').value,
            IsSerialized: row.querySelector('.bulk-serialized').checked,
            Product_price: Number(row.querySelector('.bulk-price').value),
            reorder_level: Number(row.querySelector('.bulk-reorder').value)
        };
    }

    async function createProduct(event) {
        event.preventDefault();

        if (!categoryIdHidden.value) {
            setCategoryError(true);
            return;
        }

        if (!createForm.checkValidity()) {
            createForm.classList.add('was-validated');
            return;
        }

        setLoading(submitButton, true);

        try {
            const response = await fetch(createForm.action, {
                method: 'POST',
                body: new FormData(createForm)
            });
            const result = await response.json();

            if (result.success) {
                hasUnsavedChanges = false;
                window.redirectWithToast(
                    result.message || 'Product created successfully.',
                    'success',
                    result.redirectUrl || page.dataset.productsUrl
                );
                return;
            }

            const messages = result.errors || [result.message || 'Unable to create product.'];
            window.showToast(messages.join(' '), 'error');
        } catch {
            window.showToast('Unable to create product.', 'error');
        } finally {
            setLoading(submitButton, false);
        }
    }

    async function createBulkProducts() {
        const rowElements = [...bulkRows.querySelectorAll('.bulk-row')];
        const invalidInput = findInvalidBulkInput(rowElements);

        if (invalidInput) {
            invalidInput.reportValidity();
            return;
        }

        const products = rowElements.map(readBulkProduct);
        bulkErrors.classList.add('d-none');
        setLoading(bulkSaveButton, true);

        try {
            const response = await fetch(page.dataset.bulkCreateUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ products })
            });
            const result = await response.json();

            if (result.success) {
                hasUnsavedChanges = false;
                window.redirectWithToast(
                    result.message || 'Products created successfully.',
                    'success',
                    page.dataset.productsUrl
                );
                return;
            }

            showBulkError((result.errors || [result.message]).join(' '));
        } catch {
            showBulkError('Unable to add products.');
        } finally {
            setLoading(bulkSaveButton, false);
        }
    }

    function guardNavigation(event) {
        const link = event.target.closest('a[href]');
        if (!hasUnsavedChanges || !link) {
            return;
        }

        const href = link.getAttribute('href');
        if (href.startsWith('#') || link.target === '_blank') {
            return;
        }

        event.preventDefault();
        requestedExitUrl = link.href;
        exitModal.show();
    }

    function setCategoryError(show) {
        const errorElement = document.querySelector('[data-valmsg-for="category_ID"]');
        if (errorElement) {
            errorElement.textContent = show ? 'Select a category.' : '';
        }
        categoryTrigger.classList.toggle('is-invalid', show);
        if (show) {
            categoryTrigger.focus();
        }
    }

    function categoryIsOpen() {
        return !categoryPanel.classList.contains('d-none');
    }

    function categoryOpen() {
        categoryPanel.classList.remove('d-none');
        categoryTrigger.setAttribute('aria-expanded', 'true');
        categoryHighlightIndex = -1;
        categoryInput.value = '';
        categoryFilter();
        categoryInput.focus();
    }

    function categoryClose() {
        categoryPanel.classList.add('d-none');
        categoryTrigger.setAttribute('aria-expanded', 'false');
    }

    function categorySelect(categoryId, categoryName) {
        categoryIdHidden.value = categoryId;
        categoryText.textContent = categoryName;
        categoryText.classList.remove('is-placeholder');
        categoryIdHidden.dispatchEvent(new Event('change', { bubbles: true }));
        setCategoryError(false);
    }

    function categoryFilter() {
        const term = categoryInput.value.trim().toLowerCase();
        let visible = 0;
        categoryHighlightIndex = -1;
        [...categoryList.querySelectorAll('li[data-category-id]')].forEach(option => {
            const matches = option.dataset.categoryName.toLowerCase().includes(term);
            option.classList.toggle('d-none', !matches);
            option.classList.remove('is-highlighted');
            if (matches) {
                visible++;
            }
        });
        categoryEmpty.classList.toggle('d-none', visible > 0);
    }

    function categoryVisibleOptions() {
        return [...categoryList.querySelectorAll('li[data-category-id]:not(.d-none)')];
    }

    function categoryMove(step) {
        const options = categoryVisibleOptions();
        if (!options.length) {
            return;
        }

        options.forEach(option => option.classList.remove('is-highlighted'));
        categoryHighlightIndex = (categoryHighlightIndex + step + options.length) % options.length;
        const target = options[categoryHighlightIndex];
        target.classList.add('is-highlighted');
        target.scrollIntoView({ block: 'nearest' });
    }

    categoryTrigger.addEventListener('click', event => {
        event.stopPropagation();
        if (categoryIsOpen()) {
            categoryClose();
        } else {
            categoryOpen();
        }
    });

    categoryTrigger.addEventListener('keydown', event => {
        if (event.key === 'ArrowDown' || event.key === 'ArrowUp' || event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            event.stopPropagation();
            if (!categoryIsOpen()) {
                categoryOpen();
            } else if (event.key === 'ArrowDown') {
                categoryMove(1);
            } else if (event.key === 'ArrowUp') {
                categoryMove(-1);
            } else if (event.key === 'Enter') {
                const highlighted = categoryList.querySelector('li.is-highlighted[data-category-id]');
                if (highlighted) {
                    categorySelect(highlighted.dataset.categoryId, highlighted.dataset.categoryName);
                    categoryClose();
                } else {
                    categoryClose();
                }
            }
        } else if (event.key === 'Escape' && categoryIsOpen()) {
            categoryClose();
        }
    });

    categoryInput.addEventListener('input', categoryFilter);

    categoryInput.addEventListener('keydown', event => {
        if (event.key === 'ArrowDown') {
            event.preventDefault();
            categoryMove(1);
        } else if (event.key === 'ArrowUp') {
            event.preventDefault();
            categoryMove(-1);
        } else if (event.key === 'Enter') {
            event.preventDefault();
            const highlighted = categoryList.querySelector('li.is-highlighted[data-category-id]');
            if (highlighted) {
                categorySelect(highlighted.dataset.categoryId, highlighted.dataset.categoryName);
                categoryClose();
            }
        } else if (event.key === 'Escape') {
            categoryClose();
            categoryTrigger.focus();
        }
    });

    categoryList.addEventListener('click', event => {
        const option = event.target.closest('li[data-category-id]');
        if (!option) {
            return;
        }
        categorySelect(option.dataset.categoryId, option.dataset.categoryName);
        categoryClose();
        categoryTrigger.focus();
    });

    document.addEventListener('click', event => {
        if (categoryIsOpen() && !event.target.closest('.category-search')) {
            categoryClose();
        }
    });

    createForm.addEventListener('input', markAsChanged);
    createForm.addEventListener('change', markAsChanged);
    createForm.addEventListener('submit', createProduct);
    bulkModal.addEventListener('input', markAsChanged);
    bulkModal.addEventListener('change', markAsChanged);
    bulkRows.addEventListener('click', event => {
        if (event.target.closest('.remove-bulk-row')) {
            removeBulkRow(event);
        } else if (event.target.closest('.duplicate-bulk-row')) {
            duplicateBulkRow(event);
        }
    });
    bulkRows.addEventListener('keydown', advanceBulkRow);
    document.getElementById('addBulkRow').addEventListener('click', () => {
        addBulkRow().querySelector('.bulk-category').focus();
    });
    bulkSaveButton.addEventListener('click', createBulkProducts);

    document.addEventListener('click', guardNavigation);
    document.getElementById('discardAndLeave').addEventListener('click', () => {
        hasUnsavedChanges = false;
        location.href = requestedExitUrl || page.dataset.productsUrl;
    });
    window.addEventListener('beforeunload', event => {
        if (hasUnsavedChanges) {
            event.preventDefault();
            event.returnValue = '';
        }
    });

    addBulkRow();
    hasUnsavedChanges = false;
})();
