using Capstone_RJTech.Data;
using Capstone_RJTech.Models;

namespace Capstone_RJTech.Services
{
    public class StockNotificationService
    {
        private readonly ApplicationDbContext _db;

        public StockNotificationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public void Synchronize()
        {
            var products = _db.Products.ToList();
            var notifications = _db.Notifications
                .Where(item => item.notification_type == "low-stock" || item.notification_type == "out-of-stock")
                .ToList();

            foreach (var product in products)
            {
                string? alertStatus = product.product_status switch
                {
                    "Low Stock" => "low-stock",
                    "Out of Stock" => "out-of-stock",
                    _ => null
                };

                var productAlerts = notifications
                    .Where(item => item.product_ID == product.product_ID)
                    .ToList();

                if (alertStatus == null)
                {
                    _db.Notifications.RemoveRange(productAlerts);
                    continue;
                }

                _db.Notifications.RemoveRange(
                    productAlerts.Where(item => item.notification_type != alertStatus));

                string title = alertStatus == "out-of-stock" ? "Product out of stock" : "Low stock";
                string message = alertStatus == "out-of-stock"
                    ? $"{product.product_name} has no remaining stock."
                    : $"{product.product_name}: {product.product_quantity} left.";
                var existingAlert = productAlerts.FirstOrDefault(item => item.notification_type == alertStatus);

                if (existingAlert != null)
                {
                    existingAlert.title = title;
                    existingAlert.message = message;
                    existingAlert.action_url = $"/Product/Details/{product.product_ID}";
                    continue;
                }

                _db.Notifications.Add(new AppNotification
                {
                    product_ID = product.product_ID,
                    title = title,
                    message = message,
                    notification_type = alertStatus,
                    action_url = $"/Product/Details/{product.product_ID}",
                    created_at = DateTime.Now
                });
            }

            _db.SaveChanges();
        }
    }
}
