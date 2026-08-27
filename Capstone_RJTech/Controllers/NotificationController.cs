using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Capstone_RJTech.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone_RJTech.Controllers
{
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly StockNotificationService _stockNotifications;

        public NotificationController(ApplicationDbContext db, StockNotificationService stockNotifications)
        {
            _db = db;
            _stockNotifications = stockNotifications;
        }

        public IActionResult Index() => RedirectToAction(nameof(Notification));

        public IActionResult Notification()
        {
            _stockNotifications.Synchronize();
            return View(_db.Notifications.AsNoTracking().OrderByDescending(item => item.created_at).ToList());
        }

        [HttpGet]
        public IActionResult GetNotifications(int limit = 6)
        {
            _stockNotifications.Synchronize();
            var notifications = _db.Notifications
                .AsNoTracking()
                .OrderByDescending(item => item.created_at)
                .Take(Math.Clamp(limit, 1, 20))
                .ToList()
                .Select(ToNotificationResponse)
                .ToList();

            return Json(new
            {
                success = true,
                unreadCount = _db.Notifications.Count(item => !item.is_read),
                notifications
            });
        }

        [HttpPost]
        public IActionResult MarkAsRead(int id)
        {
            var notification = _db.Notifications.Find(id);
            if (notification == null)
                return Json(new { success = false, message = "Notification not found." });

            notification.is_read = true;
            _db.SaveChanges();
            return Json(new { success = true, message = "Notification marked as read." });
        }

        [HttpPost]
        public IActionResult MarkAllAsRead()
        {
            var unreadNotifications = _db.Notifications.Where(item => !item.is_read).ToList();
            unreadNotifications.ForEach(item => item.is_read = true);
            _db.SaveChanges();
            return Json(new { success = true, message = "All notifications marked as read." });
        }

        public IActionResult Calendar()
        {
            return View(_db.ScheduleEvents.AsNoTracking().OrderBy(item => item.event_date).ThenBy(item => item.start_time).ToList());
        }

        [HttpGet]
        public IActionResult GetEvents()
        {
            var events = _db.ScheduleEvents.AsNoTracking().ToList().Select(item => new
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
                title = request.title.Trim(),
                event_date = eventDate.Date,
                start_time = startTime,
                end_time = endTime,
                notes = request.notes?.Trim() ?? string.Empty,
                color = NormalizeEventColor(request.color)
            };
            _db.ScheduleEvents.Add(calendarEvent);
            _db.Notifications.Add(new AppNotification
            {
                title = "Calendar event scheduled",
                message = $"{calendarEvent.title} is scheduled for {calendarEvent.event_date:MMM d, yyyy}.",
                notification_type = "calendar",
                action_url = "/Notification/Calendar",
                created_at = DateTime.Now
            });
            _db.SaveChanges();

            return Json(new { success = true, message = "Event added successfully." });
        }

        [HttpPost]
        public IActionResult DeleteEvent(int id)
        {
            var calendarEvent = _db.ScheduleEvents.Find(id);
            if (calendarEvent == null)
                return Json(new { success = false, message = "Calendar event not found." });

            _db.ScheduleEvents.Remove(calendarEvent);
            _db.SaveChanges();
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
