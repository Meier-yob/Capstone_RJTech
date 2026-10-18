// Prevent dropped files from causing an accidental page navigation.
window.addEventListener('dragover', event => event.preventDefault());
window.addEventListener('drop', event => event.preventDefault());

/* ===== Toast notifications =====
   Reusable alert component. The container and the single markup definition live
   in _Layout.cshtml; this code clones the definition per message so multiple
   alerts can stack in the fixed top-right area.

   Timer state (remaining, timer, paused, resumedAt) is kept on the toast element
   as expandos. Elements are discarded with the DOM, so nothing leaks. */
const toastDuration = 4000;
const toastMaxVisible = 4;
const pendingToastKey = 'rjtech.pendingToast';
let lastToastMessage = '';
let lastToastTime = 0;

const toastVariants = {
    success: { modifier: 'app-toast--success', icon: 'bi-check-lg' },
    error: { modifier: 'app-toast--error', icon: 'bi-exclamation-lg' },
    warning: { modifier: 'app-toast--warning', icon: 'bi-exclamation-triangle-lg' },
    info: { modifier: 'app-toast--info', icon: 'bi-info-lg' }
};

function savePendingToast(message, type, detail) {
    try {
        sessionStorage.setItem(pendingToastKey, JSON.stringify({
            message,
            type,
            detail,
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

/**
 * Starts (or restarts) the auto-dismiss countdown from the time still remaining.
 */
function resumeToastTimer(toast) {
    window.clearTimeout(toast.timer);
    toast.resumedAt = Date.now();
    toast.timer = window.setTimeout(() => dismissToast(toast), toast.remaining);
}

/**
 * Freezes the countdown. Hovering keeps the toast up for as long as the pointer
 * stays on it, instead of restarting the full delay once the pointer leaves.
 */
function pauseToastTimer(toast) {
    if (toast.paused) {
        return;
    }
    window.clearTimeout(toast.timer);
    toast.remaining = Math.max(0, toast.remaining - (Date.now() - toast.resumedAt));
    toast.paused = true;
}

function dismissToast(toast) {
    if (!toast || !toast.isConnected || toast.classList.contains('is-leaving')) {
        return;
    }
    window.clearTimeout(toast.timer);
    toast.classList.remove('is-visible');
    toast.classList.add('is-leaving');
    window.setTimeout(() => toast.remove(), 240);
}

/**
 * Retires the oldest alerts so a burst of messages cannot cover the screen.
 */
function trimToastStack(container) {
    const standing = container.querySelectorAll('.app-toast:not(.is-leaving)');
    for (let index = 0; index < standing.length - toastMaxVisible; index += 1) {
        dismissToast(standing[index]);
    }
}

/**
 * Displays a toast alert.
 * @param {string} message Primary message shown to the user.
 * @param {'success'|'error'|'warning'|'info'} [type='success'] Visual toast type.
 * @param {{label: string, onClick: Function}} [action] Optional right-aligned action button.
 * @param {{detail?: string, duration?: number}} [options] Supporting text and custom lifetime.
 * @returns {{element: HTMLElement, dismiss: Function}|null} Handle for the created toast.
 */
window.showToast = function showToast(message, type = 'success', action = null, options = {}) {
    const container = document.getElementById('appToastContainer');
    const template = document.getElementById('appToastTemplate');

    if (!container || !template || !message) {
        return null;
    }

    // Keep the stack outside page-specific stacking and overflow containers.
    if (container.parentElement !== document.body) {
        document.body.appendChild(container);
    }

    const now = Date.now();
    if (message === lastToastMessage && now - lastToastTime < 750) {
        return null;
    }
    lastToastMessage = message;
    lastToastTime = now;

    const variant = toastVariants[type] ?? toastVariants.success;
    const toast = template.content.firstElementChild.cloneNode(true);

    toast.classList.add(variant.modifier);
    toast.querySelector('.app-toast-icon i').className = `bi ${variant.icon}`;
    toast.querySelector('.app-toast-title').textContent = message;

    const detailElement = toast.querySelector('.app-toast-detail');
    const detail = typeof options.detail === 'string' ? options.detail : '';
    if (detail) {
        detailElement.textContent = detail;
        detailElement.classList.remove('d-none');
    }

    const actionButton = toast.querySelector('.app-toast-action');
    if (action && typeof action.onClick === 'function') {
        actionButton.textContent = action.label || 'Action';
        actionButton.classList.remove('d-none');
        actionButton.addEventListener('click', event => {
            event.preventDefault();
            dismissToast(toast);
            action.onClick(event);
        });
    }

    toast.querySelector('.app-toast-close').addEventListener('click', () => dismissToast(toast));
    toast.addEventListener('mouseenter', () => pauseToastTimer(toast));
    toast.addEventListener('mouseleave', () => resumeToastTimer(toast));
    toast.addEventListener('focusin', () => pauseToastTimer(toast));
    toast.addEventListener('focusout', event => {
        if (!toast.contains(event.relatedTarget)) {
            resumeToastTimer(toast);
        }
    });

    toast.remaining = typeof options.duration === 'number' ? options.duration : toastDuration;
    toast.paused = false;
    container.appendChild(toast);
    trimToastStack(container);

    // Read layout so the entry transition starts from the hidden state.
    void toast.offsetWidth;
    toast.classList.add('is-visible');
    resumeToastTimer(toast);

    return { element: toast, dismiss: () => dismissToast(toast) };
};

/**
 * Carries a toast across a page navigation and displays it on the destination page.
 */
window.redirectWithToast = function redirectWithToast(message, type, url, options = {}) {
    savePendingToast(message, type, options?.detail);
    window.location.assign(url);
};

/**
 * Carries a toast across a page refresh and displays it after the refresh.
 */
window.reloadWithToast = function reloadWithToast(message, type = 'success', options = {}) {
    savePendingToast(message, type, options?.detail);
    window.location.reload();
};

// Show feedback consistently for every JSON mutation performed with fetch.
// Pages that render their own result dialog can set skipToast to avoid a duplicate toast.
const nativeFetch = window.fetch.bind(window);
window.fetch = async function fetchWithToast(resource, options = {}) {
    const skipToast = options.skipToast === true;
    try {
        const response = await nativeFetch(resource, options);
        const contentType = response.headers.get('content-type') || '';
        const method = (options.method || 'GET').toUpperCase();

        if (contentType.includes('application/json') && method !== 'GET') {
            const result = await response.clone().json();
            if (result.message && !skipToast) {
                window.showToast(result.message, result.success && response.ok ? 'success' : 'error');
            }
        }

        if (!response.ok && !contentType.includes('application/json') && !skipToast) {
            window.showToast('The request could not be completed.', 'error');
        }
        return response;
    } catch (error) {
        if (!skipToast && error?.name !== 'AbortError') {
            window.showToast('Unable to connect. Please try again.', 'error');
        }
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
    window.showToast(pendingToast.message, pendingToast.type, null, { detail: pendingToast.detail });
} else if (toastContainer?.dataset.successMessage) {
    window.showToast(toastContainer.dataset.successMessage, 'success');
} else if (toastContainer?.dataset.errorMessage) {
    window.showToast(toastContainer.dataset.errorMessage, 'error');
} else if (toastContainer?.dataset.warningMessage) {
    window.showToast(toastContainer.dataset.warningMessage, 'warning');
} else if (toastContainer?.dataset.infoMessage) {
    window.showToast(toastContainer.dataset.infoMessage, 'info');
}

// Logout confirmation modal: scoped backdrop, default focus on Cancel, busy state on submit.
(() => {
    const logoutModal = document.getElementById('logoutConfirmationModal');
    if (!logoutModal) {
        return;
    }

    const cancelButton = logoutModal.querySelector('[data-bs-dismiss="modal"]');

    logoutModal.addEventListener('show.bs.modal', () => {
        document.body.classList.add('logout-modal-open');
    });

    logoutModal.addEventListener('shown.bs.modal', () => {
        cancelButton?.focus();
    });

    logoutModal.addEventListener('hidden.bs.modal', () => {
        document.body.classList.remove('logout-modal-open');
    });

    logoutModal.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', () => {
            const button = form.querySelector('button[type="submit"]');
            if (button && !button.disabled) {
                button.disabled = true;
                button.setAttribute('aria-busy', 'true');
                button.innerHTML = '<span class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>Logging out\u2026';
            }
        });
    });
})();
