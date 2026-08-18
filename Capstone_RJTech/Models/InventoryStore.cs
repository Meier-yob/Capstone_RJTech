namespace Capstone_RJTech.Models
{
    /// <summary>
    /// Shared in-memory state for the prototype controllers. Keeping the data in
    /// one store prevents ProductController and DeliveryController from creating
    /// separate inventories while the project is not yet connected to a database.
    /// </summary>
    internal static class InventoryStore
    {
        internal static readonly List<ProductCategory> Categories = new()
        {
            new ProductCategory { category_ID = 1, category_name = "Monitors" },
            new ProductCategory { category_ID = 2, category_name = "Mouses" },
            new ProductCategory { category_ID = 3, category_name = "Keyboards" },
            new ProductCategory { category_ID = 4, category_name = "Headsets" }
        };

        internal static readonly List<Product> Products = new()
        {
            new Product { product_ID = 1, product_name = "Optical Wired Mouse", product_brand = "A4 Tech", product_description = "Optical Wired Mouse", product_quantity = 0, reorder_level = 5, Product_price = 200.00M, product_status = "Unavailable", category_ID = 2 },
            new Product { product_ID = 2, product_name = "Mechanical Keyboard", product_brand = "Logitech", product_description = "Mechanical Keyboard", product_quantity = 0, reorder_level = 5, Product_price = 1200.00M, product_status = "Unavailable", category_ID = 3 }
        };

        internal static readonly List<Delivery> Deliveries = new();
        internal static readonly List<DeliveryDetails> DeliveryDetails = new();
        internal static readonly List<ProductSerial> ProductSerials = new();
        internal static readonly List<DelSerial> DeliverySerials = new();
        internal static readonly List<AppNotification> Notifications = new();
        internal static readonly List<ScheduleEvent> ScheduleEvents = new();

        internal static string LastDeliveryDate { get; set; } = string.Empty;
        internal static int GlobalBatchSequence { get; set; }

        internal static void SyncStockNotifications()
        {
            foreach (var product in Products)
            {
                string? alertStatus = product.product_status switch
                {
                    "Low Stock" => "low-stock",
                    "Out of Stock" => "out-of-stock",
                    _ => null
                };

                if (alertStatus == null)
                {
                    // Stock alerts represent current conditions, not permanent history.
                    Notifications.RemoveAll(item =>
                        item.product_ID == product.product_ID &&
                        item.notification_type is "low-stock" or "out-of-stock");
                    continue;
                }

                // Keep only the notification for the product's current stock state.
                Notifications.RemoveAll(item =>
                    item.product_ID == product.product_ID &&
                    item.notification_type is "low-stock" or "out-of-stock" &&
                    item.notification_type != alertStatus);

                string title = alertStatus == "out-of-stock" ? "Product out of stock" : "Low stock";
                string message = alertStatus == "out-of-stock"
                    ? $"{product.product_name} has no remaining stock."
                    : $"{product.product_name}: {product.product_quantity} left.";
                var existingAlert = Notifications.FirstOrDefault(item =>
                    item.product_ID == product.product_ID && item.notification_type == alertStatus);

                if (existingAlert != null)
                {
                    existingAlert.title = title;
                    existingAlert.message = message;
                    existingAlert.action_url = $"/Product/Details/{product.product_ID}";
                    continue;
                }

                Notifications.Insert(0, new AppNotification
                {
                    notification_ID = Notifications.Any() ? Notifications.Max(item => item.notification_ID) + 1 : 1,
                    product_ID = product.product_ID,
                    title = title,
                    message = message,
                    notification_type = alertStatus,
                    action_url = $"/Product/Details/{product.product_ID}",
                    created_at = DateTime.Now
                });
            }
        }
    }
}
