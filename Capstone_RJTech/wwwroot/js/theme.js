(() => {
    'use strict';
    const storageKey = 'rjtech.theme';
    const root = document.documentElement;
    const systemTheme = window.matchMedia('(prefers-color-scheme: dark)');
    const validTheme = value => value === 'dark' || value === 'light';
    let preference;
    try { preference = localStorage.getItem(storageKey); } catch { /* Storage is optional. */ }

    function updateToggle() {
        const toggle = document.getElementById('themeToggle');
        if (!toggle) return;
        const dark = root.dataset.theme === 'dark';
        toggle.setAttribute('aria-pressed', String(dark));
        toggle.title = dark ? 'Switch to light mode' : 'Switch to dark mode';
        toggle.querySelector('i').className = dark ? 'bi bi-sun' : 'bi bi-moon';
    }

    function applyTheme() {
        const theme = validTheme(preference) ? preference : systemTheme.matches ? 'dark' : 'light';
        root.dataset.theme = theme;
        updateToggle();
        window.dispatchEvent(new CustomEvent('rj:theme-changed', { detail: { theme } }));
    }

    // Run before styles load so a saved dark theme never flashes a light page.
    applyTheme();
    document.addEventListener('DOMContentLoaded', () => {
        updateToggle();
        document.getElementById('themeToggle')?.addEventListener('click', () => {
            preference = root.dataset.theme === 'dark' ? 'light' : 'dark';
            try { localStorage.setItem(storageKey, preference); } catch { /* Keep the toggle usable. */ }
            applyTheme();
        });
    });
    systemTheme.addEventListener('change', () => {
        if (!validTheme(preference)) applyTheme();
    });
    window.addEventListener('storage', event => {
        if (event.key !== storageKey && event.key !== null) return;
        preference = event.newValue;
        applyTheme();
    });
})();
