(() => {
    const page = document.getElementById('categoryManagementPage');
    if (!page) return;
    const categoryList = document.getElementById('categoryList');
    const categorySearch = document.getElementById('searchCategoryInput');
    const productSearch = document.getElementById('searchProductInput');
    const table = document.getElementById('categoryProductsTable');
    let productRows = [...table.querySelectorAll('.product-row-item')];
    const categoryItems = [...categoryList.querySelectorAll('.category-item')];
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

    async function submitCategoryForm(form) {
        if (!form.reportValidity()) return;
        const button = form.querySelector('[type="submit"]');
        const error = form.querySelector('.category-form-error');
        const name = form.querySelector('[name="category_name"]');
        if (!name.value.trim()) {
            error.textContent = 'Enter a category name.';
            error.classList.remove('d-none');
            name.focus();
            return;
        }
        button.disabled = true;
        button.querySelector('.spinner-border').classList.remove('d-none');
        error.classList.add('d-none');
        try {
            const response = await fetch(form.action, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams(new FormData(form))
            });
            if (!response.ok) throw new Error('Request failed.');
            const result = await response.json();
            if (result.success) {
                window.reloadWithToast(result.message || 'Category saved successfully.');
                return;
            }
            error.textContent = result.message || 'Unable to save the category.';
            error.classList.remove('d-none');
        } catch {
            error.textContent = 'Unable to save the category. Please try again.';
            error.classList.remove('d-none');
        } finally {
            button.disabled = false;
            button.querySelector('.spinner-border').classList.add('d-none');
        }
    }

    categoryList.addEventListener('click', event => {
        const editButton = event.target.closest('.edit-category-btn');
        if (editButton) {
            document.getElementById('editCategoryID').value = editButton.dataset.id;
            document.getElementById('editCategoryName').value = editButton.dataset.name;
            editModalElement.querySelector('.category-form-error').classList.add('d-none');
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
    ['createCategoryForm', 'editCategoryForm'].forEach(id => {
        document.getElementById(id).addEventListener('submit', event => {
            event.preventDefault();
            submitCategoryForm(event.currentTarget);
        });
    });
    document.getElementById('createCategoryModal').addEventListener('show.bs.modal', event => {
        const form = event.currentTarget.querySelector('form');
        form.reset();
        form.querySelector('.category-form-error').classList.add('d-none');
    });
    document.querySelectorAll('.category-modal').forEach(modal => {
        modal.addEventListener('shown.bs.modal', () => modal.querySelector('[name="category_name"]').focus());
    });
    renderProducts();
})();

