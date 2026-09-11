(() => {
    const page = document.getElementById('installmentPage');
    if (!page) return;

    const currency = new Intl.NumberFormat('en-PH', {
        style: 'currency',
        currency: 'PHP',
        minimumFractionDigits: 2
    });
    const paymentForm = document.getElementById('recordPaymentForm');
    const recordButton = document.getElementById('recordInstallmentPayment');
    const paymentAmount = document.getElementById('modalPaymentAmount');

    function showError(message) {
        if (window.showToast) window.showToast(message, 'error');
        else alert(message);
    }

    function redirectWithMessage(message, url) {
        if (window.redirectWithToast) window.redirectWithToast(message, 'success', url);
        else window.location.href = url;
    }

    document.querySelectorAll('.open-payment-modal').forEach(button => {
        button.addEventListener('click', () => {
            const balance = Number(button.dataset.balance);
            paymentForm?.classList.remove('was-validated');
            document.getElementById('modalInstallmentId').value = button.dataset.installmentId;
            document.getElementById('modalInstallmentCode').textContent = button.dataset.installmentCode;
            document.getElementById('modalCustomer').textContent = button.dataset.customer;
            document.getElementById('modalMonthlyPayment').textContent = currency.format(Number(button.dataset.monthly));
            document.getElementById('modalCurrentBalance').textContent = currency.format(balance);
            document.getElementById('modalNextDue').textContent = button.dataset.nextDue;
            paymentAmount.value = Number(button.dataset.suggested).toFixed(2);
            paymentAmount.max = balance.toFixed(2);
            document.getElementById('modalPaymentMethod').value = 'Cash';
        });
    });

    paymentForm?.addEventListener('submit', async event => {
        event.preventDefault();
        paymentForm.classList.add('was-validated');
        if (!paymentForm.checkValidity()) return;

        recordButton.disabled = true;
        recordButton.querySelector('.spinner-border')?.classList.remove('d-none');

        try {
            const response = await fetch(page.dataset.recordUrl, {
                method: 'POST',
                body: new FormData(paymentForm)
            });
            const result = await response.json();
            if (!result.success) {
                showError(result.message || 'Unable to record the installment payment.');
                return;
            }

            redirectWithMessage(
                result.message || 'Payment recorded successfully.',
                page.dataset.returnUrl || result.redirectUrl || window.location.href);
        } catch {
            showError('Unable to record the installment payment.');
        } finally {
            recordButton.disabled = false;
            recordButton.querySelector('.spinner-border')?.classList.add('d-none');
        }
    });

    let rows = [...document.querySelectorAll('.installment-row')];
    const table = document.getElementById('installmentTable');
    const search = document.getElementById('installmentSearch');
    const progressFilter = document.getElementById('installmentProgressFilter');
    const statusTabs = [...document.querySelectorAll('.installment-status-tabs [data-status]')];
    const pageSize = document.getElementById('installmentPageSize');
    const pagination = document.getElementById('installmentPagination');
    const rangeText = document.getElementById('installmentRangeText');
    const emptyRow = document.getElementById('emptyInstallments');
    if (!search || !progressFilter || !pageSize || !pagination) return;

    let currentPage = 1;
    let selectedStatus = 'all';

    function matchingRows() {
        const term = search.value.trim().toLowerCase();
        return rows.filter(row =>
            (!term || row.dataset.search.includes(term)) &&
            (selectedStatus === 'all' || row.dataset.status === selectedStatus) &&
            (progressFilter.value === 'all' || row.dataset.progress === progressFilter.value));
    }

    function addPageButton(label, pageNumber, options = {}) {
        const item = document.createElement('li');
        const button = document.createElement('button');
        item.className = `page-item${options.disabled ? ' disabled' : ''}${options.active ? ' active' : ''}`;
        button.className = 'page-link';
        button.type = 'button';
        button.textContent = label;
        button.disabled = options.disabled ?? false;
        button.setAttribute('aria-label', options.ariaLabel || label);
        button.addEventListener('click', () => {
            currentPage = pageNumber;
            render();
        });
        item.appendChild(button);
        pagination.appendChild(item);
    }

    function render() {
        const filtered = matchingRows();
        const size = Number(pageSize.value);
        const pageCount = Math.max(1, Math.ceil(filtered.length / size));
        currentPage = Math.min(currentPage, pageCount);
        rows.forEach(row => row.classList.add('d-none'));

        const start = (currentPage - 1) * size;
        filtered.slice(start, start + size).forEach(row => row.classList.remove('d-none'));
        emptyRow?.classList.toggle('d-none', filtered.length > 0);
        if (rangeText) {
            rangeText.textContent = filtered.length
                ? `Showing ${start + 1}–${Math.min(start + size, filtered.length)} of ${filtered.length} installments`
                : 'Showing 0 installments';
        }

        pagination.innerHTML = '';
        addPageButton('‹', currentPage - 1, { disabled: currentPage === 1, ariaLabel: 'Previous page' });
        for (let number = 1; number <= pageCount; number += 1) {
            addPageButton(String(number), number, { active: number === currentPage, ariaLabel: `Page ${number}` });
        }
        addPageButton('›', currentPage + 1, { disabled: currentPage === pageCount, ariaLabel: 'Next page' });
    }

    async function deleteSelectedInstallments(event) {
        event.preventDefault();
        const ids = event.detail?.ids ?? [];
        if (!ids.length || !confirm(`Delete ${ids.length} selected installment details and their payment history? Sales records and inventory will remain unchanged.`)) return;

        try {
            const response = await fetch(page.dataset.bulkDeleteUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(ids)
            });
            const result = await response.json();
            if (result.success) {
                window.reloadWithToast(result.message || 'Selected installment details deleted.');
                return;
            }
            showError(result.message || 'Unable to delete the selected installment details.');
        } catch {
            showError('Unable to delete the selected installment details.');
        }
    }

    [search, progressFilter, pageSize].forEach(control => {
        control.addEventListener(control === search ? 'input' : 'change', () => {
            currentPage = 1;
            render();
        });
    });

    statusTabs.forEach(tab => tab.addEventListener('click', () => {
        selectedStatus = tab.dataset.status;
        currentPage = 1;
        statusTabs.forEach(item => {
            const active = item === tab;
            item.classList.toggle('active', active);
            item.setAttribute('aria-pressed', String(active));
        });
        render();
    }));

    rows.forEach(row => row.addEventListener('click', event => {
        if (!event.target.closest('a, button, input, label, .dropdown-menu')) {
            window.location.href = row.dataset.href;
        }
    }));

    table?.addEventListener('table:bulk-delete', deleteSelectedInstallments);
    table?.addEventListener('table:sorted', () => {
        rows = [...document.querySelectorAll('.installment-row')];
        currentPage = 1;
        render();
    });

    document.getElementById('exportInstallments')?.addEventListener('click', event => {
        const hasFilters = Boolean(search.value.trim()) || selectedStatus !== 'all' || progressFilter.value !== 'all';
        window.rjtechExcelExport.download(event.currentTarget, matchingRows(), hasFilters);
    });

    render();
})();
