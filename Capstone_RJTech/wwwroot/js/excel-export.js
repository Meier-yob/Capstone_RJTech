(() => {
    const excelContentType = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';

    function showError(message) {
        if (typeof window.showToast === 'function') window.showToast(message, 'error');
        else window.alert(message);
    }

    function downloadFileName(disposition) {
        const encoded = disposition?.match(/filename\*\s*=\s*UTF-8''([^;]+)/i);
        const plain = disposition?.match(/filename\s*=\s*(?:"([^"]+)"|([^;]+))/i);
        let name = plain?.[1] || plain?.[2] || '';
        if (encoded) {
            try { name = decodeURIComponent(encoded[1].trim()); } catch { /* Use the plain filename. */ }
        }
        name = name.trim().replace(/[\\/:*?"<>|\u0000-\u001f]/g, '_');
        return name.toLowerCase().endsWith('.xlsx') ? name : 'RJTech_Export.xlsx';
    }

    async function download(button, matchingRows, hasActiveFilters) {
        if (!button || button.disabled) return;
        if (!matchingRows.length) {
            showError('No records available to export.');
            return;
        }

        const recordIds = hasActiveFilters
            ? [...new Set(matchingRows.map(row => Number(row.dataset.recordId ?? row.dataset.id)))]
            : null;
        if (recordIds?.some(id => !Number.isSafeInteger(id) || id <= 0)) {
            showError('Unable to export these records. Refresh this page and try again.');
            return;
        }

        const token = button.closest('.app-page')
            ?.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!button.dataset.exportUrl || !token) {
            showError('Unable to start the export. Refresh this page and try again.');
            return;
        }

        const originalContent = button.innerHTML;
        const originalBusy = button.getAttribute('aria-busy');
        button.disabled = true;
        button.setAttribute('aria-busy', 'true');
        button.innerHTML = '<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Exporting…';

        try {
            const response = await fetch(button.dataset.exportUrl, {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': `${excelContentType}, application/json`,
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ recordIds })
            });
            const contentType = response.headers.get('Content-Type') || '';
            if (!response.ok || !contentType.toLowerCase().includes(excelContentType)) {
                const result = contentType.toLowerCase().includes('json')
                    ? await response.json().catch(() => null)
                    : null;
                showError(result?.message || 'Unable to export the Excel file. Refresh this page and try again.');
                return;
            }

            const blob = await response.blob();
            if (!blob.size) {
                showError('No records available to export.');
                return;
            }

            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = downloadFileName(response.headers.get('Content-Disposition'));
            link.hidden = true;
            document.body.appendChild(link);
            try { link.click(); }
            finally {
                link.remove();
                // Give the browser time to start reading the download before releasing it.
                setTimeout(() => URL.revokeObjectURL(url), 60000);
            }
        } catch {
            showError('Unable to download the Excel file. Check your connection and try again.');
        } finally {
            button.innerHTML = originalContent;
            button.disabled = false;
            if (originalBusy === null) button.removeAttribute('aria-busy');
            else button.setAttribute('aria-busy', originalBusy);
        }
    }

    window.rjtechExcelExport = { download };
})();
