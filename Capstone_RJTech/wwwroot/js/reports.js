(() => {
    'use strict';

    const page = document.getElementById('reportsPage');
    const dataElement = document.getElementById('reportsData');
    if (!page || !dataElement) return;

    const data = JSON.parse(dataElement.textContent);
    const charts = [];
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const liveWatchUrl = page.dataset.liveWatchUrl;
    const liveWatchController = new AbortController();
    const money = value => new Intl.NumberFormat('en-PH', {
        style: 'currency', currency: 'PHP', maximumFractionDigits: 0
    }).format(value);
    const shortNumber = value => new Intl.NumberFormat('en', {
        notation: 'compact', maximumFractionDigits: 1
    }).format(value);
    const css = name => getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    const color = (name, fallback) => css(name) || fallback;
    const wrapLabel = (value, maxLength) => {
        const words = String(value).trim().split(/\s+/).flatMap(word => {
            if (word.length <= maxLength) return [word];
            return word.match(new RegExp(`.{1,${maxLength}}`, 'g')) || [word];
        });
        const lines = [];
        let line = '';
        words.forEach(word => {
            if (!line || `${line} ${word}`.length <= maxLength) {
                line = line ? `${line} ${word}` : word;
                return;
            }
            lines.push(line);
            line = word;
        });
        if (line) lines.push(line);
        return lines;
    };

    async function watchForReportChanges() {
        if (!liveWatchUrl) return;

        let version = Number.parseInt(page.dataset.liveVersion || '0', 10);
        if (!Number.isFinite(version)) version = 0;

        while (!liveWatchController.signal.aborted) {
            try {
                const url = new URL(liveWatchUrl, window.location.origin);
                url.searchParams.set('version', String(version));
                const response = await fetch(url, {
                    cache: 'no-store',
                    headers: { Accept: 'application/json' },
                    signal: liveWatchController.signal
                });
                if (!response.ok) throw new Error(`Live report request failed with ${response.status}.`);

                const result = await response.json();
                const latestVersion = Number(result.version);
                if (result.changed === true) {
                    window.location.reload();
                    return;
                }
                if (Number.isFinite(latestVersion)) version = latestVersion;
            } catch (error) {
                if (error?.name === 'AbortError') return;
                await new Promise(resolve => window.setTimeout(resolve, 3000));
            }
        }
    }

    window.addEventListener('beforeunload', () => liveWatchController.abort(), { once: true });
    void watchForReportChanges();

    document.querySelector('[data-report-print]')?.addEventListener('click', () => window.print());
    document.querySelectorAll('.reports-segmented input').forEach(input => {
        input.addEventListener('change', () => {
            document.querySelectorAll('.reports-segmented label').forEach(label => label.classList.remove('active'));
            input.closest('label')?.classList.add('active');
        });
    });

    document.querySelectorAll('table[data-report-paginate="true"]').forEach(table => {
        const body = table.tBodies[0];
        if (!body) return;
        const pageSize = 8;
        let pageIndex = 0;
        const rows = () => [...body.rows].filter(row => row.cells.length > 1 && !row.querySelector('.reports-empty'));
        if (rows().length <= pageSize) return;

        const controls = document.createElement('nav');
        controls.className = 'reports-pagination';
        controls.setAttribute('aria-label', 'Table pagination');
        const previous = document.createElement('button');
        previous.type = 'button';
        previous.className = 'btn btn-sm reports-page-button';
        previous.innerHTML = '<i class="bi bi-chevron-left" aria-hidden="true"></i><span class="visually-hidden">Previous page</span>';
        const summary = document.createElement('span');
        const next = document.createElement('button');
        next.type = 'button';
        next.className = 'btn btn-sm reports-page-button';
        next.innerHTML = '<i class="bi bi-chevron-right" aria-hidden="true"></i><span class="visually-hidden">Next page</span>';
        controls.append(previous, summary, next);
        table.closest('.table-responsive')?.after(controls);

        function renderPage() {
            const currentRows = rows();
            const pageCount = Math.max(1, Math.ceil(currentRows.length / pageSize));
            pageIndex = Math.min(pageIndex, pageCount - 1);
            currentRows.forEach((row, index) => { row.hidden = Math.floor(index / pageSize) !== pageIndex; });
            summary.textContent = `Page ${pageIndex + 1} of ${pageCount}`;
            previous.disabled = pageIndex === 0;
            next.disabled = pageIndex === pageCount - 1;
        }

        previous.addEventListener('click', () => { pageIndex--; renderPage(); });
        next.addEventListener('click', () => { pageIndex++; renderPage(); });
        table.addEventListener('table:sorted', () => { pageIndex = 0; renderPage(); });
        renderPage();
    });

    if (typeof Chart === 'undefined') return;

    Chart.defaults.font.family = getComputedStyle(page).fontFamily;
    Chart.defaults.font.size = 10;
    Chart.defaults.color = color('--chart-text', '#7b8798');

    const baseOptions = {
        responsive: true,
        maintainAspectRatio: false,
        animation: reducedMotion ? false : { duration: 350 },
        plugins: {
            legend: { display: false },
            tooltip: { backgroundColor: '#172b49', cornerRadius: 7, padding: 10, displayColors: false }
        }
    };

    function barChart(canvas, labels, values, options = {}) {
        if (!canvas || !labels.length) return null;
        const valueLabels = {
            id: `reportValueLabels-${canvas.id}`,
            afterDatasetsDraw(chart) {
                if (!options.showValues) return;
                const { ctx, chartArea } = chart;
                ctx.save();
                ctx.fillStyle = color('--chart-label', '#596a82');
                ctx.font = `600 10px ${Chart.defaults.font.family}`;
                ctx.textBaseline = 'middle';
                chart.getDatasetMeta(0).data.forEach((bar, index) => {
                    const value = options.currency ? money(values[index]) : new Intl.NumberFormat('en').format(values[index]);
                    if (options.horizontal) {
                        ctx.textAlign = 'left';
                        ctx.fillText(value, Math.min(bar.x + 7, chartArea.right + 7), bar.y);
                    } else {
                        ctx.textAlign = 'center';
                        ctx.fillText(value, bar.x, Math.max(chartArea.top + 7, bar.y - 9));
                    }
                });
                ctx.restore();
            }
        };
        const chart = new Chart(canvas, {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    data: values,
                    backgroundColor: options.backgroundColor || '#176ef5',
                    hoverBackgroundColor: options.hoverColor || '#0b5ed7',
                    borderRadius: 4,
                    maxBarThickness: options.maxBarThickness || options.barThickness || 28,
                    categoryPercentage: .74,
                    barPercentage: .88
                }]
            },
            plugins: [valueLabels],
            options: {
                ...baseOptions,
                indexAxis: options.horizontal ? 'y' : 'x',
                layout: options.horizontal ? { padding: { left: 4, right: options.showValues ? 58 : 18 } } : { padding: { top: options.showValues ? 14 : 0 } },
                plugins: {
                    ...baseOptions.plugins,
                    tooltip: {
                        ...baseOptions.plugins.tooltip,
                        callbacks: { label: context => options.currency ? money(options.horizontal ? context.parsed.x : context.parsed.y) : shortNumber(options.horizontal ? context.parsed.x : context.parsed.y) }
                    }
                },
                scales: options.horizontal ? {
                    x: {
                        beginAtZero: true,
                        border: { display: false },
                        grid: { color: color('--chart-grid', '#eef2f6') },
                        ticks: { callback: value => options.currency ? money(value) : shortNumber(value), maxTicksLimit: 5 }
                    },
                    y: { border: { display: false }, grid: { display: false }, ticks: {
                        autoSkip: false,
                        padding: 8,
                        callback: function (value) {
                            const label = this.getLabelForValue(value);
                            const maxLength = canvas.parentElement.clientWidth < 520 ? 17 : 27;
                            return options.wrapLabels ? wrapLabel(label, maxLength) : label;
                        }
                    } }
                } : {
                    x: { border: { display: false }, grid: { display: false }, ticks: { maxRotation: 0 } },
                    y: {
                        beginAtZero: true,
                        border: { display: false },
                        grid: { color: color('--chart-grid', '#eef2f6') },
                        ticks: { callback: value => options.currency ? money(value) : shortNumber(value), maxTicksLimit: 5 }
                    }
                }
            }
        });
        charts.push(chart);
        return chart;
    }

    if (data.activeTab === 'sales') {
        barChart(
            document.getElementById('salesHistoryChart'),
            data.salesHistory.map(row => row.label),
            data.salesHistory.map(row => row.value),
            { currency: true, barThickness: 34 }
        );
        barChart(
            document.getElementById('salesCategoryChart'),
            data.salesByCategory.map(row => row.categoryName),
            data.salesByCategory.map(row => row.totalSalesAmount),
            { currency: true, horizontal: true, barThickness: 13 }
        );
    }

    if (data.activeTab === 'inventory') {
        const inventoryCanvas = document.getElementById('inventoryStatusChart');
        if (inventoryCanvas) {
            const hasInventory = data.inventory.some(value => value > 0);
            const chart = new Chart(inventoryCanvas, {
                type: 'doughnut',
                data: {
                    labels: hasInventory ? ['Available', 'Unavailable', 'Low Stock', 'Out of Stock'] : ['No products'],
                    datasets: [{
                        data: hasInventory ? data.inventory : [1],
                        backgroundColor: hasInventory ? ['#35b397', '#bec7d3', '#ffbb36', '#ff6569'] : [color('--chart-empty', '#edf1f6')],
                        borderColor: color('--chart-surface', '#fff'),
                        borderWidth: hasInventory ? 2 : 0,
                        hoverOffset: 3
                    }]
                },
                options: {
                    ...baseOptions,
                    cutout: '72%',
                    plugins: {
                        ...baseOptions.plugins,
                        tooltip: { ...baseOptions.plugins.tooltip, enabled: hasInventory }
                    }
                }
            });
            charts.push(chart);
        }
        const categoryQuantityCanvas = document.getElementById('categoryQuantityChart');
        const categoryQuantityChart = barChart(
            categoryQuantityCanvas,
            data.inventoryCategories.map(row => row.categoryName),
            data.inventoryCategories.map(row => row.totalQuantity),
            { horizontal: true, maxBarThickness: 16, showValues: true, wrapLabels: true }
        );
        const categoryQuantityFrame = categoryQuantityCanvas?.closest('.category-quantity-chart');
        if (categoryQuantityFrame && categoryQuantityChart) {
            const resizeCategoryChart = () => {
                const compact = categoryQuantityFrame.clientWidth < 520;
                const desiredHeight = Math.max(compact ? 320 : 280,
                    data.inventoryCategories.length * (compact ? 46 : 40) + 74);
                const nextHeight = `${desiredHeight}px`;
                if (categoryQuantityFrame.style.height !== nextHeight) {
                    categoryQuantityFrame.style.height = nextHeight;
                    categoryQuantityChart.resize();
                }
            };
            resizeCategoryChart();
            if ('ResizeObserver' in window) {
                new ResizeObserver(resizeCategoryChart).observe(categoryQuantityFrame);
            } else {
                window.addEventListener('resize', resizeCategoryChart);
            }
        }
    }

    if (data.activeTab === 'delivery') {
        barChart(
            document.getElementById('deliveryItemsChart'),
            data.deliveries.map(row => row.deliveryCode),
            data.deliveries.map(row => row.totalItems),
            { barThickness: 34 }
        );
        barChart(
            document.getElementById('productDeliveryChart'),
            data.productDeliveries.map(row => row.productCode),
            data.productDeliveries.map(row => row.totalQuantityDelivered),
            { barThickness: 34 }
        );
    }

    function updateTheme() {
        Chart.defaults.color = color('--chart-text', '#7b8798');
        charts.forEach(chart => {
            if (chart.config.type === 'doughnut')
                chart.data.datasets[0].borderColor = color('--chart-surface', '#fff');
            Object.values(chart.options.scales || {}).forEach(scale => {
                if (scale.ticks) scale.ticks.color = color('--chart-text', '#7b8798');
                if (scale.grid?.display !== false) scale.grid.color = color('--chart-grid', '#eef2f6');
            });
            chart.update('none');
        });
    }

    window.addEventListener('rj:theme-changed', updateTheme);
    window.addEventListener('beforeprint', updateTheme);
})();
