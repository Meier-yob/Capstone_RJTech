const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const test = require('node:test');

const root = path.resolve(__dirname, '../../Capstone_RJTech');
const readScript = name => fs.readFileSync(path.join(root, 'wwwroot/js', name), 'utf8');
const excelType = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
const row = (id, data = {}) => Object.assign(element(), { dataset: { recordId: String(id), ...data } });

function element() {
    const attributes = new Map();
    const classes = new Set();
    return {
        dataset: {}, style: {}, value: '', innerHTML: 'Export Excel', disabled: false,
        listeners: new Map(), children: [],
        addEventListener(name, callback) { this.listeners.set(name, callback); },
        trigger(name) { return this.listeners.get(name)?.({ currentTarget: this }); },
        appendChild(child) { this.children.push(child); },
        setAttribute(name, value) { attributes.set(name, value); },
        getAttribute(name) { return attributes.get(name) ?? null; },
        removeAttribute(name) { attributes.delete(name); },
        classList: {
            add(name) { classes.add(name); }, remove(name) { classes.delete(name); },
            contains(name) { return classes.has(name); },
            toggle(name, enabled) { enabled ? classes.add(name) : classes.delete(name); }
        },
        click() { this.clicked = true; }, remove() { this.removed = true; }
    };
}

function downloadHarness(options = {}) {
    const errors = [], requests = [], links = [], timers = [], revoked = [];
    const button = element();
    button.dataset.exportUrl = '/ExcelExport/Inventory';
    button.closest = () => ({ querySelector: () => ({ value: options.token ?? 'test-token' }) });
    const context = {
        window: { showToast: message => errors.push(message) },
        document: { body: { appendChild: link => links.push(link) }, createElement: element },
        URL: { createObjectURL: () => 'blob:test', revokeObjectURL: url => revoked.push(url) },
        setTimeout: callback => timers.push(callback),
        fetch: async (url, request) => {
            requests.push({ url, ...request, body: JSON.parse(request.body) });
            if (options.fetch) return options.fetch();
            return new Response('workbook', {
                headers: { 'Content-Type': excelType, 'Content-Disposition': options.disposition ?? 'attachment; filename="RJTech_Inventory_2026-09-06.xlsx"' }
            });
        }
    };
    vm.runInNewContext(readScript('excel-export.js'), context);
    return { button, errors, requests, links, timers, revoked, download: context.window.rjtechExcelExport.download };
}

test('unfiltered export asks the server for all records and downloads its named workbook', async () => {
    const h = downloadHarness();
    await h.download(h.button, [row(1), row(2)], false);
    assert.deepEqual(h.requests[0].body, { recordIds: null });
    assert.equal(h.requests[0].method, 'POST');
    assert.equal(h.requests[0].headers.RequestVerificationToken, 'test-token');
    assert.equal(h.links[0].download, 'RJTech_Inventory_2026-09-06.xlsx');
    assert.equal(h.links[0].clicked, true);
    assert.equal(h.links[0].removed, true);
    assert.equal(h.button.disabled, false);
    assert.equal(h.button.innerHTML, 'Export Excel');
    assert.equal(h.button.getAttribute('aria-busy'), null);
    assert.equal(h.revoked.length, 0);
    h.timers.forEach(callback => callback());
    assert.deepEqual(h.revoked, ['blob:test']);
});

test('filtered exports send numeric distinct IDs, including rows hidden by pagination', async () => {
    const h = downloadHarness({ disposition: "attachment; filename*=UTF-8''RJTech_Customers_%E2%82%B1.xlsx" });
    const hidden = row(2);
    hidden.classList.add('d-none');
    await h.download(h.button, [row(1), hidden, row(1)], true);
    assert.deepEqual(h.requests[0].body, { recordIds: [1, 2] });
    assert.equal(h.links[0].download, 'RJTech_Customers_₱.xlsx');
});

test('empty matches show an explanation and make no request', async () => {
    const h = downloadHarness();
    await h.download(h.button, [], true);
    assert.deepEqual(h.errors, ['No records available to export.']);
    assert.equal(h.requests.length, 0);
});

test('server errors, sign-in HTML, empty files, and network failures never download a false workbook', async t => {
    for (const [name, fetch, expected] of [
        ['empty database', () => Response.json({ message: 'No records available to export.' }, { status: 404 }), /No records/],
        ['server failure', () => Response.json({ message: 'Please try again later.' }, { status: 500 }), /Please try again/],
        ['sign-in redirect', () => new Response('<html>Sign in</html>', { headers: { 'Content-Type': 'text/html' } }), /Unable to export/],
        ['empty workbook', () => new Response('', { headers: { 'Content-Type': excelType } }), /No records/],
        ['network failure', () => { throw new Error('offline'); }, /Check your connection/]
    ]) {
        await t.test(name, async () => {
            const h = downloadHarness({ fetch });
            await h.download(h.button, [row(1)], false);
            assert.match(h.errors[0], expected);
            assert.equal(h.links.length, 0);
            assert.equal(h.button.disabled, false);
            assert.equal(h.button.innerHTML, 'Export Excel');
        });
    }
});

