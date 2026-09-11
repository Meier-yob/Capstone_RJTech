(() => {
    const page = document.getElementById('checkoutPage');
    const customerId = document.getElementById('customerId');
    const customerSearch = document.getElementById('customerSearch');
    const customerName = document.getElementById('customerFullName');
    const customerEmail = document.getElementById('customerEmail');
    const customerPhone = document.getElementById('customerPhone');
    const customerAddress = document.getElementById('customerAddress');
    const customerResults = document.getElementById('customerSearchResults');
    const productSearch = document.getElementById('productSearch');
    const productResults = document.getElementById('productSearchResults');
    const checkoutItemsBody = document.getElementById('checkoutItemsBody');
    const emptyItems = document.getElementById('emptyCheckoutItems');
    const itemsFooter = document.getElementById('checkoutItemsFooter');
    const itemsPagination = document.getElementById('checkoutItemsPagination');
    const itemsRange = document.getElementById('checkoutItemsRange');
    const completeButton = document.getElementById('completeCheckout');
    const paymentTypeInputs = [...document.querySelectorAll('input[name="paymentType"]')];
    const installmentFields = document.getElementById('installmentFields');
    const installmentSummary = document.getElementById('installmentSummary');
    const installmentMonths = document.getElementById('installmentMonths');
    const downPayment = document.getElementById('downPayment');
    const interestRate = document.getElementById('interestRate');
    const paymentMethod = document.getElementById('paymentMethod');
    const exitModal = bootstrap.Modal.getOrCreateInstance(document.getElementById('checkoutExitModal'));
    const currency = new Intl.NumberFormat('en-PH', { style: 'currency', currency: 'PHP' });
    const fixedDownPaymentRate = 0.20;
    const fixedMonthlyInterestRate = 5;
    const allowedInstallmentTerms = [6, 12, 18, 24];

    function productIdOf(product) {
        const value = Number(product?.productId ?? product?.productID ?? product?.ProductID);
        return Number.isInteger(value) && value > 0 ? value : null;
    }

    function normalizeProductOption(product) {
        return {
            ...product,
            productId: productIdOf(product),
            code: product.code ?? product.productCode ?? product.product_code ?? '',
            name: product.name ?? product.productName ?? '',
            brand: product.brand ?? product.productBrand ?? '',
            category: product.category ?? product.productCategory ?? 'Uncategorized',
            stock: Number(product.stock ?? product.availableStock ?? 0),
            price: Number(product.price ?? 0),
            imageUrl: product.imageUrl ?? ''
        };
    }

    function productsMatch(item, product) {
        const itemId = productIdOf(item);
        const productId = productIdOf(product);
        if (itemId && productId) return itemId === productId;

        const itemCode = String(item.productCode ?? item.code ?? '').trim().toLowerCase();
        const productCode = String(product.productCode ?? product.code ?? '').trim().toLowerCase();
        if (itemCode && productCode) return itemCode === productCode;

        const itemName = String(item.productName ?? item.name ?? '').trim().toLowerCase();
        const productName = String(product.productName ?? product.name ?? '').trim().toLowerCase();
        return Boolean(itemName && productName && itemName === productName);
    }

    function syncProductSelectionState() {
        productResults.querySelectorAll('.checkout-product-card').forEach(card => {
            let product;
            try {
                product = normalizeProductOption(JSON.parse(card.dataset.product));
            } catch {
                return;
            }

            const isSelected = checkoutItems.some(item => productsMatch(item, product));
            card.classList.toggle('is-selected', isSelected);
            card.setAttribute('aria-pressed', String(isSelected));
        });
    }

    // Products live only in this array until the final checkout request succeeds.
    let checkoutItems = JSON.parse(document.getElementById('initialCheckoutItems').textContent || '[]')
        .map(item => ({
            ...item,
            productId: productIdOf(item),
            productCode: item.productCode ?? item.code ?? '',
            quantity: Math.max(1, Number(item.quantity) || 1),
            serialNumbers: Array.isArray(item.serialNumbers) ? item.serialNumbers : []
        }));
    let visibleProducts = JSON.parse(
        document.getElementById('initialCheckoutProducts')?.textContent || '[]')
        .map(normalizeProductOption);
    let selectedItemsPage = 1;
    let customerSearchTimer;
    let customerRequestId = 0;
    let productSearchTimer;
    let productRequestId = 0;
    let dirty = false;
    let pendingExitUrl = '';
    const serialValidationStates = new Map();

    function escapeHtml(value) {
        return String(value ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#039;');
    }

    function showError(message) {
        window.showToast(message, 'error');
    }

    function clearCustomerInformation() {
        customerSearch.value = '';
        customerId.value = '';
        customerName.value = '';
        customerEmail.value = '';
        customerPhone.value = '';
        customerAddress.value = '';
        customerResults.innerHTML = '';
        customerResults.classList.add('d-none');
        customerRequestId += 1;
        validateEmailFormat();
        validatePhoneFormat();
        dirty = true;
        validateInstallmentCheckout();
    }

    function checkoutTotalValue() {
        return checkoutItems.reduce((sum, item) => sum + (item.price * item.quantity), 0);
    }

    function selectedPaymentType() {
        return paymentTypeInputs.find(input => input.checked)?.value || '';
    }

    function roundMoney(value) {
        return Math.round((Number(value) + Number.EPSILON) * 100) / 100;
    }

    function calculateInstallment() {
        const originalAmount = roundMoney(checkoutTotalValue());
        const months = Number(installmentMonths.value);
        const downPaymentAmount = roundMoney(originalAmount * fixedDownPaymentRate);
        const termIsValid = installmentMonths.value !== '' &&
            allowedInstallmentTerms.includes(months);
        const calculationIsReady = Number.isFinite(originalAmount) && originalAmount > 0 && termIsValid;

        downPayment.value = downPaymentAmount.toFixed(2);
        downPayment.setCustomValidity('');
        interestRate.value = fixedMonthlyInterestRate.toFixed(2);

        const remainingPrincipal = Number.isFinite(originalAmount) && originalAmount > 0
            ? roundMoney(originalAmount - downPaymentAmount)
            : 0;
        const interestAmount = calculationIsReady
            ? roundMoney(remainingPrincipal * (fixedMonthlyInterestRate / 100) * months)
            : 0;
        const installmentTotal = calculationIsReady
            ? roundMoney(remainingPrincipal + interestAmount)
            : 0;
        const monthlyPayment = calculationIsReady
            ? roundMoney(installmentTotal / months)
            : 0;

        document.getElementById('installmentOriginal').textContent = currency.format(originalAmount);
        document.getElementById('installmentDownPayment').textContent = `-${currency.format(downPaymentAmount)}`;
        document.getElementById('installmentPrincipal').textContent = currency.format(remainingPrincipal);
        document.getElementById('installmentInterest').textContent = currency.format(interestAmount);
        document.getElementById('installmentTotal').textContent = currency.format(installmentTotal);
        document.getElementById('monthlyPayment').textContent = currency.format(monthlyPayment);
        document.getElementById('summaryMonths').textContent = String(months);
        document.getElementById('summaryMonthsRemaining').textContent = String(months);

        return {
            originalAmount,
            months,
            downPaymentAmount,
            remainingPrincipal,
            interestAmount,
            installmentTotal,
            monthlyPayment,
            termIsValid,
            calculationIsReady
        };
    }

    function updatePaymentType() {
        const isInstallment = selectedPaymentType() === 'Installment';
        installmentFields.classList.toggle('d-none', !isInstallment);
        installmentSummary.classList.toggle('d-none', !isInstallment);
        const checkoutStatus = document.getElementById('checkoutStatus');
        checkoutStatus.textContent = isInstallment ? 'Ongoing' : 'Paid';
        checkoutStatus.classList.toggle('status-paid', !isInstallment);
        checkoutStatus.classList.toggle('status-ongoing', isInstallment);
        document.getElementById('checkoutSubmitLabel').textContent = isInstallment
            ? 'Create Installment'
            : 'Complete Checkout';
        validateInstallmentCheckout();
    }

    function addPageButton(label, pageNumber, options = {}) {
        const item = document.createElement('li');
        const button = document.createElement('button');
        item.className = `page-item${options.disabled ? ' disabled' : ''}${options.active ? ' active' : ''}`;
        button.className = 'page-link';
        button.type = 'button';
        button.textContent = label;
        button.disabled = options.disabled ?? false;
        button.addEventListener('click', () => { selectedItemsPage = pageNumber; renderItems(); });
        item.appendChild(button);
        itemsPagination.appendChild(item);
    }

    function normalizeSerial(value) {
        return String(value || '').trim().toUpperCase();
    }

    function resizeSerialNumbers(item, quantity) {
        const current = Array.isArray(item.serialNumbers) ? item.serialNumbers : [];
        item.serialNumbers = Array.from({ length: quantity }, (_, index) => current[index] || '');
    }

    function setItemQuantity(itemIndex, requestedQuantity) {
        const item = checkoutItems[itemIndex];
        if (!item) return;

        const maximum = Math.max(1, Number(item.availableStock) || 1);
        const quantity = Math.min(maximum, Math.max(1, Number.parseInt(requestedQuantity, 10) || 1));
        item.quantity = quantity;
        resizeSerialNumbers(item, quantity);
        dirty = true;
        renderItems();
    }

    function selectedItemsValidationState() {
        const serials = checkoutItems.flatMap(item => item.serialNumbers.map(normalizeSerial));
        const renderedQuantitiesMatchState = [...checkoutItemsBody.querySelectorAll('.checkout-item-quantity')]
            .every(input => input.checkValidity() && input.value !== '' &&
                Number(input.value) === checkoutItems[Number(input.dataset.itemIndex)]?.quantity);
        const quantitiesAreValid = checkoutItems.every(item =>
            Number.isInteger(item.quantity) &&
            item.quantity >= 1 &&
            item.quantity <= item.availableStock &&
            item.serialNumbers.length === item.quantity);
        const serialsAreComplete = serials.length > 0 && serials.every(Boolean);
        const serialsAreUnique = new Set(serials).size === serials.length;
        const databaseChecksPassed = serials.every(serial => serialValidationStates.get(serial) === 'unique');
        return {
            hasProducts: checkoutItems.length > 0,
            quantitiesAreValid: renderedQuantitiesMatchState && quantitiesAreValid,
            serialsAreComplete,
            serialsAreUnique,
            databaseChecksPassed
        };
    }

    function updateSerialIndicators() {
        const serialCounts = new Map();
        checkoutItems
            .flatMap(item => item.serialNumbers)
            .map(normalizeSerial)
            .filter(Boolean)
            .forEach(serial => serialCounts.set(serial, (serialCounts.get(serial) || 0) + 1));

        checkoutItemsBody.querySelectorAll('.checkout-item-serial').forEach(input => {
            const itemIndex = Number(input.dataset.itemIndex);
            const serialIndex = Number(input.dataset.serialIndex);
            const serial = normalizeSerial(checkoutItems[itemIndex]?.serialNumbers[serialIndex]);
            const repeated = Boolean(serial && serialCounts.get(serial) > 1);
            const state = serial ? serialValidationStates.get(serial) : undefined;
            const duplicateInDatabase = state === 'duplicate';
            const indicator = input.parentElement.querySelector('.serial-duplicate-indicator');

            input.classList.toggle('is-invalid', repeated || duplicateInDatabase);
            indicator.textContent = duplicateInDatabase
                ? 'This serial number was already used.'
                : repeated
                    ? 'This serial number is already added.'
                    : state === 'pending'
                        ? 'Checking serial number...'
                        : state === 'unique'
                            ? 'Serial number is unique.'
                            : '';
            indicator.classList.toggle('is-duplicate', repeated || duplicateInDatabase);
            indicator.classList.toggle('is-unique', state === 'unique' && !repeated);
        });

    }

    function renderItems() {
        const pageSize = 10;
        const pageCount = Math.max(1, Math.ceil(checkoutItems.length / pageSize));
        selectedItemsPage = Math.min(selectedItemsPage, pageCount);
        const start = (selectedItemsPage - 1) * pageSize;
        const visibleItems = checkoutItems.slice(start, start + pageSize);

        checkoutItemsBody.innerHTML = visibleItems.map((item, pageIndex) => {
            const itemIndex = start + pageIndex;
            resizeSerialNumbers(item, item.quantity);
            return `
                <tr class="checkout-selected-item" data-index="${itemIndex}">
                    <td>
                        <div class="checkout-selected-product-cell">
                            <span class="checkout-selected-thumb">
                                <i class="bi bi-box-seam"></i>
                                <img src="${escapeHtml(item.imageUrl || '')}" alt="" loading="lazy" />
                            </span>
                            <div class="checkout-selected-product">
                            <strong>${escapeHtml(item.productName)}</strong>
                                <span>${escapeHtml(item.productCode || '')}${item.productBrand ? ` · ${escapeHtml(item.productBrand)}` : ''}</span>
                            </div>
                        </div>
                    </td>
                    <td>
                        <div class="checkout-row-serials">
                            ${item.serialNumbers.map((serial, serialIndex) => `
                                <div class="checkout-serial-field">
                                    <label class="visually-hidden" for="checkoutSerial-${item.productId}-${serialIndex}">Serial Number ${serialIndex + 1}</label>
                                    <input id="checkoutSerial-${item.productId}-${serialIndex}" class="form-control checkout-item-serial"
                                           type="text" maxlength="100" required autocomplete="off"
                                           data-item-index="${itemIndex}" data-serial-index="${serialIndex}"
                                           value="${escapeHtml(serial)}" placeholder="Serial Number ${serialIndex + 1}" />
                                    <div class="serial-duplicate-indicator" aria-live="polite"></div>
                                </div>`).join('')}
                        </div>
                    </td>
                    <td>
                            <div class="checkout-quantity-control">
                                <button class="btn checkout-quantity-button" type="button" data-quantity-action="decrease"
                                        aria-label="Decrease ${escapeHtml(item.productName)} quantity" ${item.quantity <= 1 ? 'disabled' : ''}>−</button>
                                <input class="form-control checkout-item-quantity" type="number" min="1" max="${item.availableStock}"
                                       step="1" value="${item.quantity}" data-item-index="${itemIndex}"
                                       aria-label="${escapeHtml(item.productName)} quantity" />
                                <button class="btn checkout-quantity-button" type="button" data-quantity-action="increase"
                                        aria-label="Increase ${escapeHtml(item.productName)} quantity" ${item.quantity >= item.availableStock ? 'disabled' : ''}>+</button>
                            </div>
                    </td>
                    <td class="text-end checkout-item-price">${currency.format(item.price)}</td>
                    <td class="text-end checkout-item-subtotal">${currency.format(item.price * item.quantity)}</td>
                    <td class="text-end">
                        <button class="btn btn-sm btn-outline-danger remove-checkout-item" type="button"
                                aria-label="Remove ${escapeHtml(item.productName)}"><i class="bi bi-trash"></i></button>
                    </td>
                </tr>`;
        }).join('');

        checkoutItemsBody.querySelectorAll('.checkout-selected-thumb img').forEach(image => {
            image.addEventListener('error', () => image.classList.add('d-none'));
        });

        checkoutItemsBody.querySelectorAll('.remove-checkout-item').forEach(button => {
            button.addEventListener('click', event => {
                const index = Number(event.currentTarget.closest('.checkout-selected-item').dataset.index);
                checkoutItems.splice(index, 1);
                dirty = true;
                syncProductSelectionState();
                renderItems();
            });
        });

        checkoutItemsBody.querySelectorAll('.checkout-quantity-button').forEach(button => {
            button.addEventListener('click', event => {
                const selectedItem = event.currentTarget.closest('.checkout-selected-item');
                const itemIndex = Number(selectedItem.dataset.index);
                const change = event.currentTarget.dataset.quantityAction === 'increase' ? 1 : -1;
                setItemQuantity(itemIndex, checkoutItems[itemIndex].quantity + change);
            });
        });

        checkoutItemsBody.querySelectorAll('.checkout-item-quantity').forEach(input => {
            input.addEventListener('input', event => {
                const quantityInput = event.currentTarget;
                dirty = true;
                validateInstallmentCheckout();
                window.clearTimeout(quantityInput.quantityTimer);
                quantityInput.quantityTimer = window.setTimeout(
                    () => setItemQuantity(Number(quantityInput.dataset.itemIndex), quantityInput.value), 250);
            });
            input.addEventListener('change', event => {
                window.clearTimeout(event.currentTarget.quantityTimer);
                setItemQuantity(Number(event.currentTarget.dataset.itemIndex), event.currentTarget.value);
            });
        });

        checkoutItemsBody.querySelectorAll('.checkout-item-serial').forEach(input => {
            input.addEventListener('input', event => {
                const currentInput = event.currentTarget;
                const itemIndex = Number(currentInput.dataset.itemIndex);
                const serialIndex = Number(currentInput.dataset.serialIndex);
                const serial = normalizeSerial(currentInput.value);
                checkoutItems[itemIndex].serialNumbers[serialIndex] = serial;
                dirty = true;

                if (serial) serialValidationStates.set(serial, 'pending');
                window.clearTimeout(currentInput.serialCheckTimer);
                currentInput.serialCheckTimer = window.setTimeout(
                    () => checkSerialInput(currentInput, itemIndex, serialIndex), 350);
                updateSerialIndicators();
                validateInstallmentCheckout();
            });
        });

        emptyItems.classList.toggle('d-none', checkoutItems.length > 0);
        itemsFooter.classList.toggle('d-none', checkoutItems.length === 0);
        document.getElementById('checkoutItemsTotalBar').classList.toggle('d-none', checkoutItems.length === 0);
        document.getElementById('selectedItemsCount').textContent = `${checkoutItems.length} item${checkoutItems.length === 1 ? '' : 's'}`;
        document.getElementById('selectedItemsTotal').textContent = currency.format(checkoutTotalValue());
        document.getElementById('summaryItemCount').textContent = checkoutItems.reduce((sum, item) => sum + item.quantity, 0);
        document.getElementById('checkoutTotal').textContent = currency.format(checkoutTotalValue());
        itemsRange.textContent = checkoutItems.length
            ? `Showing ${start + 1}–${Math.min(start + pageSize, checkoutItems.length)} of ${checkoutItems.length}`
            : 'Showing 0 items';
        itemsPagination.innerHTML = '';
        addPageButton('Previous', selectedItemsPage - 1, { disabled: selectedItemsPage === 1 });
        for (let pageNumber = 1; pageNumber <= pageCount; pageNumber += 1) {
            addPageButton(String(pageNumber), pageNumber, { active: pageNumber === selectedItemsPage });
        }
        addPageButton('Next', selectedItemsPage + 1, { disabled: selectedItemsPage === pageCount });
        updateSerialIndicators();
        validateInstallmentCheckout();
    }

    function validateInstallmentCheckout() {
        validateEmailFormat();
        validatePhoneFormat();

        const calculation = calculateInstallment();
        const itemState = selectedItemsValidationState();
        const customerIsValid = [customerName, customerEmail, customerPhone, customerAddress]
            .every(input => input.value.trim() && input.checkValidity());
        const paymentType = selectedPaymentType();
        const paymentTypeIsValid = paymentType === 'Full Payment' || paymentType === 'Installment';
        const paymentMethodIsValid = paymentMethod.value.trim() !== '' && paymentMethod.checkValidity();
        const expectedDownPayment = roundMoney(calculation.originalAmount * fixedDownPaymentRate);
        const downPaymentValue = Number(downPayment.value);
        const interestRateValue = Number(interestRate.value);
        const expectedRemainingPrincipal = roundMoney(calculation.originalAmount - expectedDownPayment);
        const expectedInterest = calculation.termIsValid
            ? roundMoney(expectedRemainingPrincipal * (fixedMonthlyInterestRate / 100) * calculation.months)
            : 0;
        const expectedInstallmentTotal = calculation.termIsValid
            ? roundMoney(expectedRemainingPrincipal + expectedInterest)
            : 0;
        const expectedMonthlyPayment = calculation.termIsValid
            ? roundMoney(expectedInstallmentTotal / calculation.months)
            : 0;
        const installmentValues = [
            calculation.originalAmount,
            downPaymentValue,
            calculation.remainingPrincipal,
            interestRateValue,
            calculation.interestAmount,
            calculation.installmentTotal,
            calculation.monthlyPayment
        ];
        const installmentFieldsAreFinite = installmentValues.every(Number.isFinite);
        const downPaymentIsValid = downPayment.checkValidity() &&
            downPaymentValue === expectedDownPayment &&
            calculation.downPaymentAmount === expectedDownPayment;
        const installmentCalculationIsValid = calculation.calculationIsReady &&
            calculation.termIsValid &&
            calculation.originalAmount > 0 &&
            downPaymentIsValid &&
            calculation.remainingPrincipal === expectedRemainingPrincipal &&
            calculation.remainingPrincipal > 0 &&
            interestRate.checkValidity() &&
            interestRateValue === fixedMonthlyInterestRate &&
            calculation.interestAmount === expectedInterest &&
            calculation.installmentTotal === expectedInstallmentTotal &&
            calculation.installmentTotal > 0 &&
            calculation.monthlyPayment === expectedMonthlyPayment &&
            calculation.monthlyPayment > 0 &&
            installmentFieldsAreFinite;
        const installmentIsValid = paymentType !== 'Installment' || installmentCalculationIsValid;
        const itemsAreValid = itemState.hasProducts && itemState.quantitiesAreValid &&
            itemState.serialsAreComplete && itemState.serialsAreUnique &&
            itemState.databaseChecksPassed;
        const checkoutIsValid = customerIsValid && itemsAreValid && paymentTypeIsValid &&
            paymentMethodIsValid && installmentIsValid;

        completeButton.disabled = !checkoutIsValid;
        return checkoutIsValid;
    }

    function renderCustomerResults(customers) {
        if (!customers.length) {
            customerResults.innerHTML = '<div class="live-search-empty">No existing customer found. Continue entering the new customer details.</div>';
        } else {
            customerResults.innerHTML = customers.map(customer => `
                <button class="live-search-option customer-result" type="button"
                        data-id="${customer.customerId}" data-name="${escapeHtml(customer.fullName)}"
                        data-email="${escapeHtml(customer.email)}" data-phone="${escapeHtml(customer.phone)}"
                        data-address="${escapeHtml(customer.address)}">
                    <strong>${escapeHtml(customer.fullName)}</strong>
                    <span>${escapeHtml(customer.email)} · ${escapeHtml(customer.phone)}</span>
                </button>`).join('');
        }
        customerResults.classList.remove('d-none');

        customerResults.querySelectorAll('.customer-result').forEach(button => {
            button.addEventListener('click', () => {
                customerId.value = button.dataset.id;
                customerSearch.value = button.dataset.name;
                customerName.value = button.dataset.name;
                customerEmail.value = button.dataset.email;
                customerPhone.value = button.dataset.phone;
                customerAddress.value = button.dataset.address;
                validateEmailFormat();
                validatePhoneFormat();
                customerResults.classList.add('d-none');
                dirty = true;
                validateInstallmentCheckout();
            });
        });
    }

    async function searchCustomers() {
        const requestId = ++customerRequestId;
        const url = new URL(page.dataset.customerSearchUrl, window.location.origin);
        url.searchParams.set('query', customerSearch.value.trim());
        const response = await fetch(url);
        const result = await response.json();
        if (requestId !== customerRequestId) return;
        if (result.success) renderCustomerResults(result.customers);
    }

    function renderProductResults(products) {
        products = products.map(normalizeProductOption);
        visibleProducts = products;
        if (!products.length) {
            productResults.innerHTML = '<div class="checkout-product-empty">No available products match this search.</div>';
        } else {
            productResults.innerHTML = products.map(product => {
                const isSelected = checkoutItems.some(item => productsMatch(item, product));
                return `
                <article class="checkout-product-card${isSelected ? ' is-selected' : ''}"
                         tabindex="0" role="button" aria-pressed="${isSelected}"
                         data-product='${escapeHtml(JSON.stringify(product))}'>
                    <span class="checkout-product-selected-overlay" aria-hidden="true">
                        <i class="bi bi-check-circle-fill"></i>
                        <span>Selected</span>
                    </span>
                    <div class="checkout-product-row">
                        <span class="checkout-product-thumb">
                            <i class="bi bi-box-seam"></i>
                            <img src="${escapeHtml(product.imageUrl || '')}" alt="" loading="lazy" />
                        </span>
                        <div class="checkout-product-copy">
                            <strong>${escapeHtml(product.name)}</strong>
                            <small>${escapeHtml(product.code)} · ${escapeHtml(product.category)} · ${escapeHtml(product.brand)}</small>
                        </div>
                        <div class="checkout-product-summary">
                            <strong>${Number(product.stock)} available</strong>
                            <span>${currency.format(product.price)}</span>
                        </div>
                    </div>
                </article>`;
            }).join('');
        }
        productResults.classList.remove('d-none');

        productResults.querySelectorAll('.checkout-product-thumb img').forEach(image => {
            image.addEventListener('error', () => image.classList.add('d-none'));
        });

        productResults.querySelectorAll('.checkout-product-card').forEach(card => {
            const toggle = () => toggleProductInCheckout(JSON.parse(card.dataset.product));
            card.addEventListener('click', toggle);
            card.addEventListener('keydown', event => {
                if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    toggle();
                }
            });
        });
        syncProductSelectionState();
    }

    async function searchProducts(showLoading = false) {
        const requestId = ++productRequestId;
        if (showLoading === true) {
            productResults.innerHTML = `
                <div class="checkout-product-empty checkout-product-loading">
                    <span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>
                    Loading available products...
                </div>`;
            productResults.classList.remove('d-none');
        }

        const url = new URL(page.dataset.productSearchUrl, window.location.origin);
        url.searchParams.set('query', productSearch.value.trim());
        try {
            const response = await fetch(url);
            if (!response.ok) throw new Error(`Product request failed with ${response.status}.`);
            const result = await response.json();
            if (requestId !== productRequestId) return;
            if (result.success) {
                renderProductResults(result.products);
                return;
            }
            throw new Error(result.message || 'Unable to load products.');
        } catch {
            if (requestId !== productRequestId) return;
            productResults.innerHTML = '<div class="checkout-product-empty">Unable to load available products. Try searching again.</div>';
            productResults.classList.remove('d-none');
        }
    }

    async function serialExistsInDatabase(serial) {
        const url = new URL(page.dataset.serialCheckUrl, window.location.origin);
        url.searchParams.set('serialNumber', serial);
        const response = await fetch(url);
        const result = await response.json();
        return result.success && result.duplicate;
    }

    async function checkSerialInput(input, itemIndex, serialIndex) {
        const serial = normalizeSerial(input.value);
        let duplicate = false;
        try {
            duplicate = serial ? await serialExistsInDatabase(serial) : false;
        } catch {
            if (serial) serialValidationStates.delete(serial);
            updateSerialIndicators();
            validateInstallmentCheckout();
            return;
        }
        if (normalizeSerial(checkoutItems[itemIndex]?.serialNumbers[serialIndex]) !== serial) return;
        serialValidationStates.set(serial, duplicate ? 'duplicate' : 'unique');
        updateSerialIndicators();
        validateInstallmentCheckout();
    }

    function addProductToCheckout(product) {
        product = normalizeProductOption(product);
        if (checkoutItems.some(item => productsMatch(item, product))) return;

        checkoutItems.push({
            productId: product.productId,
            productName: product.name,
            productCode: product.code,
            productBrand: product.brand,
            productCategory: product.category,
            imageUrl: product.imageUrl,
            serialNumbers: [''],
            quantity: 1,
            price: Number(product.price),
            availableStock: Number(product.stock)
        });
        dirty = true;
        selectedItemsPage = Math.ceil(checkoutItems.length / 10);
        syncProductSelectionState();
        renderItems();
        window.showToast(`${product.name} added to Selected Items.`, 'success');
    }

    function toggleProductInCheckout(product) {
        product = normalizeProductOption(product);
        const selectedIndex = checkoutItems.findIndex(item => productsMatch(item, product));
        if (selectedIndex < 0) {
            addProductToCheckout(product);
            return;
        }

        const [removed] = checkoutItems.splice(selectedIndex, 1);
        dirty = true;
        syncProductSelectionState();
        renderItems();
        window.showToast(`${removed.productName} removed from Selected Items.`, 'success');
    }

    function validateCustomerFields() {
        validateEmailFormat();
        validatePhoneFormat();
        for (const input of [customerName, customerEmail, customerPhone, customerAddress]) {
            if (!input.checkValidity() || !input.value.trim()) {
                input.reportValidity();
                return false;
            }
        }
        return true;
    }

    function validateSelectedItems() {
        const invalidQuantity = checkoutItems.find(item =>
            !Number.isInteger(item.quantity) || item.quantity < 1 || item.quantity > item.availableStock);
        if (invalidQuantity) {
            showError(`Enter a quantity between 1 and ${invalidQuantity.availableStock} for ${invalidQuantity.productName}.`);
            return false;
        }

        const serials = checkoutItems.flatMap(item => item.serialNumbers.map(normalizeSerial));
        if (serials.some(serial => !serial)) {
            showError('Enter one serial number for every selected product unit.');
            checkoutItemsBody.querySelector('.checkout-item-serial:invalid')?.reportValidity();
            return false;
        }
        if (new Set(serials).size !== serials.length) {
            showError('Duplicate serial numbers are not allowed.');
            updateSerialIndicators();
            return false;
        }
        if (serials.some(serial => serialValidationStates.get(serial) !== 'unique')) {
            showError('Wait for all serial numbers to finish validation and correct any duplicates.');
            return false;
        }
        return true;
    }

    async function saveCheckout() {
        if (!validateCustomerFields()) return;
        if (!checkoutItems.length) {
            showError('Add at least one product to the order.');
            return;
        }
        if (!validateSelectedItems()) return;
        if (!validateInstallmentCheckout()) return;

        completeButton.disabled = true;
        completeButton.querySelector('.spinner-border').classList.remove('d-none');

        const payload = {
            customerId: customerId.value ? Number(customerId.value) : null,
            customerFullName: customerName.value.trim(),
            customerEmail: customerEmail.value.trim(),
            customerPhone: customerPhone.value.trim(),
            customerAddress: customerAddress.value.trim(),
            paymentType: selectedPaymentType(),
            paymentMethod: document.getElementById('paymentMethod').value,
            installmentMonths: selectedPaymentType() === 'Installment' ? Number(installmentMonths.value) : null,
            downPayment: selectedPaymentType() === 'Installment' ? Number(downPayment.value) : null,
            interestRate: selectedPaymentType() === 'Installment' ? 5 : null,
            items: checkoutItems.map(item => ({
                productId: item.productId,
                quantity: item.quantity,
                serialNumbers: item.serialNumbers
            }))
        };

        try {
            const response = await fetch(page.dataset.saveUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            const result = await response.json();

            if (result.success) {
                dirty = false;
                window.redirectWithToast(result.message || 'Sale completed successfully.', 'success', result.redirectUrl);
                return;
            }
            showError(result.message || 'Unable to save the sales transaction.');
        } catch {
            showError('Unable to save the sales transaction.');
        } finally {
            completeButton.querySelector('.spinner-border').classList.add('d-none');
            validateInstallmentCheckout();
        }
    }

    customerSearch.addEventListener('input', () => {
        window.clearTimeout(customerSearchTimer);

        if (!customerSearch.value.trim()) {
            clearCustomerInformation();
            return;
        }

        customerId.value = '';
        customerRequestId += 1;
        customerSearchTimer = window.setTimeout(searchCustomers, 200);
    });
    customerSearch.addEventListener('focus', searchCustomers);
    customerName.addEventListener('input', () => {
        customerId.value = '';
        dirty = true;
        validateInstallmentCheckout();
    });
    function validateEmailFormat() {
        const validGmail = /^[^@\s]+@gmail\.com$/i.test(customerEmail.value.trim());
        customerEmail.setCustomValidity(validGmail || !customerEmail.value ? '' : 'Enter a valid @gmail.com email address.');
    }

    function validatePhoneFormat() {
        const numericValue = customerPhone.value.replace(/\D/g, '').slice(0, 11);
        if (customerPhone.value !== numericValue) customerPhone.value = numericValue;
        customerPhone.setCustomValidity(
            numericValue.length === 11 || !numericValue ? '' : 'Phone number must contain exactly 11 numeric digits.');
    }

    customerEmail.addEventListener('input', () => {
        validateEmailFormat();
        dirty = true;
        validateInstallmentCheckout();
    });
    customerPhone.addEventListener('input', () => {
        validatePhoneFormat();
        dirty = true;
        validateInstallmentCheckout();
    });
    customerAddress.addEventListener('input', () => { dirty = true; validateInstallmentCheckout(); });
    productSearch.addEventListener('input', () => {
        window.clearTimeout(productSearchTimer);
        productSearchTimer = window.setTimeout(searchProducts, 200);
    });
    productSearch.addEventListener('focus', searchProducts);
    completeButton.addEventListener('click', saveCheckout);
    paymentMethod.addEventListener('change', () => {
        dirty = true;
        validateInstallmentCheckout();
    });
    paymentTypeInputs.forEach(input => input.addEventListener('change', () => {
        dirty = true;
        updatePaymentType();
    }));
    installmentMonths.addEventListener('change', () => {
        dirty = true;
        validateInstallmentCheckout();
    });

    document.addEventListener('click', event => {
        if (!event.target.closest('.customer-search-shell')) customerResults.classList.add('d-none');

        const link = event.target.closest('a[href]');
        if (!dirty || !link || link.target === '_blank' || link.getAttribute('href').startsWith('#')) return;
        event.preventDefault();
        pendingExitUrl = link.href;
        exitModal.show();
    });
    document.getElementById('discardCheckout').addEventListener('click', () => {
        dirty = false;
        window.location.href = pendingExitUrl || page.dataset.ordersUrl;
    });
    window.addEventListener('beforeunload', event => {
        if (!dirty) return;
        event.preventDefault();
        event.returnValue = '';
    });

    // Render server-provided products synchronously so selection state is visible
    // before the search input receives focus, then refresh inventory in the background.
    renderProductResults(visibleProducts);
    void searchProducts(false);
    updatePaymentType();
    renderItems();
})();
