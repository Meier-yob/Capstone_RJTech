(() => {
    const page = document.getElementById('checkoutPage');
    const customerId = document.getElementById('customerId');
    const customerName = document.getElementById('customerFullName');
    const customerEmail = document.getElementById('customerEmail');
    const customerPhone = document.getElementById('customerPhone');
    const customerAddress = document.getElementById('customerAddress');
    const customerResults = document.getElementById('customerSearchResults');
    const productSearch = document.getElementById('productSearch');
    const productResults = document.getElementById('productSearchResults');
    const productPanel = document.getElementById('selectedProductPanel');
    const quantityInput = document.getElementById('itemQuantity');
    const serialInputsContainer = document.getElementById('serialInputsContainer');
    const addItemButton = document.getElementById('addCheckoutItem');
    const checkoutItemsBody = document.getElementById('checkoutItemsBody');
    const emptyItems = document.getElementById('emptyCheckoutItems');
    const itemsFooter = document.getElementById('checkoutItemsFooter');
    const itemsPagination = document.getElementById('checkoutItemsPagination');
    const itemsRange = document.getElementById('checkoutItemsRange');
    const completeButton = document.getElementById('completeCheckout');
    const exitModal = bootstrap.Modal.getOrCreateInstance(document.getElementById('checkoutExitModal'));
    const currency = new Intl.NumberFormat('en-PH', { style: 'currency', currency: 'PHP' });

    // Products live only in this array until the final checkout request succeeds.
    let checkoutItems = JSON.parse(document.getElementById('initialCheckoutItems').textContent || '[]');
    let selectedProduct = null;
    let selectedItemsPage = 1;
    let customerSearchTimer;
    let productSearchTimer;
    let dirty = false;
    let pendingExitUrl = '';

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

    function availableStockFor(productId) {
        const itemStocks = checkoutItems
            .filter(item => item.productId === productId)
            .map(item => item.availableStock);
        return itemStocks.length ? Math.max(...itemStocks) : 0;
    }

    function totalQuantityFor(productId, ignoredIndex = -1) {
        return checkoutItems.reduce((total, item, index) =>
            index === ignoredIndex || item.productId !== productId ? total : total + item.quantity, 0);
    }

    function renderItems() {
        const pageSize = 10;
        const pageCount = Math.max(1, Math.ceil(checkoutItems.length / pageSize));
        selectedItemsPage = Math.min(selectedItemsPage, pageCount);
        const start = (selectedItemsPage - 1) * pageSize;
        const visibleItems = checkoutItems.slice(start, start + pageSize);

        checkoutItemsBody.innerHTML = visibleItems.map((item, pageIndex) => {
            const itemIndex = start + pageIndex;
            return `
                <tr data-index="${itemIndex}">
                    <td><strong>${escapeHtml(item.productName)}</strong><div class="product-meta">${escapeHtml(item.productCode || '')}</div></td>
                    <td><div class="serial-number-list">${item.serialNumbers.map(serial => `<span>${escapeHtml(serial)}</span>`).join('')}</div></td>
                    <td class="text-end">${item.quantity}</td>
                    <td class="text-end">${currency.format(item.price)}</td>
                    <td class="text-end fw-semibold">${currency.format(item.price * item.quantity)}</td>
                    <td class="text-end"><button class="btn btn-sm btn-outline-danger remove-checkout-item" type="button" aria-label="Remove ${escapeHtml(item.productName)}"><i class="bi bi-trash"></i></button></td>
                </tr>`;
        }).join('');

        checkoutItemsBody.querySelectorAll('.remove-checkout-item').forEach(button => {
            button.addEventListener('click', event => {
                const index = Number(event.currentTarget.closest('tr').dataset.index);
                checkoutItems.splice(index, 1);
                dirty = true;
                renderItems();
            });
        });

        emptyItems.classList.toggle('d-none', checkoutItems.length > 0);
        itemsFooter.classList.toggle('d-none', checkoutItems.length === 0);
        document.getElementById('selectedItemsCount').textContent = `${checkoutItems.length} item${checkoutItems.length === 1 ? '' : 's'}`;
        document.getElementById('summaryItemCount').textContent = checkoutItems.reduce((sum, item) => sum + item.quantity, 0);
        document.getElementById('checkoutTotal').textContent = currency.format(
            checkoutItems.reduce((sum, item) => sum + (item.price * item.quantity), 0));

        itemsRange.textContent = checkoutItems.length
            ? `Showing ${start + 1}–${Math.min(start + pageSize, checkoutItems.length)} of ${checkoutItems.length}`
            : 'Showing 0 items';
        itemsPagination.innerHTML = '';
        addPageButton('Previous', selectedItemsPage - 1, { disabled: selectedItemsPage === 1 });
        for (let pageNumber = 1; pageNumber <= pageCount; pageNumber += 1) {
            addPageButton(String(pageNumber), pageNumber, { active: pageNumber === selectedItemsPage });
        }
        addPageButton('Next', selectedItemsPage + 1, { disabled: selectedItemsPage === pageCount });
        updateCompleteButton();
    }

    function updateCompleteButton() {
        const hasCustomer = customerName.value.trim() &&
            customerEmail.validity.valid &&
            customerPhone.validity.valid &&
            customerAddress.value.trim();
        completeButton.disabled = !hasCustomer || checkoutItems.length === 0;
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
                customerName.value = button.dataset.name;
                customerEmail.value = button.dataset.email;
                customerPhone.value = button.dataset.phone;
                customerAddress.value = button.dataset.address;
                validateEmailFormat();
                validatePhoneFormat();
                customerResults.classList.add('d-none');
                dirty = true;
                updateCompleteButton();
            });
        });
    }

    async function searchCustomers() {
        const url = new URL(page.dataset.customerSearchUrl, window.location.origin);
        url.searchParams.set('query', customerName.value.trim());
        const response = await fetch(url);
        const result = await response.json();
        if (result.success) renderCustomerResults(result.customers);
    }

    function renderProductResults(products) {
        if (!products.length) {
            productResults.innerHTML = '<div class="live-search-empty">No available products match this search.</div>';
        } else {
            productResults.innerHTML = products.map(product => `
                <button class="live-search-option product-result" type="button" data-product='${escapeHtml(JSON.stringify(product))}'>
                    <span><strong>${escapeHtml(product.name)}</strong><small>${escapeHtml(product.code)} · ${escapeHtml(product.brand)}</small></span>
                    <span class="search-product-numbers"><small>Stock: ${product.stock}</small><strong>${currency.format(product.price)}</strong></span>
                </button>`).join('');
        }
        productResults.classList.remove('d-none');

        productResults.querySelectorAll('.product-result').forEach(button => {
            button.addEventListener('click', () => selectProduct(JSON.parse(button.dataset.product)));
        });
    }

    async function searchProducts() {
        const url = new URL(page.dataset.productSearchUrl, window.location.origin);
        url.searchParams.set('query', productSearch.value.trim());
        const response = await fetch(url);
        const result = await response.json();
        if (result.success) renderProductResults(result.products);
    }

    function selectProduct(product) {
        selectedProduct = product;
        productSearch.value = product.name;
        productResults.classList.add('d-none');
        productPanel.classList.remove('d-none');
        document.getElementById('selectedProductName').textContent = product.name;
        document.getElementById('selectedProductMeta').textContent = `${product.code} · ${product.brand} · ${product.category}`;
        document.getElementById('selectedProductStock').textContent = product.stock;
        document.getElementById('itemPrice').value = currency.format(product.price);
        quantityInput.value = 1;
        quantityInput.max = product.stock;
        renderSerialInputs(1);
        updateSelectedSubtotal();
    }

    function renderSerialInputs(quantity) {
        const existingValues = [...serialInputsContainer.querySelectorAll('.serial-number-input')]
            .map(input => input.value);

        serialInputsContainer.innerHTML = Array.from({ length: quantity }, (_, index) => `
            <div class="serial-input-field">
                <label class="form-label" for="serialNumber-${index}">Unit ${index + 1}</label>
                <input id="serialNumber-${index}" class="form-control serial-number-input" type="text"
                       maxlength="100" value="${escapeHtml(existingValues[index] || '')}"
                       placeholder="Serial number ${index + 1}" required />
                <div class="serial-duplicate-indicator" aria-live="polite"></div>
            </div>`).join('');

        updateSerialIndicators();
    }

    function normalizedSerial(input) {
        return input.value.trim().toUpperCase();
    }

    function updateSerialIndicators() {
        const inputs = [...serialInputsContainer.querySelectorAll('.serial-number-input')];
        const currentSerials = inputs.map(normalizedSerial);
        const cartSerials = new Set(
            checkoutItems.flatMap(item => item.serialNumbers).map(serial => serial.toUpperCase()));

        inputs.forEach((input, index) => {
            const serial = currentSerials[index];
            const repeatedHere = serial && currentSerials.filter(value => value === serial).length > 1;
            const repeatedInCart = serial && cartSerials.has(serial);
            const repeatedInDatabase = input.dataset.databaseDuplicate === 'true';
            const indicator = input.parentElement.querySelector('.serial-duplicate-indicator');

            input.classList.toggle('is-invalid', Boolean(repeatedHere || repeatedInCart || repeatedInDatabase));
            indicator.textContent = repeatedInDatabase
                ? 'This serial number was already used.'
                : repeatedHere || repeatedInCart
                    ? 'This serial number is already added.'
                    : serial
                        ? 'Serial number is unique.'
                        : '';
            indicator.classList.toggle('is-duplicate', Boolean(repeatedHere || repeatedInCart || repeatedInDatabase));
            indicator.classList.toggle('is-unique', Boolean(serial && !repeatedHere && !repeatedInCart && !repeatedInDatabase));
        });
    }

    async function serialExistsInDatabase(serial) {
        const url = new URL(page.dataset.serialCheckUrl, window.location.origin);
        url.searchParams.set('serialNumber', serial);
        const response = await fetch(url);
        const result = await response.json();
        return result.success && result.duplicate;
    }

    async function checkSerialInput(input) {
        const serial = normalizedSerial(input);
        let duplicate = false;
        try {
            duplicate = serial ? await serialExistsInDatabase(serial) : false;
        } catch {
            return;
        }
        if (normalizedSerial(input) !== serial) return;
        input.dataset.databaseDuplicate = duplicate ? 'true' : 'false';
        updateSerialIndicators();
    }

    function updateSelectedSubtotal() {
        const quantity = Math.max(0, Number(quantityInput.value) || 0);
        document.getElementById('itemSubtotal').value = currency.format((selectedProduct?.price || 0) * quantity);
        addItemButton.querySelector('span').textContent = quantity > 1 ? `Add ${quantity} Items` : 'Add Item';

        if (selectedProduct && Number.isInteger(quantity) && quantity > 0 && quantity <= selectedProduct.stock) {
            renderSerialInputs(quantity);
        }
    }

    async function addSelectedProduct() {
        if (!selectedProduct) return;
        const quantity = Number(quantityInput.value);
        const serialNumbers = [...serialInputsContainer.querySelectorAll('.serial-number-input')]
            .map(input => input.value.trim().toUpperCase());
        const alreadySelected = totalQuantityFor(selectedProduct.productId);

        if (!Number.isInteger(quantity) || quantity < 1) {
            showError('Quantity must be a whole number of at least one.');
            return;
        }
        if (alreadySelected + quantity > selectedProduct.stock) {
            showError(`Only ${selectedProduct.stock} unit(s) of ${selectedProduct.name} are available.`);
            return;
        }
        if (serialNumbers.length !== quantity || serialNumbers.some(serial => !serial)) {
            showError('Enter one serial number for every product unit.');
            serialInputsContainer.querySelector('.serial-number-input:invalid')?.reportValidity();
            return;
        }
        if (new Set(serialNumbers).size !== serialNumbers.length) {
            showError('Duplicate serial numbers are not allowed.');
            updateSerialIndicators();
            return;
        }

        const existingSerials = new Set(checkoutItems.flatMap(item => item.serialNumbers).map(serial => serial.toUpperCase()));
        if (serialNumbers.some(serial => existingSerials.has(serial))) {
            showError('Duplicate serial numbers are not allowed.');
            updateSerialIndicators();
            return;
        }

        let databaseDuplicates;
        addItemButton.disabled = true;
        try {
            databaseDuplicates = await Promise.all(serialNumbers.map(serialExistsInDatabase));
        } catch {
            showError('Unable to verify the serial numbers. Please try again.');
            return;
        } finally {
            addItemButton.disabled = false;
        }

        if (databaseDuplicates.some(Boolean)) {
            [...serialInputsContainer.querySelectorAll('.serial-number-input')].forEach((input, index) => {
                input.dataset.databaseDuplicate = databaseDuplicates[index] ? 'true' : 'false';
            });
            updateSerialIndicators();
            showError('A serial number was already used in another checkout.');
            return;
        }

        checkoutItems.push({
            productId: selectedProduct.productId,
            productName: selectedProduct.name,
            productCode: selectedProduct.code,
            serialNumbers,
            quantity,
            price: Number(selectedProduct.price),
            availableStock: selectedProduct.stock
        });
        dirty = true;
        selectedItemsPage = Math.ceil(checkoutItems.length / 10);
        renderItems();
        window.showToast(`${quantity} item${quantity === 1 ? '' : 's'} added to checkout.`, 'success');
        quantityInput.value = 1;
        renderSerialInputs(1);
        updateSelectedSubtotal();
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

    async function saveCheckout() {
        if (!validateCustomerFields()) return;
        if (!checkoutItems.length) {
            showError('Add at least one product to the order.');
            return;
        }

        completeButton.disabled = true;
        completeButton.querySelector('.spinner-border').classList.remove('d-none');

        const payload = {
            customerId: customerId.value ? Number(customerId.value) : null,
            customerFullName: customerName.value.trim(),
            customerEmail: customerEmail.value.trim(),
            customerPhone: customerPhone.value.trim(),
            customerAddress: customerAddress.value.trim(),
            paymentMethod: document.getElementById('paymentMethod').value,
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
            updateCompleteButton();
        }
    }

    customerName.addEventListener('input', () => {
        customerId.value = '';
        dirty = true;
        updateCompleteButton();
        window.clearTimeout(customerSearchTimer);
        customerSearchTimer = window.setTimeout(searchCustomers, 200);
    });
    customerName.addEventListener('focus', searchCustomers);
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
        updateCompleteButton();
    });
    customerPhone.addEventListener('input', () => {
        validatePhoneFormat();
        dirty = true;
        updateCompleteButton();
    });
    customerAddress.addEventListener('input', () => { dirty = true; updateCompleteButton(); });
    productSearch.addEventListener('input', () => {
        selectedProduct = null;
        productPanel.classList.add('d-none');
        window.clearTimeout(productSearchTimer);
        productSearchTimer = window.setTimeout(searchProducts, 200);
    });
    productSearch.addEventListener('focus', searchProducts);
    quantityInput.addEventListener('input', updateSelectedSubtotal);
    serialInputsContainer.addEventListener('input', event => {
        if (!event.target.classList.contains('serial-number-input')) return;
        event.target.dataset.databaseDuplicate = 'false';
        updateSerialIndicators();
        window.clearTimeout(event.target.serialCheckTimer);
        event.target.serialCheckTimer = window.setTimeout(() => checkSerialInput(event.target), 350);
    });
    addItemButton.addEventListener('click', addSelectedProduct);
    completeButton.addEventListener('click', saveCheckout);
    document.getElementById('paymentMethod').addEventListener('change', () => { dirty = true; });

    document.addEventListener('click', event => {
        if (!event.target.closest('.customer-search-shell')) customerResults.classList.add('d-none');
        if (!event.target.closest('.product-search-shell')) productResults.classList.add('d-none');

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

    renderItems();
})();
