using Capstone_RJTech.Models;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_RJTech.Controllers
{
    public class NotificationController : Controller
    {
        public IActionResult Index() => RedirectToAction(nameof(Notification));

        public IActionResult Notification()
        {
            InventoryStore.SyncStockNotifications();
            return View(InventoryStore.Notifications.OrderByDescending(item => item.created_at).ToList());
        }

        [HttpGet]
        public IActionResult GetNotifications(int limit = 6)
        {
            InventoryStore.SyncStockNotifications();
            var notifications = InventoryStore.Notifications
                .OrderByDescending(item => item.created_at)
                .Take(Math.Clamp(limit, 1, 20))
                .Select(ToNotificationResponse)
                .ToList();

            return Json(new
            {
                success = true,
                unreadCount = InventoryStore.Notifications.Count(item => !item.is_read),
                notifications
            });
        }

        [HttpPost]
        public IActionResult MarkAsRead(int id)
        {
            var notification = InventoryStore.Notifications.FirstOrDefault(item => item.notification_ID == id);
            if (notification == null)
                return Json(new { success = false, message = "Notification not found." });

            notification.is_read = true;
            return Json(new { success = true, message = "Notification marked as read." });
        }

        [HttpPost]
        public IActionResult MarkAllAsRead()
        {
            InventoryStore.Notifications.ForEach(item => item.is_read = true);
            return Json(new { success = true, message = "All notifications marked as read." });
        }

        public IActionResult Calendar()
        {
            return View(InventoryStore.ScheduleEvents.OrderBy(item => item.event_date).ThenBy(item => item.start_time).ToList());
        }

        [HttpGet]
        public IActionResult GetEvents()
        {
            var events = InventoryStore.ScheduleEvents.Select(item => new
            {
                id = item.event_ID,
                item.title,
                date = item.event_date.ToString("yyyy-MM-dd"),
                startTime = item.start_time.ToString(@"hh\:mm"),
                endTime = item.end_time.ToString(@"hh\:mm"),
                item.notes,
                item.color
            });
            return Json(new { success = true, events });
        }

        [HttpPost]
        public IActionResult CreateEvent([FromBody] ScheduleEventRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.title) ||
                !DateTime.TryParse(request.date, out DateTime eventDate) ||
                !TimeSpan.TryParse(request.startTime, out TimeSpan startTime) ||
                !TimeSpan.TryParse(request.endTime, out TimeSpan endTime))
            {
                return Json(new { success = false, message = "Complete all required event details." });
            }

            if (endTime <= startTime)
                return Json(new { success = false, message = "End time must be later than start time." });

            var calendarEvent = new ScheduleEvent
            {
                event_ID = InventoryStore.ScheduleEvents.Any() ? InventoryStore.ScheduleEvents.Max(item => item.event_ID) + 1 : 1,
                title = request.title.Trim(),
                event_date = eventDate.Date,
                start_time = startTime,
                end_time = endTime,
                notes = request.notes?.Trim() ?? string.Empty,
                color = NormalizeEventColor(request.color)
            };
            InventoryStore.ScheduleEvents.Add(calendarEvent);
            InventoryStore.Notifications.Insert(0, new AppNotification
            {
                notification_ID = InventoryStore.Notifications.Any() ? InventoryStore.Notifications.Max(item => item.notification_ID) + 1 : 1,
                title = "Calendar event scheduled",
                message = $"{calendarEvent.title} is scheduled for {calendarEvent.event_date:MMM d, yyyy}.",
                notification_type = "calendar",
                action_url = "/Notification/Calendar",
                created_at = DateTime.Now
            });

            return Json(new { success = true, message = "Event added successfully." });
        }

        [HttpPost]
        public IActionResult DeleteEvent(int id)
        {
            var calendarEvent = InventoryStore.ScheduleEvents.FirstOrDefault(item => item.event_ID == id);
            if (calendarEvent == null)
                return Json(new { success = false, message = "Calendar event not found." });

            InventoryStore.ScheduleEvents.Remove(calendarEvent);
            return Json(new { success = true, message = "Event deleted successfully." });
        }

        public class ScheduleEventRequest
        {
            public string? title { get; set; }
            public string? date { get; set; }
            public string? startTime { get; set; }
            public string? endTime { get; set; }
            public string? notes { get; set; }
            public string? color { get; set; }
        }

        private static string NormalizeEventColor(string? color)
        {
            string normalizedColor = color?.Trim().ToLowerInvariant() ?? "blue";
            string[] allowedColors = { "blue", "gray", "green", "yellow", "red", "cyan" };

            return allowedColors.Contains(normalizedColor) ? normalizedColor : "blue";
        }

        private static object ToNotificationResponse(AppNotification item) => new
        {
            id = item.notification_ID,
            item.title,
            item.message,
            type = item.notification_type,
            url = item.action_url,
            createdAt = item.created_at,
            isRead = item.is_read
        };
    }
}
