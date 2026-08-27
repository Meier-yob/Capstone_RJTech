(() => {
    const rows = [...document.querySelectorAll('.purchased-item-row')];
    const pagination = document.getElementById('purchasedItemsPagination');
    const range = document.getElementById('purchasedItemsRange');
    const pageSize = 10;
    let currentPage = 1;

    function addButton(label, page, disabled = false, active = false) {
        const item = document.createElement('li');
        const button = document.createElement('button');
        item.className = `page-item${disabled ? ' disabled' : ''}${active ? ' active' : ''}`;
        button.className = 'page-link';
        button.type = 'button';
        button.textContent = label;
        button.disabled = disabled;
        button.addEventListener('click', () => { currentPage = page; render(); });
        item.appendChild(button);
        pagination.appendChild(item);
    }

    function render() {
        const pages = Math.max(1, Math.ceil(rows.length / pageSize));
        const start = (currentPage - 1) * pageSize;
        rows.forEach((row, index) => row.classList.toggle('d-none', index < start || index >= start + pageSize));
        range.textContent = rows.length ? `Showing ${start + 1}–${Math.min(start + pageSize, rows.length)} of ${rows.length}` : 'Showing 0 items';
        pagination.innerHTML = '';
        addButton('Previous', currentPage - 1, currentPage === 1);
        for (let page = 1; page <= pages; page += 1) addButton(String(page), page, false, page === currentPage);
        addButton('Next', currentPage + 1, currentPage === pages);
    }

    document.getElementById('printReceipt').addEventListener('click', () => window.print());
    window.addEventListener('beforeprint', () => rows.forEach(row => row.classList.remove('d-none')));
    window.addEventListener('afterprint', render);
    render();
})();
