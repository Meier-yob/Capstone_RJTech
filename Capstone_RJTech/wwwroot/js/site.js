// Prevent dropped files from causing an accidental page navigation.
window.addEventListener('dragover', event => event.preventDefault());
window.addEventListener('drop', event => event.preventDefault());

/**
 * Displays the shared Bootstrap toast.
 * @param {string} message Message shown to the user.
 * @param {'success'|'error'|'warning'|'info'} [type='success'] Visual toast type.
 */
window.showToast = function showToast(message, type = 'success') {
    const toast = document.getElementById('appToast');
    const messageElement = document.getElementById('appToastMessage');
    const icon = document.getElementById('appToastIcon');

    if (!toast || !messageElement || !icon) {
        return;
    }

    const styles = {
        success: ['text-bg-success', 'bi-check-circle-fill'],
        error: ['text-bg-danger', 'bi-exclamation-circle-fill'],
        warning: ['text-bg-warning', 'bi-exclamation-triangle-fill'],
        info: ['text-bg-info', 'bi-info-circle-fill']
    };
    const [backgroundClass, iconClass] = styles[type] ?? styles.success;

    toast.classList.remove(
        'text-bg-success',
        'text-bg-danger',
        'text-bg-warning',
        'text-bg-info'
    );
    toast.classList.add(backgroundClass);
    icon.className = `bi ${iconClass}`;
    messageElement.textContent = message || '';

    bootstrap.Toast.getOrCreateInstance(toast, { delay: 3200 }).show();
};
