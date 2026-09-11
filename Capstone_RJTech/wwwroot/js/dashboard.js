(() => {
    'use strict';
    const page = document.getElementById('dashboardPage');
    if (!page) return;
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    const motionTargets = [
        page.querySelector('.dashboard-heading'),
        ...page.querySelectorAll('.dashboard-summary .metric-card'),
        ...page.querySelectorAll('.dashboard-section.dashboard-panel, .dashboard-section .dashboard-panel')
    ].filter(Boolean);
    motionTargets.forEach((target, index) => {
        target.classList.add('dashboard-motion-item');
        target.style.setProperty('--dashboard-motion-order', index);
        target.dataset.dashboardMotionOrder = String(index);
    });
    if (!reducedMotion) {
        window.setTimeout(() => motionTargets.forEach(target => { target.style.willChange = 'auto'; }), 1600);
    }

    const data = JSON.parse(document.getElementById('dashboardData').textContent);
    const money = value => new Intl.NumberFormat('en-PH', {
        style: 'currency', currency: 'PHP', minimumFractionDigits: 2
    }).format(value);
    const shortMoney = value => '₱' + new Intl.NumberFormat('en', {
        notation: 'compact', maximumFractionDigits: 1
    }).format(value);

    const dashboardCountValues = [...page.querySelectorAll('[data-dashboard-count]')];
    const dashboardInteger = new Intl.NumberFormat('en-PH', { maximumFractionDigits: 0 });

    function formatDashboardCount(element, value) {
        return element.dataset.dashboardFormat === 'currency'
            ? money(value)
            : dashboardInteger.format(Math.round(value));
    }

    function finishDashboardCounts() {
        dashboardCountValues.forEach(element => {
            const target = Number(element.dataset.dashboardCount);
            if (Number.isFinite(target)) element.textContent = formatDashboardCount(element, target);
        });
    }

    function animateDashboardCounts() {
        if (reducedMotion) return;

        dashboardCountValues.forEach(element => {
            const target = Number(element.dataset.dashboardCount);
            if (Number.isFinite(target)) element.textContent = formatDashboardCount(element, 0);
        });

        const startedAt = performance.now();
        const duration = 850;

        function renderFrame(now) {
            let pending = false;
            dashboardCountValues.forEach(element => {
                const target = Number(element.dataset.dashboardCount);
                if (!Number.isFinite(target)) return;

                const motionOrder = Number(element.closest('.dashboard-motion-item')?.dataset.dashboardMotionOrder || 0);
                const delay = (motionOrder * 75) + 120;
                const elapsed = now - startedAt - delay;
                const progress = Math.min(1, Math.max(0, elapsed / duration));
                const eased = 1 - Math.pow(1 - progress, 3);
                element.textContent = formatDashboardCount(element, target * eased);
                pending ||= progress < 1;
            });

            if (pending) window.requestAnimationFrame(renderFrame);
        }

        window.requestAnimationFrame(renderFrame);
    }

    animateDashboardCounts();
    window.addEventListener('beforeprint', finishDashboardCounts);
    const monthName = new Intl.DateTimeFormat('en', { month: 'short' })
        .format(new Date(data.year, data.month - 1, 1));
    const form = document.getElementById('dashboardFilters');
    ['dashboardMonth', 'dashboardYear'].forEach(id => {
        document.getElementById(id).addEventListener('change', () => form.requestSubmit());
    });

    const categorySelect = document.getElementById('dashboardCategory');
    if (typeof Chart === 'undefined') {
        const error = document.getElementById('categoryError');
        error.textContent = 'Charts could not load. Use the data tables below.';
        error.hidden = false;
        categorySelect.addEventListener('change', () => form.requestSubmit());
        document.querySelectorAll('.dashboard-chart-data').forEach(details => { details.open = true; });
        document.querySelectorAll('[data-sales-interval]').forEach(button => { button.disabled = true; });
        return;
    }

    Chart.defaults.font.family = getComputedStyle(page).fontFamily;
    Chart.defaults.font.size = 10;
    const chartColor = name => getComputedStyle(document.documentElement).getPropertyValue(`--chart-${name}`).trim();
    Chart.defaults.color = chartColor('text');
    const animation = reducedMotion ? false : {
        duration: 850,
        easing: 'easeOutQuart',
        delay: 420
    };
    const tooltip = { backgroundColor: '#172b49', padding: 10, cornerRadius: 7, displayColors: false };
    const baseOptions = { responsive: true, maintainAspectRatio: false, animation };
    function areaFill(context) {
        const { ctx, chartArea } = context.chart;
        if (!chartArea) return 'rgba(13, 110, 253, .08)';
        const gradient = ctx.createLinearGradient(0, chartArea.top, 0, chartArea.bottom);
        gradient.addColorStop(0, 'rgba(13, 110, 253, .22)');
        gradient.addColorStop(1, 'rgba(13, 110, 253, .01)');
        return gradient;
    }
    const salesCanvas = document.getElementById('salesOverviewChart');
    const salesChart = salesCanvas ? new Chart(salesCanvas, {
        type: 'line',
        data: {
            labels: data.salesOverview.map(day => `${monthName} ${day.day}`),
            datasets: [{ label: 'Paid sales', data: data.salesOverview.map(day => day.revenue),
                borderColor: chartColor('line'), borderWidth: 2, backgroundColor: areaFill, fill: true,
                tension: .35, cubicInterpolationMode: 'monotone', pointRadius: 0, pointHoverRadius: 4, pointBackgroundColor: chartColor('line') }]
        },
        options: {
            ...baseOptions, interaction: { intersect: false, mode: 'index' },
            plugins: { legend: { display: false }, tooltip: { ...tooltip, callbacks: { label: context => money(context.parsed.y) } } },
            scales: {
                x: { grid: { display: false }, border: { display: false }, ticks: { maxTicksLimit: 7, maxRotation: 0, padding: 8 } },
                y: { beginAtZero: true, grid: { color: chartColor('grid'), drawTicks: false }, border: { display: false },
                    ticks: { maxTicksLimit: 5, padding: 8, callback: shortMoney } }
            }
        }
    }) : null;

    document.querySelectorAll('[data-sales-interval]').forEach(button => {
        button.disabled = !salesChart;
        button.addEventListener('click', () => {
            const weekly = button.dataset.salesInterval === 'weekly';
            const points = weekly
                ? data.weeklySalesOverview.map(week => ({ label: week.label, value: week.revenue }))
                : data.salesOverview.map(day => ({ label: `${monthName} ${day.day}`, value: day.revenue }));
            salesChart.data.labels = points.map(point => point.label);
            salesChart.data.datasets[0].data = points.map(point => point.value);
            salesChart.update();
            salesCanvas.setAttribute('aria-label', `${weekly ? 'Weekly' : 'Daily'} paid sales for ${data.periodLabel}. Total ${money(data.periodSales)}.`);
            document.querySelectorAll('[data-sales-interval]').forEach(control => {
                control.classList.toggle('active', control === button);
                control.setAttribute('aria-pressed', String(control === button));
            });
        });
    });

    const inventoryChart = new Chart(document.getElementById('inventoryHealthChart'), {
        type: 'doughnut',
        data: {
            labels: data.totalProducts ? ['Available', 'Unavailable', 'Low Stock', 'Out of Stock'] : ['No products'],
            datasets: [{ data: data.totalProducts ? [data.inventoryHealth.available, data.inventoryHealth.unavailable,
                data.inventoryHealth.lowStock, data.inventoryHealth.outOfStock] : [1],
                backgroundColor: data.totalProducts ? ['#35b397', '#bec7d3', '#ffbb36', '#ff6569'] : [chartColor('empty')],
                borderWidth: data.totalProducts ? 2 : 0, borderColor: chartColor('surface'), hoverOffset: 3 }]
        },
        options: { ...baseOptions, cutout: '73%', plugins: { legend: { display: false },
            tooltip: { ...tooltip, enabled: data.totalProducts > 0, callbacks: {
                label: context => `${context.label}: ${context.raw} products`
            } } } }
    });

    let categoryChart;
    function renderCategories(categories) {
        const frame = document.getElementById('categoryChartFrame');
        frame.hidden = !categories.length;
        document.getElementById('categoryEmpty').hidden = categories.length > 0;
        frame.style.height = `${Math.max(150, categories.length * 25 + 24)}px`;
        if (categoryChart) categoryChart.destroy();
        categoryChart = null;
        if (!categories.length) return;
        categoryChart = new Chart(document.getElementById('categorySalesChart'), {
            type: 'bar',
            data: { labels: categories.map(category => category.category), datasets: [{
                data: categories.map(category => category.revenue), backgroundColor: '#2179ff',
                hoverBackgroundColor: '#0b5ed7', borderRadius: 4, barThickness: 7
            }] },
            plugins: [{ id: 'categoryAmounts', afterDatasetsDraw(chart) {
                const { ctx } = chart;
                ctx.save();
                ctx.font = `10px ${Chart.defaults.font.family}`;
                ctx.fillStyle = chartColor('label');
                ctx.textAlign = 'right';
                ctx.textBaseline = 'middle';
                chart.getDatasetMeta(0).data.forEach((bar, index) => {
                    ctx.fillText(shortMoney(categories[index].revenue), chart.width - 1, bar.y);
                });
                ctx.restore();
            } }],
            options: { ...baseOptions, indexAxis: 'y', layout: { padding: { right: 58 } },
                plugins: { legend: { display: false }, tooltip: { ...tooltip, callbacks: { label: context => money(context.parsed.x) } } },
                scales: {
                    x: { beginAtZero: true, grid: { display: false }, border: { display: false },
                        ticks: { maxTicksLimit: 3, maxRotation: 0, callback: shortMoney, font: { size: 9 } } },
                    y: { grid: { display: false }, border: { display: false }, ticks: { font: { size: 10 }, callback: function(value) {
                        const label = this.getLabelForValue(value);
                        return label.length > 17 ? label.slice(0, 16) + '…' : label;
                    } } }
                }
            }
        });
    }
    renderCategories(data.salesByCategory);

    if (!reducedMotion) {
        window.setTimeout(() => {
            baseOptions.animation = { duration: 450, easing: 'easeOutQuart', delay: 0 };
            [salesChart, inventoryChart, categoryChart].filter(Boolean).forEach(chart => {
                chart.options.animation = { duration: 450, easing: 'easeOutQuart', delay: 0 };
            });
        }, 1800);
    }

    function updateChartTheme() {
        Chart.defaults.color = chartColor('text');
        if (salesChart) {
            salesChart.data.datasets[0].borderColor = chartColor('line');
            salesChart.data.datasets[0].pointBackgroundColor = chartColor('line');
            salesChart.options.scales.y.grid.color = chartColor('grid');
        }
        inventoryChart.data.datasets[0].borderColor = chartColor('surface');
        if (!data.totalProducts) inventoryChart.data.datasets[0].backgroundColor = [chartColor('empty')];
        [salesChart, inventoryChart, categoryChart].filter(Boolean).forEach(chart => {
            chart.options.color = chartColor('text');
            Object.values(chart.options.scales || {}).forEach(scale => {
                if (scale.ticks) scale.ticks.color = chartColor('text');
            });
            chart.update('none');
        });
    }
    window.addEventListener('rj:theme-changed', updateChartTheme);
    window.addEventListener('beforeprint', updateChartTheme);
    window.addEventListener('afterprint', updateChartTheme);

    let pendingRequest;
    let selectedCategory = categorySelect.value;
    categorySelect.addEventListener('change', async () => {
        pendingRequest?.abort();
        const request = new AbortController();
        pendingRequest = request;
        const requestedCategory = categorySelect.value;
        const panel = document.getElementById('categoryPanel');
        const error = document.getElementById('categoryError');
        error.hidden = true;
        panel.setAttribute('aria-busy', 'true');
        try {
            const url = new URL(page.dataset.categoryUrl, window.location.origin);
            url.search = new URLSearchParams({ month: data.month, year: data.year, categoryId: requestedCategory });
            const response = await fetch(url, { signal: request.signal, headers: { Accept: 'application/json' } });
            if (!response.ok) throw new Error('Category request failed');
            const categories = await response.json();
            if (request.signal.aborted) return;
            data.salesByCategory = categories;
            data.categoryId = requestedCategory ? Number(requestedCategory) : null;
            selectedCategory = requestedCategory;
            renderCategories(categories);
            const body = document.getElementById('categoryDataBody');
            body.replaceChildren();
            categories.forEach(category => {
                const row = body.insertRow();
                row.insertCell().textContent = category.category;
                const value = row.insertCell();
                value.className = 'text-end';
                value.textContent = money(category.revenue);
            });
            const address = new URL(window.location.href);
            if (requestedCategory) address.searchParams.set('categoryId', requestedCategory);
            else address.searchParams.delete('categoryId');
            history.replaceState(null, '', address);
        } catch (failure) {
            if (failure.name === 'AbortError') return;
            categorySelect.value = selectedCategory;
            error.textContent = 'Unable to update category sales. Please try again.';
            error.hidden = false;
        } finally {
            if (pendingRequest === request) {
                panel.setAttribute('aria-busy', 'false');
            }
        }
    });
})();
