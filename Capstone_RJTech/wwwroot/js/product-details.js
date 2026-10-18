(() => {
    const page = document.getElementById('productDetailsPage');
    const readOnlyDetails = document.getElementById('readOnlyDetails');
    const editForm = document.getElementById('editForm');
    const imageInput = document.getElementById('productImage');
    const imagePreview = document.getElementById('imagePreview');

    const availabilityModalElement = document.getElementById('productAvailabilityModal');
    const availabilityModal = availabilityModalElement
        ? bootstrap.Modal.getOrCreateInstance(availabilityModalElement)
        : null;
    const availabilityTitle = document.getElementById('productAvailabilityTitle');
    const availabilitySubtitle = document.getElementById('productAvailabilitySubtitle');
    const availabilityMessage = document.getElementById('productAvailabilityMessage');
    const availabilityIcon = document.getElementById('productAvailabilityIcon');
    const availabilityConfirm = document.getElementById('confirmProductAvailability');

    let pendingAvailabilityButton = null;

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
            window.reloadWithToast(result.message || 'Product updated successfully.');
            return;
        }

        window.showToast(result.message || 'Unable to update product.', 'error');
    }

    function toggleAvailability(button) {
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
            ? 'Activate this product? It will become available for sales again.'
            : 'Deactivate this product? It will remain in the system with the status "Unavailable" and will no longer appear in sales.';
        availabilityIcon.className = isActivate ? 'bi bi-check-circle' : 'bi bi-exclamation-triangle';
        availabilityConfirm.textContent = isActivate ? 'Activate' : 'Deactivate';
        availabilityConfirm.classList.toggle('btn-danger', !isActivate);
        availabilityConfirm.classList.toggle('btn-success', isActivate);

        availabilityModal.show();
    }

    async function confirmAvailability() {
        const button = pendingAvailabilityButton;
        if (!button) {
            return;
        }

        const isActivate = button.dataset.action === 'activate';
        const verb = isActivate ? 'Activate' : 'Deactivate';
        const url = isActivate ? page.dataset.activateUrl : page.dataset.deactivateUrl;

        availabilityConfirm.disabled = true;

        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams({ id: page.dataset.productId }),
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
    const availabilityButton =
        document.getElementById('deactivateProduct') || document.getElementById('activateProduct');
    availabilityButton?.addEventListener('click', () => toggleAvailability(availabilityButton));
    availabilityConfirm?.addEventListener('click', confirmAvailability);
    availabilityModalElement?.addEventListener('hidden.bs.modal', () => {
        pendingAvailabilityButton = null;
    });
    imageInput.addEventListener('change', previewImage);
    editForm.addEventListener('submit', updateProduct);

    if (location.hash === '#edit') {
        setEditing(true);
    }
})();
