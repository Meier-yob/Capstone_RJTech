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

    let hasUnsavedChanges = false;
    let requestedExitUrl = '';

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

    function addBulkRow() {
        bulkRows.appendChild(bulkRowTemplate.content.cloneNode(true));
        markAsChanged();
    }

    function removeBulkRow(event) {
        const removeButton = event.target.closest('.remove-bulk-row');
        if (!removeButton) {
            return;
        }

        removeButton.closest('tr').remove();
        if (!bulkRows.children.length) {
            addBulkRow();
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
            Product_price: Number(row.querySelector('.bulk-price').value),
            product_quantity: Number(row.querySelector('.bulk-qty').value),
            reorder_level: Number(row.querySelector('.bulk-reorder').value)
        };
    }

    async function createProduct(event) {
        event.preventDefault();

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
                location.href = result.redirectUrl || page.dataset.productsUrl;
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
                location.href = page.dataset.productsUrl;
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

    createForm.addEventListener('input', markAsChanged);
    createForm.addEventListener('change', markAsChanged);
    createForm.addEventListener('submit', createProduct);
    bulkModal.addEventListener('input', markAsChanged);
    bulkModal.addEventListener('change', markAsChanged);
    bulkRows.addEventListener('click', removeBulkRow);
    document.getElementById('addBulkRow').addEventListener('click', addBulkRow);
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
