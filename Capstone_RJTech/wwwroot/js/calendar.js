(() => {
    const page = document.getElementById('calendarPage');
    const grid = document.getElementById('calendarGrid');
    const modalElement = document.getElementById('eventModal');
    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const form = document.getElementById('eventForm');
    const today = new Date();
    let visibleMonth = new Date(today.getFullYear(), today.getMonth(), 1);
    let selectedDate = formatDate(today);
    let events = [];
    const allowedEventColors = new Set(['blue', 'gray', 'green', 'yellow', 'red', 'cyan']);

    function formatDate(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    function parseDate(value) {
        const [year, month, day] = value.split('-').map(Number);
        return new Date(year, month - 1, day);
    }

    function formatTime(value) {
        const [hour, minute] = value.split(':').map(Number);
        return new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit' })
            .format(new Date(2000, 0, 1, hour, minute));
    }

    function eventsForDate(date) {
        return events.filter(item => item.date === date).sort((a, b) => a.startTime.localeCompare(b.startTime));
    }

    function eventColor(item) {
        const color = String(item.color || '').toLowerCase();
        return allowedEventColors.has(color) ? color : 'blue';
    }

    function createEventChip(item) {
        const chip = document.createElement('span');
        chip.className = `calendar-event-chip event-${eventColor(item)}`;
        chip.textContent = item.title;
        return chip;
    }

    function renderCalendar() {
        const year = visibleMonth.getFullYear();
        const month = visibleMonth.getMonth();
        document.getElementById('calendarMonth').textContent = visibleMonth.toLocaleDateString(undefined, {
            month: 'long', year: 'numeric'
        });
        grid.innerHTML = '';

        const firstCell = new Date(year, month, 1 - new Date(year, month, 1).getDay());
        for (let index = 0; index < 42; index += 1) {
            const date = new Date(firstCell);
            date.setDate(firstCell.getDate() + index);
            const dateValue = formatDate(date);
            const cell = document.createElement('button');
            cell.type = 'button';
            cell.className = 'calendar-day';
            cell.dataset.date = dateValue;
            cell.classList.toggle('outside-month', date.getMonth() !== month);
            cell.classList.toggle('is-today', dateValue === formatDate(today));
            cell.classList.toggle('is-selected', dateValue === selectedDate);

            const number = document.createElement('span');
            number.className = 'calendar-day-number';
            number.textContent = date.getDate();
            cell.appendChild(number);
            eventsForDate(dateValue).slice(0, 3).forEach(item => cell.appendChild(createEventChip(item)));
            cell.addEventListener('click', () => {
                selectedDate = dateValue;
                visibleMonth = new Date(date.getFullYear(), date.getMonth(), 1);
                renderCalendar();
                renderSelectedDate();
            });
            grid.appendChild(cell);
        }
    }

    function createEventCard(item, canDelete = false) {
        const card = document.createElement('div');
        card.className = `calendar-event-card event-${eventColor(item)}`;
        const content = document.createElement('div');
        content.innerHTML = `<strong></strong><span><i class="bi bi-clock me-1"></i>${formatTime(item.startTime)} – ${formatTime(item.endTime)}</span><small></small>`;
        content.querySelector('strong').textContent = item.title;
        content.querySelector('small').textContent = item.notes || 'No notes';
        card.appendChild(content);

        if (canDelete) {
            const button = document.createElement('button');
            button.className = 'btn btn-sm btn-link text-danger';
            button.type = 'button';
            button.innerHTML = '<i class="bi bi-trash"></i>';
            button.setAttribute('aria-label', `Delete ${item.title}`);
            button.addEventListener('click', () => deleteEvent(item.id));
            card.appendChild(button);
        }
        return card;
    }

    function renderSelectedDate() {
        const date = parseDate(selectedDate);
        const selectedEvents = eventsForDate(selectedDate);
        document.getElementById('selectedDateTitle').textContent = date.toLocaleDateString(undefined, {
            weekday: 'long', month: 'long', day: 'numeric', year: 'numeric'
        });
        document.getElementById('selectedEventCount').textContent = `${selectedEvents.length} event${selectedEvents.length === 1 ? '' : 's'}`;
        const container = document.getElementById('selectedDateEvents');
        container.innerHTML = '';
        selectedEvents.forEach(item => container.appendChild(createEventCard(item, true)));
        if (!selectedEvents.length) container.innerHTML = '<div class="calendar-empty">No events scheduled.</div>';
    }

    function renderUpcoming() {
        const container = document.getElementById('upcomingEvents');
        const upcoming = events
            .filter(item => item.date >= formatDate(today))
            .sort((a, b) => `${a.date}${a.startTime}`.localeCompare(`${b.date}${b.startTime}`))
            .slice(0, 5);
        container.innerHTML = '';
        upcoming.forEach(item => {
            const row = document.createElement('button');
            row.type = 'button';
            row.className = 'upcoming-event';
            row.innerHTML = `<span class="upcoming-dot event-${eventColor(item)}"></span><span><strong></strong><small></small></span>`;
            row.querySelector('strong').textContent = item.title;
            row.querySelector('small').textContent = `${parseDate(item.date).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })} at ${formatTime(item.startTime)}`;
            row.addEventListener('click', () => {
                selectedDate = item.date;
                const eventDate = parseDate(item.date);
                visibleMonth = new Date(eventDate.getFullYear(), eventDate.getMonth(), 1);
                renderAll();
            });
            container.appendChild(row);
        });
        if (!upcoming.length) container.innerHTML = '<div class="calendar-empty">No upcoming events.</div>';
    }

    function renderAll() {
        renderCalendar();
        renderSelectedDate();
        renderUpcoming();
    }

    function openEventModal() {
        form.reset();
        document.getElementById('eventDate').value = selectedDate;
        document.getElementById('eventStart').value = '09:00';
        document.getElementById('eventEnd').value = '10:00';
        document.getElementById('eventColor').value = 'blue';
        document.getElementById('eventError').classList.add('d-none');
        modal.show();
    }

    async function loadEvents() {
        const response = await fetch(page.dataset.eventsUrl);
        const result = await response.json();
        if (!result.success) throw new Error(result.message);
        events = result.events;
        renderAll();
    }

    async function saveEvent(event) {
        event.preventDefault();
        if (!form.checkValidity()) return form.reportValidity();
        const button = document.getElementById('saveEvent');
        button.disabled = true;
        button.querySelector('.spinner-border').classList.remove('d-none');
        const payload = {
            title: document.getElementById('eventTitle').value,
            date: document.getElementById('eventDate').value,
            startTime: document.getElementById('eventStart').value,
            endTime: document.getElementById('eventEnd').value,
            color: document.getElementById('eventColor').value,
            notes: document.getElementById('eventNotes').value
        };
        try {
            const response = await fetch(page.dataset.createUrl, {
                method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload)
            });
            const result = await response.json();
            if (!result.success) {
                const error = document.getElementById('eventError');
                error.textContent = result.message;
                error.classList.remove('d-none');
                return;
            }
            modal.hide();
            selectedDate = payload.date;
            await loadEvents();
        } finally {
            button.disabled = false;
            button.querySelector('.spinner-border').classList.add('d-none');
        }
    }

    async function deleteEvent(id) {
        if (!confirm('Delete this calendar event?')) return;
        await fetch(page.dataset.deleteUrl, {
            method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body: new URLSearchParams({ id })
        });
        await loadEvents();
    }

    document.getElementById('previousMonth').addEventListener('click', () => {
        visibleMonth = new Date(visibleMonth.getFullYear(), visibleMonth.getMonth() - 1, 1);
        renderCalendar();
    });
    document.getElementById('nextMonth').addEventListener('click', () => {
        visibleMonth = new Date(visibleMonth.getFullYear(), visibleMonth.getMonth() + 1, 1);
        renderCalendar();
    });
    document.getElementById('calendarToday').addEventListener('click', () => {
        selectedDate = formatDate(today);
        visibleMonth = new Date(today.getFullYear(), today.getMonth(), 1);
        renderAll();
    });
    document.getElementById('addSelectedDateEvent').addEventListener('click', openEventModal);
    document.getElementById('addCalendarEvent').addEventListener('click', openEventModal);
    form.addEventListener('submit', saveEvent);

    loadEvents().catch(() => window.showToast('Unable to load calendar events.', 'error'));
})();
