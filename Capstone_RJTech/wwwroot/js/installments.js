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
    const recordButtonLabel = document.getElementById('recordButtonLabel');
    const paymentAmount = document.getElementById('modalPaymentAmount');
    const modalEl = document.getElementById('recordPaymentModal');
    const confirmationModal = document.getElementById('completeInstallmentModal');
    const confirmationButton = document.getElementById('confirmInstallmentPayment');
    const confirmationButtonLabel = document.getElementById('confirmPaymentButtonLabel');
    const confirmationCancelButton = document.getElementById('paymentConfirmationCancel');
    const confirmationSpinner = confirmationButton?.querySelector('.spinner-border');
    const successModal = document.getElementById('paymentSuccessModal');
    const viewInstallmentButton = document.getElementById('viewInstallmentButton');

    let modalBalance = 0;
    let modalMonthly = 0;
    let modalMonths = 0;
    let modalMonthsPaid = 0;
    let modalStatus = 'Active';
    let modalCustomer = '';
    let pendingPayment = null;
    let paymentResultAfterConfirmation = null;
    let paymentInFlight = false;
    let openConfirmationAfterRecordHidden = false;
    let confirmationClosingAction = null;
    let confirmationResultDelivered = false;
    let successNeedsRefresh = false;

    function getModal(element) {
        if (!element || !window.bootstrap?.Modal) return null;
        return window.bootstrap.Modal.getOrCreateInstance(element);
    }

    function showModal(element) {
        getModal(element)?.show();
    }

    function hideModal(element) {
        getModal(element)?.hide();
    }

    function isModalShown(element) {
        return Boolean(element?.classList.contains('show'));
    }

    function setConfirmationBusy(isBusy) {
        if (confirmationButton) {
            confirmationButton.disabled = isBusy;
            confirmationButton.setAttribute('aria-busy', String(isBusy));
        }
        if (confirmationCancelButton) confirmationCancelButton.disabled = isBusy;
        confirmationModal?.setAttribute('aria-busy', String(isBusy));
        confirmationSpinner?.classList.toggle('d-none', !isBusy);
        if (confirmationButtonLabel) {
            confirmationButtonLabel.textContent = isBusy ? 'Recording…' : 'Confirm Payment';
        }
    }

    function getInstallmentDetailsUrl(installmentId) {
        const baseUrl = page.dataset.detailsUrl || '/Installment/Details';
        const separator = baseUrl.includes('?') ? '&' : '?';
        return `${baseUrl}${separator}id=${encodeURIComponent(String(installmentId))}`;
    }

    function showError(message) {
        if (typeof window.showToast === 'function') window.showToast(message, 'error');
        else window.console?.error(message);
    }

    /* ---------- Masked amount input (thousands separators, 2 decimals) ---------- */

    function stripToDigitsAndDot(value) {
        let clean = value.replace(/[^\d.]/g, '');
        const dot = clean.indexOf('.');
        if (dot !== -1) {
            clean = clean.slice(0, dot + 1) + clean.slice(dot + 1).replace(/\./g, '');
        }
        return clean;
    }

    function formatMasked(value) {
        const clean = stripToDigitsAndDot(value);
        if (!clean) return '';
        const dot = clean.indexOf('.');
        const intRaw = dot === -1 ? clean : clean.slice(0, dot);
        const decRaw = dot === -1 ? '' : clean.slice(dot + 1).slice(0, 2);
        let int = intRaw.replace(/^0+(?=\d)/, '');
        if (!int) int = '0';
        int = int.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
        if (dot !== -1 && decRaw === '') return `${int}.`;
        return decRaw ? `${int}.${decRaw}` : int;
    }

    function offsetAfterDigits(text, digits) {
        let seen = 0;
        for (let i = 0; i < text.length; i += 1) {
            if (/\d/.test(text[i])) seen += 1;
            if (seen === digits) return i + 1;
        }
        return text.length;
    }

    function restoreCaret(formatted, digitsBefore, hadDot) {
        const dot = formatted.indexOf('.');
        if (hadDot && dot !== -1) {
            const intDigits = (formatted.slice(0, dot).match(/\d/g) || []).length;
            if (digitsBefore >= intDigits) {
                let consumed = intDigits;
                let position = dot + 1;
                for (let i = dot + 1; i < formatted.length && consumed < digitsBefore; i += 1) {
                    if (/\d/.test(formatted[i])) { consumed += 1; position = i + 1; }
                }
                return position;
            }
            return offsetAfterDigits(formatted.slice(0, dot), digitsBefore);
        }
        return offsetAfterDigits(formatted, digitsBefore);
    }

    paymentAmount?.addEventListener('input', () => {
        const previous = paymentAmount.value;
        const caret = paymentAmount.selectionStart ?? previous.length;
        let digitsBefore = 0;
        for (let i = 0; i < caret && i < previous.length; i += 1) {
            if (/\d/.test(previous[i])) digitsBefore += 1;
        }
        const hadDot = previous.slice(0, caret).includes('.');

        const formatted = formatMasked(previous);
        if (formatted !== previous) {
            paymentAmount.value = formatted;
            const position = restoreCaret(formatted, digitsBefore, hadDot);
            paymentAmount.setSelectionRange(position, position);
        }
        updateAmountFeedback();
    });

    function parseAmount() {
        const cleaned = paymentAmount ? paymentAmount.value.replace(/[^\d.]/g, '') : '';
        const value = Number(cleaned);
        return Number.isFinite(value) && value > 0 ? value : 0;
    }

    function isPayoffAmount(amount) {
        return amount > 0 && Math.round(amount * 100) >= Math.round(modalBalance * 100);
    }

    function setAmount(value) {
        if (!paymentAmount) return;
        paymentAmount.value = formatMasked((value || 0).toFixed(2));
        updateAmountFeedback();
        paymentAmount.focus();
    }

    /* ---------- Live summary + validation ---------- */

    function updateAmountFeedback() {
        if (!recordButton) return;
        const amount = parseAmount();
        const balanceAfter = Math.max(0, modalBalance - amount);

        const isPayoff = isPayoffAmount(amount);
        const afterEl = document.getElementById('modalBalanceAfter');
        if (afterEl) {
            afterEl.textContent = `Balance after this payment: ${currency.format(balanceAfter)}`;
            afterEl.classList.toggle('is-payoff', isPayoff);
        }

        const errorEl = document.getElementById('modalAmountError');
        if (errorEl) {
            if (amount > modalBalance) {
                errorEl.textContent = `Amount can't exceed the balance of ${currency.format(modalBalance)}.`;
            } else if (amount < 0.01) {
                errorEl.textContent = 'Enter a payment amount greater than ₱0.00.';
            } else {
                errorEl.textContent = '';
            }
        }

        const partialEl = document.getElementById('modalPartialNote');
        if (partialEl) {
            partialEl.classList.toggle('d-none', !(amount > 0 && !isPayoff && amount < modalMonthly));
        }

        const valid = amount >= 0.01 && amount <= modalBalance;
        recordButton.disabled = !valid;
        if (recordButtonLabel) {
            recordButtonLabel.textContent = valid ? `Record ${currency.format(amount)} Payment` : 'Record Payment';
        }
    }

    document.getElementById('quickFillMonthly')?.addEventListener('click', () => {
        setAmount(Math.min(modalMonthly, modalBalance));
    });
    document.getElementById('quickFillFull')?.addEventListener('click', () => {
        setAmount(modalBalance);
    });

    function isDueDateLate(iso, status) {
        if (!iso || status === 'Completed') return false;
        const due = new Date(`${iso}T00:00:00`);
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        return !Number.isNaN(due.getTime()) && due < today;
    }

    /* ---------- Modal open ---------- */

    document.querySelectorAll('.open-payment-modal').forEach(button => {
        button.addEventListener('click', () => {
            pendingPayment = null;
            paymentResultAfterConfirmation = null;
            paymentInFlight = false;
            openConfirmationAfterRecordHidden = false;
            confirmationClosingAction = null;
            confirmationResultDelivered = false;
            successNeedsRefresh = false;
            setConfirmationBusy(false);

            modalBalance = Number(button.dataset.balance) || 0;
            modalMonthly = Number(button.dataset.monthly) || 0;
            modalMonths = Number(button.dataset.months) || 0;
            modalMonthsPaid = Number(button.dataset.monthsPaid) || 0;
            modalStatus = button.dataset.status || 'Active';
            modalCustomer = button.dataset.customer || '';

            document.getElementById('modalInstallmentId').value = button.dataset.installmentId;
            document.getElementById('modalInstallmentCode').textContent = button.dataset.installmentCode;
            document.getElementById('modalCustomer').textContent = modalCustomer;
            document.getElementById('modalMonthlyPayment').textContent = currency.format(modalMonthly);
            document.getElementById('modalCurrentBalance').textContent = currency.format(modalBalance);

            const chip = document.getElementById('modalStatusChip');
            if (chip) {
                chip.textContent = modalStatus;
                chip.className = `installment-status pay-modal-status status-${modalStatus.toLowerCase()}`;
            }

            const nextDueEl = document.getElementById('modalNextDue');
            if (nextDueEl) {
                nextDueEl.textContent = button.dataset.nextDue || '—';
                nextDueEl.classList.toggle('is-late', isDueDateLate(button.dataset.nextDueIso, modalStatus));
            }

            const monthsRemaining = Math.max(0, modalMonths - modalMonthsPaid);
            const progressTextEl = document.getElementById('modalProgressText');
            if (progressTextEl) {
                progressTextEl.textContent = modalStatus === 'Completed'
                    ? `${modalMonths} of ${modalMonths} paid · fully paid`
                    : `${modalMonthsPaid} of ${modalMonths} paid · ${monthsRemaining} remaining`;
            }
            const percent = modalMonths > 0 ? Math.min(100, Math.round(modalMonthsPaid * 100 / modalMonths)) : 0;
            const fill = document.getElementById('modalProgressFill');
            const progressElement = fill?.closest('.progress');
            if (fill) fill.style.width = `${percent}%`;
            progressElement?.setAttribute('aria-valuenow', String(percent));
            progressElement?.setAttribute('aria-valuetext', `${modalMonthsPaid} of ${modalMonths} months paid`);

            const suggested = Math.min(modalMonthly, modalBalance);
            paymentAmount.value = formatMasked(suggested.toFixed(2));

            const monthlyChip = document.getElementById('quickFillMonthly');
            if (monthlyChip) {
                monthlyChip.innerHTML = `<i class="bi bi-calendar3 me-1" aria-hidden="true"></i>Monthly ${currency.format(Math.min(modalMonthly, modalBalance))}`;
            }
            const fullChip = document.getElementById('quickFillFull');
            if (fullChip) {
                fullChip.innerHTML = `<i class="bi bi-check2-circle me-1" aria-hidden="true"></i>Full balance ${currency.format(modalBalance)}`;
            }

            const errorEl = document.getElementById('modalAmountError');
            if (errorEl) errorEl.textContent = '';
            const partialEl = document.getElementById('modalPartialNote');
            if (partialEl) partialEl.classList.add('d-none');
            updateAmountFeedback();
        });
    });

    modalEl?.addEventListener('shown.bs.modal', () => {
        paymentAmount?.focus();
        paymentAmount?.select();
    });

    /* ---------- Confirmation + submit ---------- */

    function populatePaymentConfirmation(payment) {
        const isPayoff = payment.isPayoff;
        const amount = isPayoff ? modalBalance : payment.amount;
        const balanceAfter = Math.max(0, modalBalance - amount);
        const title = document.getElementById('paymentConfirmationTitle');
        const introLead = document.getElementById('paymentConfirmationLead');
        const introAmount = document.getElementById('paymentConfirmationAmount');
        const balance = document.getElementById('paymentConfirmationBalance');
        const status = document.getElementById('paymentConfirmationStatusEffect');
        const checkout = document.getElementById('paymentConfirmationCheckoutEffect');
        const warning = document.getElementById('paymentConfirmationWarning');
        const partialNote = document.getElementById('paymentConfirmationPartialNote');

        if (title) {
            title.textContent = isPayoff ? 'Complete Installment Plan?' : 'Record Installment Payment?';
        }
        if (introLead) {
            introLead.textContent = isPayoff
                ? 'You are about to pay the remaining balance of'
                : 'You are about to record a payment of';
        }
        if (introAmount) introAmount.textContent = currency.format(amount);
        if (balance) balance.textContent = currency.format(balanceAfter);
        if (status) {
            status.textContent = isPayoff
                ? 'Mark the installment plan as Completed'
                : `Keep the installment plan ${modalStatus}`;
        }
        if (checkout) {
            checkout.textContent = isPayoff
                ? 'Mark the associated checkout as Paid'
                : 'Keep the associated checkout Ongoing';
        }
        warning?.classList.toggle('d-none', !isPayoff);
        partialNote?.classList.toggle('d-none', isPayoff || amount >= modalMonthly);
    }

    function showPaymentSuccess(result) {
        const amount = Number(result.amount || 0);
        const balance = Number(result.balance || 0);
        const code = result.installmentCode
            || document.getElementById('modalInstallmentCode')?.textContent
            || '—';
        const installmentId = result.installmentID
            || document.getElementById('modalInstallmentId')?.value;

        const codeElement = document.getElementById('paymentSuccessInstallmentCode');
        const amountElement = document.getElementById('paymentSuccessAmount');
        const balanceElement = document.getElementById('paymentSuccessBalance');
        const statusElement = document.getElementById('paymentSuccessStatus');

        if (codeElement) codeElement.textContent = code;
        if (amountElement) amountElement.textContent = currency.format(amount);
        if (balanceElement) balanceElement.textContent = currency.format(balance);
        if (statusElement) statusElement.textContent = result.status || 'Completed';
        if (viewInstallmentButton && installmentId) {
            viewInstallmentButton.href = getInstallmentDetailsUrl(installmentId);
        }

        if (successModal) {
            showModal(successModal);
            return;
        }

        // Keep feedback available if the optional success dialog is not rendered.
        window.showToast?.(
            `Payment Recorded Successfully. ${code} is fully paid.`,
            'success'
        );
    }

    function showPartialPaymentSuccess(result, receiptContext) {
        const code = result.installmentCode
            || document.getElementById('modalInstallmentCode')?.textContent
            || '';
        const amount = Number(result.amount || 0);
        const message = `${currency.format(amount)} recorded for ${code}`;

        if (typeof window.showToast === 'function') {
            window.showToast(message, 'success', {
                label: 'Print receipt',
                onClick: () => printPaymentReceipt(result, receiptContext)
            });
        }
    }

    function closeConfirmationWithResult(action, paymentResult) {
        confirmationClosingAction = action;
        paymentResultAfterConfirmation = paymentResult;
        if (isModalShown(confirmationModal)) hideModal(confirmationModal);
        else deliverConfirmationResult(action, paymentResult);
    }

    function deliverConfirmationResult(action, paymentResult) {
        if (confirmationResultDelivered) return;
        confirmationResultDelivered = true;
        confirmationClosingAction = null;
        paymentResultAfterConfirmation = null;

        if (action === 'success' && paymentResult) {
            pendingPayment = null;
            showPaymentSuccess(paymentResult.result);
            return;
        }
        if (action === 'partial' && paymentResult) {
            pendingPayment = null;
            if (paymentResult.row) {
                showPartialPaymentSuccess(paymentResult.result, paymentResult.receiptContext);
            } else if (typeof window.reloadWithToast === 'function') {
                // No list row on this page (e.g. Details): refresh so summary/history match.
                window.reloadWithToast(
                    `${currency.format(Number(paymentResult.result.amount || 0))} recorded for ${paymentResult.result.installmentCode || ''}`,
                    'success'
                );
            } else {
                showPartialPaymentSuccess(paymentResult.result, paymentResult.receiptContext);
            }
            return;
        }

        // Cancel, Escape, or a failed request returns to the payment form.
        confirmationResultDelivered = false;
        pendingPayment = null;
        updateAmountFeedback();
        showModal(modalEl);
    }

    modalEl?.addEventListener('hidden.bs.modal', () => {
        if (!openConfirmationAfterRecordHidden) return;
        openConfirmationAfterRecordHidden = false;
        showModal(confirmationModal);
    });

    confirmationModal?.addEventListener('hide.bs.modal', event => {
        // Do not let Escape, a backdrop click, or Cancel interrupt a committed request.
        if (paymentInFlight && !confirmationClosingAction) event.preventDefault();
    });

    confirmationModal?.addEventListener('shown.bs.modal', () => {
        confirmationCancelButton?.focus();
    });

    confirmationModal?.addEventListener('hidden.bs.modal', () => {
        if (confirmationResultDelivered) return;
        if (paymentInFlight && !confirmationClosingAction) return;
        const action = confirmationClosingAction || 'record';
        deliverConfirmationResult(action, paymentResultAfterConfirmation);
    });

    successModal?.addEventListener('shown.bs.modal', () => {
        document.getElementById('paymentSuccessTitle')?.focus();
    });

    successModal?.addEventListener('hidden.bs.modal', () => {
        if (!successNeedsRefresh) return;
        successNeedsRefresh = false;
        window.location.reload();
    });

    viewInstallmentButton?.addEventListener('click', () => {
        // The browser is navigating to the details page; do not issue a second reload.
        successNeedsRefresh = false;
    });

    paymentForm?.addEventListener('submit', event => {
        event.preventDefault();
        const amount = parseAmount();
        if (paymentInFlight || !amount || amount > modalBalance || recordButton.disabled) return;

        confirmationResultDelivered = false;
        pendingPayment = {
            amount,
            isPayoff: isPayoffAmount(amount)
        };
        if (recordButton) recordButton.disabled = true;
        populatePaymentConfirmation(pendingPayment);
        openConfirmationAfterRecordHidden = true;
        hideModal(modalEl);
    });

    confirmationButton?.addEventListener('click', async () => {
        if (!pendingPayment || paymentInFlight || !paymentForm) return;

        const formData = new FormData(paymentForm);
        const receiptContext = {
            customer: modalCustomer,
            method: String(formData.get('PaymentMethod') || 'Cash')
        };

        paymentInFlight = true;
        setConfirmationBusy(true);
        recordButton?.setAttribute('aria-busy', 'true');
        if (recordButton) recordButton.disabled = true;

        try {
            const response = await fetch(page.dataset.recordUrl, {
                method: 'POST',
                body: formData,
                // The response is rendered in the custom success dialog below.
                skipToast: true
            });
            const contentType = response.headers.get('content-type') || '';
            if (!response.ok && !contentType.includes('application/json')) {
                const message = [401, 403, 419].includes(response.status)
                    ? 'Your session has expired. Please refresh the page and try again.'
                    : 'Unable to record the installment payment.';
                showError(message);
                closeConfirmationWithResult('record', null);
                return;
            }
            const result = await response.json();
            if (!result.success) {
                showError(result.message || 'Unable to record the installment payment.');
                closeConfirmationWithResult('record', null);
                return;
            }

            const row = result.installmentID
                ? document.querySelector(`.installment-row[data-record-id="${String(result.installmentID).replace(/[^0-9_-]/g, '')}"]`)
                : null;
            if (row) updateInstallmentRow(row, result);

            const completed = result.completed === true
                || Number(result.balance) <= 0
                || String(result.status || '').toLowerCase() === 'completed';
            const paymentResult = { result, row, receiptContext, completed };
            if (completed) successNeedsRefresh = !row;
            closeConfirmationWithResult(completed ? 'success' : 'partial', paymentResult);
        } catch {
            showError('Unable to record the installment payment.');
            closeConfirmationWithResult('record', null);
        } finally {
            paymentInFlight = false;
            setConfirmationBusy(false);
            recordButton?.removeAttribute('aria-busy');
            updateAmountFeedback();
        }
    });

    /* ---------- In-place row refresh + receipt ---------- */

    function updateInstallmentRow(row, result) {
        const cells = row.cells;
        if (cells.length < 9) return;

        const balanceCell = cells[4];
        balanceCell.textContent = `₱${Number(result.balance || 0).toLocaleString('en-PH', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        })}`;
        balanceCell.className = balanceCell.className.replace(
            /balance-[\w.]+/g,
            `balance-${String(result.status || '').toLowerCase()}`);

        const progressCell = cells[5];
        const months = (result.monthsPaid || 0) + (result.monthsRemaining || 0);
        const progressCopy = progressCell.querySelector('.progress-copy strong');
        if (progressCopy) progressCopy.textContent = `${result.monthsPaid || 0} / ${months}`;
        const progressBar = progressCell.querySelector('.progress-bar');
        const progressElement = progressCell.querySelector('.progress');
        if (progressBar) progressBar.style.width = `${result.progressPercentage || 0}%`;
        progressElement?.setAttribute('aria-valuenow', String(result.progressPercentage || 0));
        progressElement?.setAttribute('aria-valuetext', `${result.monthsPaid || 0} of ${months} months paid`);

        const dueCell = cells[6];
        dueCell.textContent = result.nextDue ? formatRowDate(result.nextDue) : '—';
        dueCell.title = result.completed && result.nextDue
            ? `Completed on ${formatRowDate(result.nextDue)}`
            : '';

        const statusCell = cells[7];
        const chip = statusCell.querySelector('.installment-status');
        if (chip) {
            chip.className = `installment-status status-${String(result.status || '').toLowerCase()}`;
            chip.textContent = result.status || '—';
        }

        const nextDue = result.nextDue || '';
        const nextDueDisplay = nextDue ? formatRowDate(nextDue) : '—';
        const paymentButton = row.querySelector('.open-payment-modal');
        row.dataset.balance = String(result.balance || 0);
        row.dataset.monthsPaid = String(result.monthsPaid || 0);
        row.dataset.monthsRemaining = String(result.monthsRemaining || 0);
        row.dataset.status = String(result.status || '').toLowerCase();
        row.dataset.progress = result.completed
            ? 'fully-paid'
            : (result.monthsPaid > 0 ? 'in-progress' : 'not-started');
        if (paymentButton) {
            paymentButton.dataset.balance = String(result.balance || 0);
            paymentButton.dataset.monthsPaid = String(result.monthsPaid || 0);
            paymentButton.dataset.monthsRemaining = String(result.monthsRemaining || 0);
            paymentButton.dataset.nextDue = nextDueDisplay;
            paymentButton.dataset.nextDueIso = nextDue;
            paymentButton.dataset.status = result.status || '';
        }

        if (result.completed) {
            paymentButton?.closest('li')?.remove();
        }

        // Keep active filters, pagination counts, and the range label in sync.
        render();
    }

    function formatRowDate(iso) {
        const date = new Date(`${iso}T00:00:00`);
        if (Number.isNaN(date.getTime())) return iso;
        return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    }

    function printPaymentReceipt(result, ctx) {
        const win = window.open('', '_blank', 'width=400,height=620');
        if (!win) {
            showError('Pop-up blocked. Allow pop-ups to print the receipt.');
            return;
        }

        const esc = value => String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/"/g, '&quot;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
        const line = (label, value) => (
            `<div class="rint-line"><span>${esc(label)}</span><strong>${esc(value)}</strong></div>`
        );

        win.document.write(`<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<title>Payment Receipt · ${esc(result.installmentCode || '')}</title>
<style>
    * { box-sizing: border-box; }
    body { font: 13px/1.5 'Segoe UI', Arial, sans-serif; width: 320px; margin: 24px auto; color: #101828; }
    .rint-head { text-align: center; border-bottom: 2px solid #0d6efd; padding-bottom: 12px; margin-bottom: 16px; }
    .rint-head h1 { margin: 0; font-size: 18px; color: #0d6efd; }
    .rint-head p { margin: 2px 0 0; color: #667085; font-size: 11px; }
    .rint-badge { display: inline-block; margin-top: 6px; padding: 2px 10px; border-radius: 999px; background: #0d6efd; color: #fff; font-size: 10px; }
    .rint-line { display: flex; justify-content: space-between; gap: 12px; padding: 6px 0; border-bottom: 1px dashed #eaecf0; }
    .rint-line span { color: #667085; }
    .rint-total { margin-top: 12px; padding: 10px; background: #eef4ff; border-radius: 8px; font-size: 15px; font-weight: 650; }
    .rint-foot { margin-top: 16px; text-align: center; color: #98a2b3; font-size: 11px; }
</style></head><body>
    <div class="rint-head">
        <h1>RJTech · Payment Receipt</h1>
        <p>Installment payment</p>
        <span class="rint-badge">${esc(result.installmentCode || '')}</span>
    </div>
    ${line('Receipt No.', `PMT-${String(result.paymentID || '').padStart(5, '0')}`)}
    ${line('Customer', esc(ctx.customer))}
    ${line('Payment method', esc(ctx.method))}
    ${line('Paid on', new Date().toLocaleString('en-PH'))}
    ${line('Payment amount', currency.format(Number(result.amount) || 0))}
    ${line('Balance after', currency.format(Number(result.balance) || 0))}
    <div class="rint-total">${result.completed
        ? '✓ Plan fully paid'
        : `Remaining balance: ${currency.format(Number(result.balance) || 0)}`}</div>
    <div class="rint-foot">Thank you! Keep this receipt for your records.</div>
</body></html>`);
        win.document.close();
        win.focus();
        win.print();
    }

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
                body: JSON.stringify(ids),
                skipToast: true
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
