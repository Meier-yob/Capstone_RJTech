(() => {
    const categoryList = document.getElementById('categoryList');
    const categorySearch = document.getElementById('searchCategoryInput');
    const productSearch = document.getElementById('searchProductInput');
    const productTableBody = document.querySelector('#categoryProductsTable tbody');
    const productRows = [...document.querySelectorAll('.product-row-item')];
    const selectedCategoryHeader = document.getElementById('selectedCategoryHeader');
    const selectedCategorySub = document.getElementById('selectedCategorySub');
    const editModal = bootstrap.Modal.getOrCreateInstance(document.getElementById('editCategoryModal'));
    let selectedCategoryId = 'all';

    function renderProducts() {
        const searchTerm = productSearch.value.trim().toLowerCase();
        let visibleCount = 0;

        productRows.forEach(row => {
            const matchesCategory = selectedCategoryId === 'all' || row.dataset.categoryId === selectedCategoryId;
            const matchesSearch = !searchTerm || row.textContent.toLowerCase().includes(searchTerm);
            const visible = matchesCategory && matchesSearch;
            row.classList.toggle('d-none', !visible);
            if (visible) visibleCount += 1;
        });

        let emptyRow = document.getElementById('noProductsRow');
        if (!emptyRow) {
            emptyRow = document.createElement('tr');
            emptyRow.id = 'noProductsRow';
            emptyRow.innerHTML = '<td colspan="6" class="empty-state py-4">No products found in this category.</td>';
            productTableBody.appendChild(emptyRow);
        }
        emptyRow.classList.toggle('d-none', visibleCount > 0);
    }

    function selectCategory(categoryItem) {
        document.querySelectorAll('.category-item').forEach(item => item.classList.remove('active'));
        categoryItem.classList.add('active');
        selectedCategoryId = String(categoryItem.dataset.categoryId);
        selectedCategoryHeader.textContent = categoryItem.dataset.categoryName || 'All Products';
        selectedCategorySub.textContent = selectedCategoryId === 'all'
            ? 'Showing items across all categories'
            : 'Filtered by selected category';
        renderProducts();
    }

    async function submitCategoryForm(form, url, successMessage) {
        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams(new FormData(form))
            });
            const result = await response.json();
            if (result.success) {
                window.reloadWithToast(result.message || successMessage);
                return;
            }
            window.showToast(result.message || 'Unable to save the category.', 'error');
        } catch {
            window.showToast('Unable to save the category.', 'error');
        }
    }

    categoryList.addEventListener('click', event => {
        const editButton = event.target.closest('.edit-category-btn');
        if (editButton) {
            event.preventDefault();
            event.stopPropagation();
            document.getElementById('editCategoryID').value = editButton.dataset.id;
            document.getElementById('editCategoryName').value = editButton.dataset.name;
            editModal.show();
            return;
        }

        const categoryItem = event.target.closest('.category-item');
        if (categoryItem) {
            event.preventDefault();
            selectCategory(categoryItem);
        }
    });

    categorySearch.addEventListener('input', () => {
        const searchTerm = categorySearch.value.trim().toLowerCase();
        document.querySelectorAll('#categoryList .category-wrapper').forEach(wrapper => {
            const name = wrapper.querySelector('.category-title').textContent.toLowerCase();
            wrapper.classList.toggle('d-none', !name.includes(searchTerm));
        });
    });

    productSearch.addEventListener('input', renderProducts);
    document.getElementById('editCategoryForm').addEventListener('submit', event => {
        event.preventDefault();
        submitCategoryForm(event.currentTarget, '/Product/EditCategory', 'Category updated successfully.');
    });
    document.getElementById('createCategoryForm').addEventListener('submit', event => {
        event.preventDefault();
        submitCategoryForm(event.currentTarget, '/Product/CreateCategory', 'Category created successfully.');
    });

    renderProducts();
})();
