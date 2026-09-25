(() => {
    const page = document.getElementById('categoryManagementPage');
    if (!page) return;
    const categoryList = document.getElementById('categoryList');
    const categorySearch = document.getElementById('searchCategoryInput');
    const productSearch = document.getElementById('searchProductInput');
    const table = document.getElementById('categoryProductsTable');
    let productRows = [...table.querySelectorAll('.product-row-item')];
    let categoryItems = [...categoryList.querySelectorAll('.category-item')];
    const pageSize = document.getElementById('categoryPageSize');
    const pagination = document.getElementById('categoryPagination');
    const editModalElement = document.getElementById('editCategoryModal');
    const editModal = bootstrap.Modal.getOrCreateInstance(editModalElement);
    let selectedCategoryId = 'all';
    let currentPage = 1;

    function addPageButton(label, targetPage, disabled = false, active = false) {
        const item = document.createElement('li');
        item.className = 'page-item';
        item.classList.toggle('active', active);
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'page-link';
        button.textContent = label;
        button.disabled = disabled;
        if (active) button.setAttribute('aria-current', 'page');
        button.addEventListener('click', () => { currentPage = targetPage; renderProducts(); });
        item.appendChild(button);
        pagination.appendChild(item);
    }

    function renderProducts() {
        const searchTerm = productSearch.value.trim().toLowerCase();
        const categoryRows = productRows.filter(row => selectedCategoryId === 'all' || row.dataset.categoryId === selectedCategoryId);
        const matches = categoryRows.filter(row => !searchTerm || row.dataset.search.includes(searchTerm));
        const size = Number(pageSize.value);
        const pages = Math.max(1, Math.ceil(matches.length / size));
        currentPage = Math.min(currentPage, pages);
        const offset = (currentPage - 1) * size;
        const visible = new Set(matches.slice(offset, offset + size));
        productRows.forEach(row => row.classList.toggle('d-none', !visible.has(row)));
        document.getElementById('selectedProductCount').textContent = categoryRows.length + (categoryRows.length === 1 ? ' product' : ' products');
        document.getElementById('noProductsRow').classList.toggle('d-none', matches.length > 0);
        document.getElementById('emptyProductsTitle').textContent = searchTerm ? 'No matching products.' : 'No products in this category.';
        document.getElementById('emptyProductsHelp').textContent = searchTerm
            ? 'Try another product name, code, or brand.' : 'Products assigned to this category will appear here.';
        document.getElementById('categoryRangeText').textContent = matches.length
            ? `Showing ${offset + 1}–${Math.min(offset + size, matches.length)} of ${matches.length} products`
            : 'Showing 0 products';
        pagination.replaceChildren();
        addPageButton('Previous', currentPage - 1, currentPage === 1);
        const first = Math.max(1, Math.min(currentPage - 2, pages - 4));
        for (let number = first; number <= Math.min(pages, first + 4); number++)
            addPageButton(String(number), number, false, number === currentPage);
        addPageButton('Next', currentPage + 1, currentPage === pages);
    }

    function selectCategory(categoryItem) {
        categoryItems.forEach(item => {
            const active = item === categoryItem;
            item.classList.toggle('active', active);
            item.closest('.category-entry').classList.toggle('active', active);
            item.setAttribute('aria-pressed', String(active));
        });
        selectedCategoryId = categoryItem.dataset.categoryId;
        document.getElementById('selectedCategoryHeader').textContent = categoryItem.dataset.categoryName;
        document.getElementById('selectedCategorySub').textContent = selectedCategoryId === 'all'
            ? 'Products across all categories' : categoryItem.dataset.categoryCode + ' · Products in this category';
        currentPage = 1;
        renderProducts();
    }

    // ===== Category modal: name validation ==================================

    const NAME_MAX = 50;
    const createForm = document.getElementById('createCategoryForm');
    const createInput = document.getElementById('createCategoryName');
    const createCounter = document.getElementById('createNameCounter');
    const createFeedback = document.getElementById('createNameFeedback');
    const createSaveBtn = document.getElementById('createSaveBtn');
    const createSaveAnother = document.getElementById('createSaveAnother');
    const editForm = document.getElementById('editCategoryForm');
    const editInput = document.getElementById('editCategoryName');
    const editCounter = document.getElementById('editNameCounter');
    const editFeedback = document.getElementById('editNameFeedback');
    const editSaveBtn = document.getElementById('editSaveBtn');

    let createInFlight = false;
    let editInFlight = false;

    const createConfig = {
        form: createForm,
        input: createInput,
        counter: createCounter,
        feedback: createFeedback,
        buttons: [createSaveBtn, createSaveAnother],
        excludeName: ''
    };

    const editConfig = {
        form: editForm,
        input: editInput,
        counter: editCounter,
        feedback: editFeedback,
        buttons: [editSaveBtn],
        excludeName: ''
    };

    function levenshteinDistance(a, b) {
        const m = a.length;
        const n = b.length;
        if (m === 0) return n;
        if (n === 0) return m;
        let prev = Array.from({ length: n + 1 }, (_, j) => j);
        for (let i = 1; i <= m; i++) {
            const curr = [i];
            for (let j = 1; j <= n; j++) {
                curr[j] = a[i - 1] === b[j - 1]
                    ? prev[j - 1]
                    : Math.min(prev[j], curr[j - 1], prev[j - 1]) + 1;
            }
            prev = curr;
        }
        return prev[n];
    }

    const existingCategoryNames = () => categoryItems
        .filter(item => item.dataset.categoryId !== 'all')
        .map(item => item.dataset.categoryName)
        .filter(Boolean);

    const normalizeForCompare = value => value.trim().replace(/\s+/g, ' ');

    /**
     * Returns { error, warning } for a typed name.
     * - Empty or > 50 characters -> error.
     * - Exact duplicate (ignoring case and extra spaces) -> error.
     * - Near match (e.g. "Keyboard" vs "Keyboards") -> warning.
     */
    function analyzeName(raw, excludeName = '') {
        const trimmed = raw.trim();
        const normalized = normalizeForCompare(trimmed);
        if (!normalized) return { error: '', warning: '' };
        if (normalized.length > NAME_MAX) {
            return { error: `Category name can't exceed ${NAME_MAX} characters.`, warning: '' };
        }

        const lower = normalized.toLowerCase();
        const excluded = excludeName.trim().toLowerCase();
        let duplicate = null;
        let near = null;

        for (const name of existingCategoryNames()) {
            const candidate = normalizeForCompare(name);
            if (candidate.toLowerCase() === excluded) continue;
            if (candidate.toLowerCase() === lower) { duplicate = name; break; }
        }
        if (!duplicate) {
            for (const name of existingCategoryNames()) {
                const candidate = normalizeForCompare(name);
                if (candidate.toLowerCase() === excluded) continue;
                if (levenshteinDistance(lower, candidate.toLowerCase()) === 1) { near = name; break; }
            }
        }

        if (duplicate) return { error: `A category named '${duplicate}' already exists.`, warning: '' };
        if (near) return { error: '', warning: `Similar to existing category "${near}" — did you mean this instead?` };
        return { error: '', warning: '' };
    }

    function updateField(config) {
        const count = config.input.value.length;
        config.counter.textContent = `${count}/${NAME_MAX}`;
        config.counter.classList.toggle('is-over', count > NAME_MAX);

        const { error, warning } = analyzeName(config.input.value, config.excludeName);
        config.feedback.textContent = error || warning || '';
        config.feedback.classList.toggle('is-error', Boolean(error));
        config.feedback.classList.toggle('is-warning', Boolean(warning) && !error);

        config.input.classList.toggle('is-invalid', Boolean(error));
        config.input.classList.toggle('is-valid', !error && config.input.value.trim().length > 0);

        const valid = !error && config.input.value.trim().length > 0;
        config.buttons.forEach(button => { button.disabled = !valid; });
        return valid;
    }

    function showServerError(form, message) {
        const alert = form.querySelector('.category-form-error');
        alert.textContent = message;
        alert.classList.remove('d-none');
    }

    function hideServerError(form) {
        form.querySelector('.category-form-error').classList.add('d-none');
    }

    function showSpinner(button) {
        const spinner = button.querySelector('.spinner-border');
        if (spinner) spinner.classList.remove('d-none');
    }

    function hideSpinner(button) {
        const spinner = button.querySelector('.spinner-border');
        if (spinner) spinner.classList.add('d-none');
    }

    async function postCategoryForm(form) {
        const response = await fetch(form.action, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams(new FormData(form))
        });
        if (!response.ok) throw new Error('Request failed.');
        return response.json();
    }

    // ===== Create modal ======================================================

    function escapeHtml(value) {
        return String(value).replace(/[&<>"']/g, ch => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        }[ch]));
    }

    function updateCreateCodePreview() {
        const preview = document.getElementById('createCodePreview');
        if (!preview) return;
        let maxId = 0;
        categoryItems.forEach(item => {
            const id = Number(item.dataset.categoryId);
            if (Number.isInteger(id)) maxId = Math.max(maxId, id);
        });
        preview.textContent = `CAT-${String(maxId + 1).padStart(3, '0')}`;
    }

    /** Inserts a freshly created category into the directory without a page reload. */
    function addCategoryItem(result) {
        const code = result.code || `CAT-${String(result.categoryId).padStart(3, '0')}`;
        const safeName = escapeHtml(result.categoryName);
        const entry = document.createElement('div');
        entry.className = 'category-entry category-wrapper';
        entry.innerHTML =
            '<button type="button" class="category-item" data-category-id="' + result.categoryId +
            '" data-category-name="' + safeName + '" data-category-code="' + code +
            '" aria-pressed="false" aria-controls="categoryProductsTable">' +
            '<span class="category-list-icon"><i class="bi bi-folder2" aria-hidden="true"></i></span>' +
            '<span class="category-copy"><strong class="category-title">' + safeName + '</strong><small>' + code + '</small></span>' +
            '<span class="category-product-count" aria-label="0 products">0</span>' +
            '</button>' +
            '<button type="button" class="category-edit edit-category-btn" data-id="' + result.categoryId +
            '" data-name="' + safeName + '" aria-label="Edit ' + safeName + ' category" title="Edit category">' +
            '<i class="bi bi-pencil" aria-hidden="true"></i></button>';

        const searchEmpty = document.getElementById('categorySearchEmpty');
        if (searchEmpty) categoryList.insertBefore(entry, searchEmpty);
        else categoryList.appendChild(entry);

        const listEmpty = document.getElementById('categoryListEmpty');
        if (listEmpty) listEmpty.remove();

        categoryItems = [...categoryList.querySelectorAll('.category-item')];
        const total = document.querySelector('#categoriesHeading .category-total');
        if (total) total.textContent = String((Number(total.textContent) || 0) + 1);
        updateCreateCodePreview();
    }

    function resetCreateForm() {
        createForm.reset();
        hideServerError(createForm);
        updateField(createConfig);
        updateCreateCodePreview();
    }

    async function handleCreateSubmit(button, keepOpen) {
        if (createInFlight || !updateField(createConfig)) return;
        createInFlight = true;
        createConfig.buttons.forEach(btn => { btn.disabled = true; });
        showSpinner(button);
        hideServerError(createForm);
        try {
            const result = await postCategoryForm(createForm);
            if (!result.success) {
                showServerError(createForm, result.message || 'Unable to save the category.');
                return;
            }
            if (keepOpen) {
                addCategoryItem(result);
                createForm.reset();
                updateField(createConfig);
                updateCreateCodePreview();
                createInput.focus();
            } else {
                window.reloadWithToast(result.message);
                return;
            }
        } catch {
            showServerError(createForm, 'Unable to save the category. Please try again.');
        } finally {
            createInFlight = false;
            createConfig.buttons.forEach(btn => { btn.disabled = false; });
            updateField(createConfig);
            hideSpinner(button);
        }
    }

    createForm.addEventListener('submit', event => {
        event.preventDefault();
        handleCreateSubmit(createSaveBtn, false);
    });
    createSaveAnother.addEventListener('click', () => handleCreateSubmit(createSaveAnother, true));
    createInput.addEventListener('input', () => updateField(createConfig));
    document.getElementById('createCategoryModal').addEventListener('show.bs.modal', resetCreateForm);

    // ===== Edit modal ========================================================

    async function handleEditSubmit() {
        if (editInFlight || !updateField(editConfig)) return;
        editInFlight = true;
        editConfig.buttons.forEach(btn => { btn.disabled = true; });
        showSpinner(editSaveBtn);
        hideServerError(editForm);
        try {
            const result = await postCategoryForm(editForm);
            if (!result.success) {
                showServerError(editForm, result.message || 'Unable to save the category.');
                return;
            }
            window.reloadWithToast(result.message);
            return;
        } catch {
            showServerError(editForm, 'Unable to save the category. Please try again.');
        } finally {
            editInFlight = false;
            editConfig.buttons.forEach(btn => { btn.disabled = false; });
            updateField(editConfig);
            hideSpinner(editSaveBtn);
        }
    }

    editForm.addEventListener('submit', event => {
        event.preventDefault();
        handleEditSubmit();
    });
    editInput.addEventListener('input', () => updateField(editConfig));

    // ===== List interactions =================================================

    categoryList.addEventListener('click', event => {
        const editButton = event.target.closest('.edit-category-btn');
        if (editButton) {
            document.getElementById('editCategoryID').value = editButton.dataset.id;
            editInput.value = editButton.dataset.name;
            editConfig.excludeName = editButton.dataset.name || '';
            hideServerError(editForm);
            updateField(editConfig);
            editModal.show();
            return;
        }
        const categoryItem = event.target.closest('.category-item');
        if (categoryItem) selectCategory(categoryItem);
    });

    categorySearch.addEventListener('input', () => {
        const term = categorySearch.value.trim().toLowerCase();
        let visible = 0;
        categoryList.querySelectorAll('.category-wrapper').forEach(wrapper => {
            const item = wrapper.querySelector('.category-item');
            const matches = (item.dataset.categoryName + ' ' + item.dataset.categoryCode).toLowerCase().includes(term);
            wrapper.classList.toggle('d-none', !matches);
            if (matches) visible++;
        });
        document.getElementById('categorySearchEmpty').classList.toggle('d-none', !term || visible > 0);
    });
    productSearch.addEventListener('input', () => { currentPage = 1; renderProducts(); });
    pageSize.addEventListener('change', () => { currentPage = 1; renderProducts(); });
    table.addEventListener('table:sorted', () => {
        productRows = [...table.querySelectorAll('.product-row-item')];
        currentPage = 1;
        renderProducts();
    });
    document.querySelectorAll('.category-modal').forEach(modal => {
        modal.addEventListener('shown.bs.modal', () => modal.querySelector('[name="category_name"]').focus());
    });
    renderProducts();
})();