(() => {
    const page = document.getElementById('productDetailsPage');
    const readOnlyDetails = document.getElementById('readOnlyDetails');
    const editForm = document.getElementById('editForm');
    const imageInput = document.getElementById('productImage');
    const imagePreview = document.getElementById('imagePreview');

    function setEditing(isEditing) {
        readOnlyDetails.classList.toggle('d-none', isEditing);
        editForm.classList.toggle('d-none', !isEditing);
    }

    async function updateProduct(event) {
        event.preventDefault();

        if (!editForm.checkValidity()) {
            editForm.reportValidity();
            return;
        }

        const response = await fetch(editForm.action, {
            method: 'POST',
            body: new FormData(editForm)
        });
        const result = await response.json();

        if (result.success) {
            location.reload();
            return;
        }

        window.showToast(result.message || 'Unable to update product.', 'error');
    }

    async function deleteProduct() {
        if (!confirm('Delete this product? This cannot be undone.')) {
            return;
        }

        const response = await fetch(page.dataset.deleteUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ id: page.dataset.productId })
        });
        const result = await response.json();

        if (result.success) {
            location.href = page.dataset.productsUrl;
            return;
        }

        window.showToast(result.message || 'Unable to delete product.', 'error');
    }

    function previewImage() {
        const [file] = imageInput.files;

        if (!file) {
            imagePreview.classList.add('d-none');
            imagePreview.removeAttribute('src');
            return;
        }

        imagePreview.src = URL.createObjectURL(file);
        imagePreview.classList.remove('d-none');
    }

    document.getElementById('editToggle').addEventListener('click', () => setEditing(true));
    document.getElementById('cancelEdit').addEventListener('click', () => setEditing(false));
    document.getElementById('deleteProduct').addEventListener('click', deleteProduct);
    imageInput.addEventListener('change', previewImage);
    editForm.addEventListener('submit', updateProduct);

    if (location.hash === '#edit') {
        setEditing(true);
    }
})();