test('invalid filtered IDs and missing antiforgery tokens fail before requesting the server', async () => {
    const invalid = downloadHarness();
    await invalid.download(invalid.button, [row('invalid')], true);
    assert.equal(invalid.requests.length, 0);
    assert.match(invalid.errors[0], /Refresh this page/);
    const missingToken = downloadHarness({ token: '' });
    await missingToken.download(missingToken.button, [row(1)], false);
    assert.equal(missingToken.requests.length, 0);
});

test('an in-progress download blocks duplicate clicks and restores the button afterwards', async () => {
    let complete;
    const h = downloadHarness({ fetch: () => new Promise(resolve => { complete = resolve; }) });
    const download = h.download(h.button, [row(1)], false);
    assert.equal(h.button.disabled, true);
    await h.download(h.button, [row(1)], false);
    assert.equal(h.requests.length, 1);
    complete(new Response('workbook', { headers: { 'Content-Type': excelType } }));
    await download;
    assert.equal(h.button.disabled, false);
});

const modules = [
    { script: 'product-management.js', view: 'Product/ProductManagement.cshtml', action: 'Inventory', button: 'exportProducts', rows: '.product-row', search: 'productSearch', size: 'productPageSize', tabs: '.product-status-tabs [data-status]' },
    { script: 'delivery-management.js', view: 'Delivery/DeliveryManagement.cshtml', action: 'Delivery', button: 'exportDeliveries', rows: '.delivery-row', search: 'deliverySearch', size: 'deliveryPageSize' },
    { script: 'customer-management.js', view: 'Sales/Customer.cshtml', action: 'Customers', button: 'exportCustomers', rows: '.customer-row', search: 'customerSearch', size: 'customerPageSize' },
    { script: 'sales-orders.js', view: 'Sales/SalesOrders.cshtml', action: 'SalesSummary', button: 'exportSalesOrders', rows: '.sales-order-row', search: 'salesOrderSearch', size: 'salesOrderPageSize', tabs: '.sales-status-tabs [data-status]' },
    { script: 'installments.js', view: 'Installment/Index.cshtml', action: 'Installments', button: 'exportInstallments', rows: '.installment-row', search: 'installmentSearch', size: 'installmentPageSize', tabs: '.installment-status-tabs [data-status]' }
];

for (const module of modules) {
    test(`${module.action} integrates export with all existing filters before pagination`, () => {
        const elements = new Map(), calls = [];
        const get = id => {
            if (!elements.has(id)) elements.set(id, element());
            return elements.get(id);
        };
        const rows = [
            row(1, { search: 'matching first', status: 'active', category: 'monitors', progress: 'in-progress' }),
            row(2, { search: 'matching second', status: 'active', category: 'monitors', progress: 'in-progress' }),
            row(3, { search: 'other third', status: 'completed', category: 'cables', progress: 'fully-paid' })
        ];
        const activeTab = Object.assign(element(), { dataset: { status: 'active' } });
        const category = Object.assign(element(), { value: 'monitors', checked: false });
        get(module.size).value = '1';
        get('installmentProgressFilter').value = 'all';
        const document = {
            getElementById: get, createElement: element, addEventListener() {},
            querySelectorAll: selector => selector === module.rows ? rows
                : selector === module.tabs ? [activeTab]
                    : selector === '.category-checkbox' ? [category] : []
        };
        vm.runInNewContext(readScript(module.script), {
            document, window: { rjtechExcelExport: { download: (button, records, filtered) => calls.push({ button, ids: Array.from(records, row => Number(row.dataset.recordId)), filtered }) } }
        });
        assert.equal(rows[1].classList.contains('d-none'), true, 'second record starts off-page');
        get(module.button).trigger('click');
        assert.deepEqual(calls.at(-1).ids, [1, 2, 3]);
        assert.equal(calls.at(-1).filtered, false);

        get(module.search).value = 'matching';
        get(module.search).trigger('input');
        get(module.button).trigger('click');
        assert.deepEqual(calls.at(-1).ids, [1, 2]);
        assert.equal(calls.at(-1).filtered, true);

        get(module.search).value = '';
        if (module.tabs) {
            activeTab.trigger('click');
            get(module.button).trigger('click');
            assert.deepEqual(calls.at(-1).ids, [1, 2]);
            assert.equal(calls.at(-1).filtered, true);
        }
        if (module.action === 'Inventory') {
            activeTab.dataset.status = 'all';
            activeTab.trigger('click');
            category.checked = true;
            category.trigger('change');
            get(module.button).trigger('click');
            assert.deepEqual(calls.at(-1).ids, [1, 2]);
            assert.equal(calls.at(-1).filtered, true);
        }
        if (module.action === 'Installments') {
            activeTab.dataset.status = 'all';
            activeTab.trigger('click');
            get('installmentProgressFilter').value = 'fully-paid';
            get('installmentProgressFilter').trigger('change');
            get(module.button).trigger('click');
            assert.deepEqual(calls.at(-1).ids, [3]);
            assert.equal(calls.at(-1).filtered, true);
        }

        get(module.search).value = 'no matches';
        get(module.button).trigger('click');
        assert.deepEqual(calls.at(-1).ids, []);

        const view = fs.readFileSync(path.join(root, 'Views', module.view), 'utf8');
        assert.ok(view.includes(`@Url.Action("${module.action}", "ExcelExport")`));
        assert.ok(view.includes('@Html.AntiForgeryToken()'));
        assert.ok(view.indexOf('~/js/excel-export.js') < view.indexOf(`~/js/${module.script}`));
    });
}
