document.addEventListener('DOMContentLoaded', () => {
    'use strict';

    const trigger = document.getElementById('globalSearchTrigger');
    const modal = document.getElementById('globalSearchModal');
    const dialog = modal?.querySelector('.global-search-dialog');
    const input = document.getElementById('globalSearchInput');
    const closeButton = document.getElementById('globalSearchClose');
    const results = document.getElementById('globalSearchResults');
    const empty = document.getElementById('globalSearchEmpty');
    const count = document.getElementById('globalSearchCount');
    if (!trigger || !modal || !dialog || !input || !closeButton || !results || !empty || !count) return;

    const normalizedPath = value => {
        const path = String(value || '/').replace(/\/$/, '').toLocaleLowerCase();
        return path || '/';
    };
    const currentPath = normalizedPath(window.location.pathname);
    const seenUrls = new Set();
    const pages = [...document.querySelectorAll('.sidebar a[href]')]
        .filter(link => !link.hasAttribute('data-nav-placeholder'))
        .map(link => {
            const url = new URL(link.href, window.location.origin);
            const routeKey = `${normalizedPath(url.pathname)}${url.search}${url.hash}`;
            if (seenUrls.has(routeKey)) return null;
            seenUrls.add(routeKey);

            const group = link.closest('.nav-group');
            const parentTitle = group?.querySelector(':scope > .nav-group-toggle .nav-label')?.textContent.trim() || '';
            const sectionTitle = link.closest('.nav-section')?.querySelector(':scope > .nav-section-label')?.textContent.trim() || '';
            const title = link.querySelector('.nav-label')?.textContent.trim() || link.textContent.trim();
            const iconClass = link.querySelector('.nav-icon i')?.className ||
                group?.querySelector(':scope > .nav-group-toggle .nav-icon i')?.className ||
                'bi bi-file-earmark';
            const location = parentTitle || sectionTitle || 'Workspace';

            return {
                title,
                location,
                iconClass,
                url: url.href,
                isCurrent: normalizedPath(url.pathname) === currentPath,
                searchText: `${title} ${location} ${sectionTitle} ${url.pathname}`.toLocaleLowerCase()
            };
        })
        .filter(Boolean);

    let visiblePages = [];
    let activeIndex = -1;
    let previouslyFocused = null;

    function setActiveResult(index) {
        const items = [...results.querySelectorAll('.global-search-result')];
        if (!items.length) {
            activeIndex = -1;
            input.removeAttribute('aria-activedescendant');
            return;
        }

        activeIndex = (index + items.length) % items.length;
        items.forEach((item, itemIndex) => {
            const active = itemIndex === activeIndex;
            item.classList.toggle('is-active', active);
            item.setAttribute('aria-selected', String(active));
        });
        input.setAttribute('aria-activedescendant', items[activeIndex].id);
        items[activeIndex].scrollIntoView({ block: 'nearest' });
    }

    function rankPage(page, query) {
        const title = page.title.toLocaleLowerCase();
        if (title === query) return 0;
        if (title.startsWith(query)) return 1;
        if (title.includes(query)) return 2;
        return 3;
    }

    function renderResults() {
        const query = input.value.trim().toLocaleLowerCase();
        const terms = query.split(/\s+/).filter(Boolean);
        visiblePages = pages
            .filter(page => terms.every(term => page.searchText.includes(term)))
            .sort((left, right) => query
                ? rankPage(left, query) - rankPage(right, query) || left.title.localeCompare(right.title)
                : 0);

        results.replaceChildren();
        visiblePages.forEach((page, index) => {
            const link = document.createElement('a');
            link.id = `globalSearchResult-${index}`;
            link.className = 'global-search-result';
            link.href = page.url;
            link.tabIndex = -1;
            link.setAttribute('role', 'option');
            link.setAttribute('aria-selected', 'false');

            const icon = document.createElement('span');
            icon.className = 'global-search-result-icon';
            icon.setAttribute('aria-hidden', 'true');
            const iconGlyph = document.createElement('i');
            iconGlyph.className = page.iconClass;
            icon.appendChild(iconGlyph);

            const copy = document.createElement('span');
            copy.className = 'global-search-result-copy';
            const title = document.createElement('strong');
            title.textContent = page.title;
            const location = document.createElement('small');
            location.textContent = page.location;
            copy.append(title, location);
            link.append(icon, copy);

            if (page.isCurrent) {
                const current = document.createElement('span');
                current.className = 'global-search-current';
                current.textContent = 'Current';
                link.appendChild(current);
            }

            link.addEventListener('mouseenter', () => setActiveResult(index));
            link.addEventListener('click', closeSearch);
            results.appendChild(link);
        });

        const hasResults = visiblePages.length > 0;
        results.hidden = !hasResults;
        empty.hidden = hasResults;
        count.textContent = `${visiblePages.length} result${visiblePages.length === 1 ? '' : 's'}`;
        setActiveResult(hasResults ? 0 : -1);
    }

    function openSearch() {
        if (!modal.hidden) return;
        previouslyFocused = document.activeElement;
        modal.hidden = false;
        document.body.classList.add('global-search-open');
        input.value = '';
        renderResults();
        window.requestAnimationFrame(() => input.focus());
    }

    function closeSearch() {
        if (modal.hidden) return;
        modal.hidden = true;
        document.body.classList.remove('global-search-open');
        input.value = '';
        results.replaceChildren();
        input.removeAttribute('aria-activedescendant');
        if (previouslyFocused instanceof HTMLElement) previouslyFocused.focus();
    }

    function openActiveResult() {
        if (activeIndex < 0 || !visiblePages[activeIndex]) return;
        results.querySelectorAll('.global-search-result')[activeIndex]?.click();
    }

    trigger.addEventListener('click', openSearch);
    closeButton.addEventListener('click', closeSearch);
    input.addEventListener('input', renderResults);
    input.addEventListener('keydown', event => {
        if (event.key === 'ArrowDown') {
            event.preventDefault();
            setActiveResult(activeIndex + 1);
        } else if (event.key === 'ArrowUp') {
            event.preventDefault();
            setActiveResult(activeIndex - 1);
        } else if (event.key === 'Enter') {
            event.preventDefault();
            openActiveResult();
        }
    });
    modal.addEventListener('click', event => {
        if (event.target === modal) closeSearch();
    });

    document.addEventListener('keydown', event => {
        const key = event.key.toLocaleLowerCase();
        if (key === 'k' && (event.ctrlKey || event.metaKey)) {
            event.preventDefault();
            openSearch();
            return;
        }
        if (event.key === 'Escape' && !modal.hidden) {
            event.preventDefault();
            closeSearch();
            return;
        }
        if (event.key === 'Tab' && !modal.hidden) {
            const focusable = [input, closeButton];
            const currentIndex = focusable.indexOf(document.activeElement);
            if (event.shiftKey && currentIndex <= 0) {
                event.preventDefault();
                closeButton.focus();
            } else if (!event.shiftKey && currentIndex === focusable.length - 1) {
                event.preventDefault();
                input.focus();
            }
        }
    });
});
