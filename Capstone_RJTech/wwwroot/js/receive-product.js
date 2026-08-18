(() => {
    // Page elements used by product selection and receiving.
    const rows = [...document.querySelectorAll('.selectable-product')];
    const selected = new Map();
    const selectedContainer = document.getElementById('selectedProducts');
    const selectedEmpty = document.getElementById('selectedEmpty');
    const complete = document.getElementById('completeDelivery');
    const error = document.getElementById('receiveError');

    // State Variables
    let availablePage = 1;
    let selectedPage = 1;
    let dirty = false;
    let leaveHref = '';

    // Helper Functions
    const escape = value => String(value ?? '').replace(/[&<>"']/g, char => ({
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#39;'
    }[char]));

    // Pagination Component Builder
    function buildPagination(element, pages, current, onChange) {
        element.innerHTML = '';

        const add = (label, target, disabled = false, active = false) => {
            const li = document.createElement('li');
            li.className = `page-item${disabled ? ' disabled' : ''}${active ? ' active' : ''}`;

            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'page-link';
            button.innerHTML = label;
            button.disabled = disabled;
            button.addEventListener('click', () => onChange(target));

            li.appendChild(button);
            element.appendChild(li);
        };

        // Previous
        add('<i class="bi bi-chevron-left"></i>', current - 1, current === 1);

        // Pages
        for (let i = 1; i <= pages; i++) {
            add(String(i), i, false, i === current);
        }

        // Next
        add('<i class="bi bi-chevron-right"></i>', current + 1, current === pages);
    }

    // Available Products Filtering
    function filteredAvailable() {
        const term = document.getElementById('availableSearch').value.trim().toLowerCase();
        const status = document.getElementById('availableStatus').value;

        return rows.filter(row =>
            (!term || row.dataset.search.includes(term)) &&
            (status === 'all' || row.dataset.status === status)
        );
    }

    // Render Available Products
    function renderAvailable() {
        const filtered = filteredAvailable();
        const size = Number(document.getElementById('availablePageSize').value);
        const pages = Math.max(1, Math.ceil(filtered.length / size));

        availablePage = Math.min(availablePage, pages);

        rows.forEach(row => row.classList.add('d-none'));
        filtered.slice((availablePage - 1) * size, availablePage * size).forEach(row => row.classList.remove('d-none'));

        document.getElementById('availableEmpty').classList.toggle('d-none', filtered.length !== 0);

        const start = filtered.length ? (availablePage - 1) * size + 1 : 0;
        const end = Math.min(availablePage * size, filtered.length);
        document.getElementById('availableRange').textContent = `${start}–${end} of ${filtered.length}`;

        buildPagination(document.getElementById('availablePagination'), pages, availablePage, target => {
            availablePage = target;
            renderAvailable();
        });
    }

    // Product Selection Handlers
    function select(row) {
        if (selected.has(row.dataset.id)) return;

        const pendingQuantity = Number(row.dataset.pendingQuantity);
        const defaultReceiveQuantity = pendingQuantity > 0 ? pendingQuantity : 1;

        selected.set(row.dataset.id, {
            row,
            quantity: defaultReceiveQuantity
        });
        row.classList.add('is-selected');
        dirty = true;

        selectedPage = Math.ceil(selected.size / Number(document.getElementById('selectedPageSize').value));
        renderSelected();
    }

    rows.forEach(row => {
        row.addEventListener('click', () => select(row));
        row.addEventListener('keydown', event => {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                select(row);
            }
        });
    });

    // Render Selected Products List
    function renderSelected() {
        selectedContainer.querySelectorAll('.receive-item').forEach(item => item.remove());
        selectedEmpty.classList.toggle('d-none', selected.size > 0);

        const entries = [...selected];
        const size = Number(document.getElementById('selectedPageSize').value);
        const pages = Math.max(1, Math.ceil(entries.length / size));

        selectedPage = Math.min(selectedPage, pages);

        entries.slice((selectedPage - 1) * size, selectedPage * size).forEach(([id, item]) => {
            const p = item.row.dataset;
            const card = document.createElement('div');
            card.className = 'receive-item receive-item-compact mb-2';
            card.dataset.id = id;
            card.innerHTML = `
                        <div class="d-flex align-items-center gap-2">
                            <span class="product-thumb product-thumb-sm">
                                <i class="bi bi-box-seam"></i>
                            </span>
                            <div class="flex-grow-1 min-width-0">
                                <div class="product-name">${escape(p.name)}</div>
                                <div class="product-meta">${escape(p.category)} · ${escape(p.brand)} · ${escape(p.code)}</div>
                            </div>
                            <div class="receive-stock text-center">
                                <div class="detail-label">Stock</div>
                                <strong>${escape(p.stock)}</strong>
                            </div>
                            <div class="receive-qty">
                                <label class="detail-label" for="qty-${id}">Receive Qty</label>
                                <input id="qty-${id}" class="form-control form-control-sm receive-quantity" type="number" min="1" step="1" value="${item.quantity}" required>
                            </div>
                            <button class="btn btn-sm btn-link view-receive-details" type="button" title="View details">
                                <i class="bi bi-eye"></i>
                            </button>
                            <button class="btn btn-sm btn-outline-danger remove-item" type="button" title="Remove">
                                <i class="bi bi-x-lg"></i>
                            </button>
                        </div>
                    `;
            selectedContainer.appendChild(card);
        });

        // Event Listeners for Selected Product Items
        selectedContainer.querySelectorAll('.remove-item').forEach(button => {
            button.addEventListener('click', event => {
                const id = event.currentTarget.closest('.receive-item').dataset.id;
                selected.get(id).row.classList.remove('is-selected');
                selected.delete(id);
                dirty = true;
                renderSelected();
            });
        });

        selectedContainer.querySelectorAll('.receive-quantity').forEach(input => {
            input.addEventListener('input', event => {
                const id = event.currentTarget.closest('.receive-item').dataset.id;
                selected.get(id).quantity = Number(event.currentTarget.value);
                dirty = true;
                updateSummary();
            });
        });

        selectedContainer.querySelectorAll('.view-receive-details').forEach(button => {
            button.addEventListener('click', event => {
                const id = event.currentTarget.closest('.receive-item').dataset.id;
                showDetails(selected.get(id).row.dataset);
            });
        });

        // Pagination for Selected List
        document.getElementById('selectedPaging').classList.toggle('d-none', !selected.size);
        const start = entries.length ? (selectedPage - 1) * size + 1 : 0;
        const end = Math.min(selectedPage * size, entries.length);
        document.getElementById('selectedRange').textContent = `${start}–${end} of ${entries.length}`;

        buildPagination(document.getElementById('selectedPagination'), pages, selectedPage, target => {
            selectedPage = target;
            renderSelected();
        });

        updateSummary();
    }

    // Modal Details Display
    function showDetails(p) {
        document.getElementById('receiveDetailsTitle').textContent = p.name;
        document.getElementById('receiveDetailsMeta').textContent = `${p.category} · ${p.code}`;
        document.getElementById('detailBrand').textContent = p.brand;
        document.getElementById('detailCategory').textContent = p.category;
        document.getElementById('detailStock').textContent = p.stock;
        document.getElementById('detailReorder').textContent = p.reorder;
        document.getElementById('detailPrice').textContent = `₱${Number(p.price).toLocaleString(undefined, { minimumFractionDigits: 2 })}`;
        document.getElementById('detailDescription').textContent = p.description || 'No description provided.';

        bootstrap.Modal.getOrCreateInstance(document.getElementById('receiveDetailsModal')).show();
    }

    // Summary & Validation Updates
    function updateSummary() {
        let total = 0;
        let valid = selected.size > 0;

        selected.forEach(item => {
            if (!Number.isInteger(item.quantity) || item.quantity < 1) {
                valid = false;
            } else {
                total += item.quantity;
            }
        });

        document.getElementById('selectedCount').textContent = selected.size;
        document.getElementById('totalQuantity').textContent = total;

        complete.disabled = !valid ||
            !document.getElementById('batchId').value ||
            !document.getElementById('deliveryDate').value ||
            !document.getElementById('receivedBy').value;
    }

    // Event Listeners: Filter & Page Size Inputs
    document.getElementById('availableSearch').addEventListener('input', () => { availablePage = 1; renderAvailable(); });
    document.getElementById('availableStatus').addEventListener('change', () => { availablePage = 1; renderAvailable(); });
    document.getElementById('availablePageSize').addEventListener('change', () => { availablePage = 1; renderAvailable(); });
    document.getElementById('selectedPageSize').addEventListener('change', () => { selectedPage = 1; renderSelected(); });

    document.getElementById('deliveryDate').addEventListener('change', () => {
        dirty = true;
        updateSummary();
    });

    // Unsaved Changes Guard
    const exitModal = new bootstrap.Modal(document.getElementById('confirmExitModal'));

    document.addEventListener('click', event => {
        const link = event.target.closest('a[href]');
        if (!dirty || !link || link.getAttribute('href').startsWith('#') || link.target === '_blank') return;

        event.preventDefault();
        leaveHref = link.href;
        exitModal.show();
    });

    document.getElementById('discardAndLeave').addEventListener('click', () => {
        dirty = false;
        location.href = leaveHref || '@Url.Action("DeliveryManagement")';
    });

    window.addEventListener('beforeunload', event => {
        if (dirty) {
            event.preventDefault();
            event.returnValue = '';
        }
    });

    // Complete Delivery Submission
    complete.addEventListener('click', async () => {
        updateSummary();
        if (complete.disabled) return;

        complete.disabled = true;
        complete.querySelector('.spinner-border').classList.remove('d-none');
        error.classList.add('d-none');

        const payload = {
            batch_ID: document.getElementById('batchId').value,
            delivery_date: document.getElementById('deliveryDate').value,
            received_by: document.getElementById('receivedBy').value,
            items: [...selected].map(([id, item]) => ({
                product_ID: Number(id),
                quantity: item.quantity
            }))
        };

        try {
            const response = await fetch('@Url.Action("CompleteDelivery")', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            const result = await response.json();

            if (result.success) {
                dirty = false;
                location.href = result.redirectUrl || '@Url.Action("DeliveryManagement")';
            } else {
                error.textContent = result.message || 'Unable to complete delivery.';
                error.classList.remove('d-none');
                complete.disabled = false;
            }
        } catch {
            error.textContent = 'Unable to complete delivery.';
            error.classList.remove('d-none');
            complete.disabled = false;
        } finally {
            complete.querySelector('.spinner-border').classList.add('d-none');
        }
    });

    // Initializations
    renderAvailable();
    renderSelected();
})();