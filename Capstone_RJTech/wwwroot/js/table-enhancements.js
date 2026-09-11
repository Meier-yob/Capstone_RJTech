(() => {
    function sentenceCase(value) {
        const text = value.trim();
        return text ? text.charAt(0).toUpperCase() + text.slice(1).toLowerCase() : text;
    }

    function dataRows(table) {
        return [...table.querySelectorAll('tbody > tr')]
            .filter(row => !row.querySelector('.empty-state') && row.querySelectorAll('td').length > 1);
    }

    function sortValue(cell) {
        const raw = (cell?.dataset.sortValue || cell?.innerText || '').trim();
        const numeric = raw.replace(/[₱,%\s]/g, '').replaceAll(',', '');
        if (/^-?\d+(\.\d+)?$/.test(numeric)) return { type: 'number', value: Number(numeric) };
        if (/^-?\d+(\.\d+)?\s*\//.test(raw)) return { type: 'number', value: Number.parseFloat(raw) };
        const date = /[A-Za-z]{3,}|\d{4}-\d{1,2}-\d{1,2}/.test(raw) ? Date.parse(raw) : Number.NaN;
        if (!Number.isNaN(date)) return { type: 'number', value: date };
        return { type: 'text', value: raw.toLocaleLowerCase() };
    }

    function sortTable(table, heading, button) {
        const headingRow = heading.parentElement;
        const columnIndex = [...headingRow.children].indexOf(heading);
        const ascending = heading.dataset.sortDirection !== 'asc';
        const body = table.querySelector('tbody');
        const rows = dataRows(table);
        const emptyRows = [...body.children].filter(row => !rows.includes(row));

        rows.sort((left, right) => {
            const a = sortValue(left.children[columnIndex]);
            const b = sortValue(right.children[columnIndex]);
            const comparison = a.type === 'number' && b.type === 'number'
                ? a.value - b.value
                : String(a.value).localeCompare(String(b.value), undefined, { numeric: true, sensitivity: 'base' });
            return ascending ? comparison : -comparison;
        });

        table.querySelectorAll('th[data-sort-direction]').forEach(item => {
            delete item.dataset.sortDirection;
            item.querySelector('.table-sort-indicator').textContent = '↑↓';
            item.querySelector('.table-sort-button')?.setAttribute('aria-label', 'Sort column');
        });
        heading.dataset.sortDirection = ascending ? 'asc' : 'desc';
        button.querySelector('.table-sort-indicator').textContent = ascending ? '↑' : '↓';
        button.setAttribute('aria-label', ascending ? 'Sort descending' : 'Sort ascending');
        rows.forEach(row => body.appendChild(row));
        emptyRows.forEach(row => body.appendChild(row));
        table.dispatchEvent(new CustomEvent('table:sorted', { detail: { ascending, columnIndex } }));
    }

    function enhanceTable(table) {
        if (table.dataset.tableEnhanced === 'true' || table.dataset.sortable === 'false') return;
        const headingRow = table.querySelector('thead tr');
        const body = table.querySelector('tbody');
        if (!headingRow || !body) return;
        table.dataset.tableEnhanced = 'true';
        table.classList.add('enhanced-data-table');

        const frame = table.closest('.table-responsive');
        if (frame && !frame.closest('.management-card')) frame.classList.add('data-table-frame');
        if (!frame && !table.closest('.management-card')) table.classList.add('standalone-data-table');

        [...headingRow.children].forEach(heading => {
            const textNode = [...heading.childNodes].find(node => node.nodeType === Node.TEXT_NODE && node.textContent.trim());
            const label = textNode?.textContent.trim() || heading.textContent.trim();
            if (textNode) textNode.textContent = sentenceCase(label);
            if (/^(action|actions)$/i.test(label)) return;

            const button = document.createElement('button');
            button.className = 'table-sort-button';
            button.type = 'button';
            button.setAttribute('aria-label', `Sort by ${label}`);
            const indicator = document.createElement('span');
            indicator.className = 'table-sort-indicator';
            indicator.textContent = '↑↓';
            indicator.setAttribute('aria-hidden', 'true');
            button.appendChild(indicator);
            button.addEventListener('click', event => {
                event.stopPropagation();
                sortTable(table, heading, button);
            });
            heading.appendChild(button);
        });

    }

    document.querySelectorAll('table').forEach(enhanceTable);
})();
