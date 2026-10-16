// Shared confirmation dialog for the deactivate / reactivate actions on the
// Sales and Delivery pages. Trigger buttons carry their own copy through data
// attributes, so one dialog instance serves every record on the page.
(() => {
    const modalElement = document.getElementById('deactivateConfirmModal');
    if (!modalElement || !window.bootstrap) {
        return;
    }

    const modal = new bootstrap.Modal(modalElement);
    const titleElement = modalElement.querySelector('#deactivateConfirmTitle');
    const subtitleElement = modalElement.querySelector('#deactivateConfirmSubtitle');
    const messageElement = modalElement.querySelector('#deactivateConfirmMessage');
    const iconWrap = modalElement.querySelector('#deactivateConfirmIconWrap');
    const iconElement = modalElement.querySelector('#deactivateConfirmIcon');
    const confirmButton = modalElement.querySelector('#deactivateConfirmButton');
    const cancelButton = modalElement.querySelector('.confirm-exit-cancel');
    let pendingTrigger = null;

    function decorate(trigger) {
        const isRestore = trigger.dataset.confirmVariant === 'success';
        titleElement.textContent = trigger.dataset.confirmTitle || 'Are you sure?';
        subtitleElement.textContent = isRestore
            ? 'This will return the record to the active list.'
            : 'You can restore this record later.';
        messageElement.textContent = trigger.dataset.confirmMessage || '';
        confirmButton.textContent = trigger.dataset.confirmLabel || 'Confirm';
        confirmButton.classList.toggle('btn-success', isRestore);
        confirmButton.classList.toggle('btn-warning', !isRestore);
        iconWrap.classList.toggle('is-success', isRestore);
        iconElement.className = `bi ${isRestore ? 'bi-arrow-clockwise' : 'bi-slash-circle'}`;
    }

    document.addEventListener('click', event => {
        const trigger = event.target.closest('.js-deactivate, .js-reactivate');
        if (!trigger) {
            return;
        }
        event.preventDefault();
        pendingTrigger = trigger;
        decorate(trigger);
        modal.show();
    });

    modalElement.addEventListener('shown.bs.modal', () => cancelButton?.focus());
    modalElement.addEventListener('hidden.bs.modal', () => { pendingTrigger = null; });

    confirmButton.addEventListener('click', async () => {
        if (!pendingTrigger) {
            return;
        }

        const trigger = pendingTrigger;
        const payload = new URLSearchParams();
        payload.set(trigger.dataset.confirmField || 'id', trigger.dataset.id);

        confirmButton.disabled = true;
        confirmButton.setAttribute('aria-busy', 'true');

        try {
            const response = await fetch(trigger.dataset.confirmUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: payload,
                skipToast: true
            });
            const result = await response.json();
            if (result.success) {
                window.reloadWithToast(result.message || 'Done.');
                return;
            }
            modal.hide();
            window.showToast(result.message || 'Unable to complete the action.', 'error');
        } catch {
            modal.hide();
            window.showToast('Unable to connect. Please try again.', 'error');
        } finally {
            confirmButton.disabled = false;
            confirmButton.removeAttribute('aria-busy');
        }
    });
})();
