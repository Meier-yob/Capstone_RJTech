
document.addEventListener('DOMContentLoaded', function () {
    const sidebar = document.getElementById('sidebar');
    const sidebarOverlay = document.getElementById('sidebarOverlay');
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebarClose = document.getElementById('sidebarClose');
    const groupToggles = document.querySelectorAll('.nav-group-toggle');
    const sidebarLinks = document.querySelectorAll('.sidebar a');
    const searchInput = document.querySelector('.top-navbar .search-bar input');

    function setMobileSidebar(open) {
        if (!sidebar || !sidebarOverlay) return;
        sidebar.classList.toggle('active', open);
        sidebarOverlay.classList.toggle('show', open);
        document.body.classList.toggle('sidebar-open', open);
        sidebarToggle?.setAttribute('aria-expanded', String(open));
    }

    function setGroupState(group, open) {
        const toggle = group.querySelector(':scope > .nav-group-toggle');
        group.classList.toggle('open', open);
        toggle?.setAttribute('aria-expanded', String(open));
    }

    groupToggles.forEach(function (toggle) {
        toggle.addEventListener('click', function () {
            const group = toggle.closest('.nav-group');
            if (!group) return;
            const willOpen = !group.classList.contains('open');

            document.querySelectorAll('.sidebar .nav-group.open').forEach(function (openGroup) {
                if (openGroup !== group) setGroupState(openGroup, false);
            });
            setGroupState(group, willOpen);
        });
    });

    function markCurrentRoute() {
        const currentPath = window.location.pathname.replace(/\/$/, '').toLowerCase() || '/';
        const currentHash = window.location.hash.toLowerCase();

        document.querySelectorAll('.sidebar .nav-sublink').forEach(function (link) {
            const url = new URL(link.href, window.location.origin);
            const linkPath = url.pathname.replace(/\/$/, '').toLowerCase() || '/';
            const linkHash = url.hash.toLowerCase();
            const isActive = linkPath === currentPath && (currentHash ? linkHash === currentHash : !linkHash);
            link.classList.toggle('active', isActive);
            if (isActive) {
                const group = link.closest('.nav-group');
                if (group) setGroupState(group, true);
            }
        });
    }

    function runHashAction() {
        const actionTargets = {};
        const targetSelector = actionTargets[window.location.hash.toLowerCase()];
        if (!targetSelector) return;

        const actionButton = document.querySelector(targetSelector);
        if (actionButton) window.setTimeout(function () { actionButton.click(); }, 120);
    }

    sidebarToggle?.addEventListener('click', function () { setMobileSidebar(true); });
    sidebarClose?.addEventListener('click', function () { setMobileSidebar(false); });
    sidebarOverlay?.addEventListener('click', function () { setMobileSidebar(false); });

    sidebarLinks.forEach(function (link) {
        link.addEventListener('click', function () {
            if (window.matchMedia('(max-width: 991.98px)').matches && !link.hasAttribute('data-nav-placeholder')) {
                setMobileSidebar(false);
            }
        });
    });

    document.querySelectorAll('[data-nav-placeholder]').forEach(function (item) {
        item.addEventListener('click', function (event) {
            event.preventDefault();
            const label = item.getAttribute('data-nav-placeholder') || 'This module';
            if (typeof window.showToast === 'function') {
                window.showToast(label + ' is prepared in navigation and can be connected when its module is ready.', 'info');
            }
        });
    });

    document.addEventListener('keydown', function (event) {
        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k' && searchInput) {
            event.preventDefault();
            searchInput.focus();
            searchInput.select();
        }
        if (event.key === 'Escape' && document.body.classList.contains('sidebar-open')) {
            setMobileSidebar(false);
            sidebarToggle?.focus();
        }
    });

    window.addEventListener('hashchange', function () {
        markCurrentRoute();
        runHashAction();
    });

    markCurrentRoute();
    runHashAction();
});
