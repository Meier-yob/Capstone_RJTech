// Prevent dropped files from causing an accidental page navigation.
window.addEventListener('dragover', event => event.preventDefault());
window.addEventListener('drop', event => event.preventDefault());

/**
 * Displays the shared Bootstrap toast.
 * @param {string} message Message shown to the user.
 * @param {'success'|'error'|'warning'|'info'} [type='success'] Visual toast type.
 */
const toastDuration = 5000;
const pendingToastKey = 'rjtech.pendingToast';
let lastToastMessage = '';
let lastToastTime = 0;

function savePendingToast(message, type) {
    try {
        sessionStorage.setItem(pendingToastKey, JSON.stringify({
            message,
            type,
            createdAt: Date.now()
        }));
    } catch {
        // Navigation still works when browser storage is unavailable.
    }
}

function takePendingToast() {
    try {
        const storedToast = sessionStorage.getItem(pendingToastKey);
        sessionStorage.removeItem(pendingToastKey);

        if (!storedToast) {
            return null;
        }

        const pendingToast = JSON.parse(storedToast);
        const isRecent = Date.now() - pendingToast.createdAt < 30000;
        return isRecent ? pendingToast : null;
    } catch {
        return null;
    }
}

window.showToast = function showToast(message, type = 'success') {
    const toast = document.getElementById('appToast');
    const messageElement = document.getElementById('appToastMessage');
    const icon = document.getElementById('appToastIcon');
    const container = document.getElementById('appToastContainer');

    if (!toast || !messageElement || !icon || !container) {
        return;
    }

    // Keep the toast outside page-specific stacking and overflow containers.
    if (container.parentElement !== document.body) {
        document.body.appendChild(container);
    }

    const now = Date.now();
    if (message === lastToastMessage && now - lastToastTime < 750) {
        return;
    }
    lastToastMessage = message;
    lastToastTime = now;

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

    if (window.bootstrap?.Toast) {
        bootstrap.Toast.getOrCreateInstance(toast, {
            autohide: true,
            delay: toastDuration
        }).show();
        return;
    }

    // Basic fallback so feedback remains visible even if Bootstrap fails to load.
    toast.classList.add('show');
    window.setTimeout(() => toast.classList.remove('show'), toastDuration);
};

/**
 * Carries a toast across a page navigation and displays it on the destination page.
 */
window.redirectWithToast = function redirectWithToast(message, type, url) {
    savePendingToast(message, type);
    window.location.assign(url);
};

/**
 * Carries a toast across a page refresh and displays it after the refresh.
 */
window.reloadWithToast = function reloadWithToast(message, type = 'success') {
    savePendingToast(message, type);
    window.location.reload();
};

// Show feedback consistently for every JSON mutation performed with fetch.
const nativeFetch = window.fetch.bind(window);
window.fetch = async function fetchWithToast(resource, options = {}) {
    try {
        const response = await nativeFetch(resource, options);
        const contentType = response.headers.get('content-type') || '';
        const method = (options.method || 'GET').toUpperCase();

        if (contentType.includes('application/json') && method !== 'GET') {
            const result = await response.clone().json();
            if (result.message) {
                window.showToast(result.message, result.success && response.ok ? 'success' : 'error');
            }
        }

        if (!response.ok && !contentType.includes('application/json')) {
            window.showToast('The request could not be completed.', 'error');
        }
        return response;
    } catch (error) {
        window.showToast('Unable to connect. Please try again.', 'error');
        throw error;
    }
};

// Display feedback returned by the older jQuery AJAX screens as well.
if (window.jQuery) {
    window.jQuery(document).ajaxComplete((event, xhr) => {
        const result = xhr.responseJSON;
        if (result?.message) {
            window.showToast(result.message, result.success ? 'success' : 'error');
        }
    });
}

// Display messages carried across a normal form redirect.
const pendingToast = takePendingToast();
const toastContainer = document.getElementById('appToastContainer');

if (pendingToast?.message) {
    window.showToast(pendingToast.message, pendingToast.type);
} else if (toastContainer?.dataset.successMessage) {
    window.showToast(toastContainer.dataset.successMessage, 'success');
} else if (toastContainer?.dataset.errorMessage) {
    window.showToast(toastContainer.dataset.errorMessage, 'error');
} else if (toastContainer?.dataset.warningMessage) {
    window.showToast(toastContainer.dataset.warningMessage, 'warning');
} else if (toastContainer?.dataset.infoMessage) {
    window.showToast(toastContainer.dataset.infoMessage, 'info');
}
