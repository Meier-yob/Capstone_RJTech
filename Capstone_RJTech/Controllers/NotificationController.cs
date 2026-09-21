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
        private readonly InstallmentNotificationService _installmentNotifications;

        public NotificationController(
            ApplicationDbContext db,
            StockNotificationService stockNotifications,
            InstallmentNotificationService installmentNotifications)
        {
            _db = db;
            _stockNotifications = stockNotifications;
            _installmentNotifications = installmentNotifications;
        }

        public IActionResult Index() => RedirectToAction(nameof(Notification));

        public IActionResult Notification()
        {
            _stockNotifications.Synchronize();
            _installmentNotifications.Synchronize();
            return View(_db.Notifications.AsNoTracking().OrderByDescending(item => item.created_at).ToList());
        }

        [HttpGet]
        public IActionResult GetNotifications(int limit = 6)
        {
            _stockNotifications.Synchronize();
            _installmentNotifications.Synchronize();
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
