using Capstone_RJTech.Models;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_RJTech.Controllers
{
    public class DeliveryController : Controller
    {
        private readonly ILogger<DeliveryController> _logger;

        private static List<ProductCategory> Categories => InventoryStore.Categories;
        private static List<Product> Products => InventoryStore.Products;
        private static List<Delivery> Deliveries => InventoryStore.Deliveries;
        private static List<DeliveryDetails> DeliveryDetails => InventoryStore.DeliveryDetails;
        private static List<ProductSerial> ProductSerials => InventoryStore.ProductSerials;
        private static List<DelSerial> DeliverySerials => InventoryStore.DeliverySerials;

        public DeliveryController(ILogger<DeliveryController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index() => RedirectToAction(nameof(DeliveryManagement));

        public IActionResult DeliveryManagement()
        {
            foreach (var delivery in Deliveries)
                delivery.DeliveryDetails = DeliveryDetails.Where(detail => detail.delivery_ID == delivery.delivery_ID).ToList();
            return View(Deliveries.OrderByDescending(delivery => delivery.date_delivered).ToList());
        }

        [HttpGet]
        public IActionResult Receive()
        {
            foreach (var product in Products)
            {
                product.Category = Categories.FirstOrDefault(category => category.category_ID == product.category_ID);
                product.product_status = ProductController.EvaluateProductStatus(product);
            }
            ViewBag.NextBatchId = BuildNextBatchId();
            return View("ReceiveProduct", Products.OrderBy(product => product.product_name).ToList());
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var delivery = Deliveries.FirstOrDefault(item => item.delivery_ID == id);
            if (delivery == null) return NotFound();

            delivery.DeliveryDetails = DeliveryDetails.Where(detail => detail.delivery_ID == id).ToList();
            foreach (var detail in delivery.DeliveryDetails)
                detail.Product = Products.FirstOrDefault(product => product.product_ID == detail.product_ID);
            return View("SelectedDeliveryView", delivery);
        }

        [HttpGet]
        public IActionResult GetDeliveries()
        {
            try
            {
                var list = Deliveries.Select(delivery =>
                {
                    var items = DeliveryDetails.Where(detail => detail.delivery_ID == delivery.delivery_ID).Select(detail =>
                    {
                        var product = Products.FirstOrDefault(item => item.product_ID == detail.product_ID);
                        var category = product == null ? null : Categories.FirstOrDefault(item => item.category_ID == product.category_ID);
                        var serials = DeliverySerials.Where(serial => serial.deldetails_ID == detail.deldetails_ID).Select(serial => serial.serial_No).ToList();
                        return new
                        {
                            deldetails_ID = detail.deldetails_ID,
                            product_ID = detail.product_ID,
                            formatted_code = ProductController.GetFormattedCodeForProduct(product),
                            product_name = product?.product_name ?? "Unknown",
                            product_brand = product?.product_brand ?? "Unknown",
                            product_description = product?.product_description ?? "N/A",
                            category_name = category?.category_name ?? "N/A",
                            quantity = detail.product_quantity,
                            serials,
                            serials_pending = Math.Max(0, detail.product_quantity - serials.Count)
                        };
                    }).ToList();

                    return new
                    {
                        delivery_ID = delivery.delivery_ID,
                        delivery_code = delivery.batch_ID?.Replace("BATCH-", "DEL-") ?? $"DEL-{delivery.date_delivered:yyyyMMdd}-{delivery.delivery_ID:D3}",
                        batch_ID = delivery.batch_ID,
                        date_delivered = delivery.date_delivered.ToString("yyyy-MM-dd HH:mm:ss"),
                        received_by = delivery.received_by,
                        items,
                        serials_pending = items.Sum(item => item.serials_pending)
                    };
                }).ToList();

                return Json(new { success = true, data = list });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error fetching delivery receipts.");
                return Json(new { success = false, message = "Failed to load delivery receipts." });
            }
        }

        [HttpPost]
        public IActionResult CheckSerials([FromBody] List<string> serials)
        {
            try
            {
                if (serials == null || serials.Count == 0)
                    return Json(new { success = true, data = new List<object>() });

                var results = new List<object>();
                foreach (string serialNumber in serials.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var productSerial = ProductSerials.FirstOrDefault(serial => string.Equals(serial.serial_No, serialNumber, StringComparison.OrdinalIgnoreCase));
                    var deliverySerial = DeliverySerials.FirstOrDefault(serial => string.Equals(serial.serial_No, serialNumber, StringComparison.OrdinalIgnoreCase));
                    if (productSerial != null)
                        results.Add(new { serial = serialNumber, exists = true, location = "product", product_ID = productSerial.product_ID, batch_ID = productSerial.batch_ID });
                    else if (deliverySerial != null)
                        results.Add(new { serial = serialNumber, exists = true, location = "delivery", deldetails_ID = deliverySerial.deldetails_ID });
                    else
                        results.Add(new { serial = serialNumber, exists = false });
                }

                return Json(new { success = true, data = results });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error checking serials.");
                return Json(new { success = false, message = "Failed to validate serials." });
            }
        }

        public class DeliveryItemRequest
        {
            public int product_ID { get; set; }
            public int quantity { get; set; }
            public List<string>? serialNumbers { get; set; }
            public List<string>? serials { get; set; }
        }

        public class DeliveryCompleteRequest
        {
            public string? received_by { get; set; }
            public string? batch_ID { get; set; }
            public DateTime? delivery_date { get; set; }
            public List<DeliveryItemRequest>? items { get; set; }
        }

        [HttpPost]
        public IActionResult CompleteDelivery([FromBody] DeliveryCompleteRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.received_by) ||
                string.IsNullOrWhiteSpace(request.batch_ID) || request.items == null || request.items.Count == 0)
                return Json(new { success = false, message = "Complete the delivery information and select at least one product." });

            if (request.items.Any(item => item.quantity <= 0))
                return Json(new { success = false, message = "Every delivery item must have a whole-number quantity of at least one." });
            if (request.items.Any(item => !Products.Any(product => product.product_ID == item.product_ID)))
                return Json(new { success = false, message = "One or more selected products no longer exist." });
            if (request.items.GroupBy(item => item.product_ID).Any(group => group.Count() > 1))
                return Json(new { success = false, message = "A product can only appear once in a delivery." });

            int deliveryCount = Deliveries.Count;
            int detailCount = DeliveryDetails.Count;
            int productSerialCount = ProductSerials.Count;
            int deliverySerialCount = DeliverySerials.Count;
            string priorDeliveryDate = InventoryStore.LastDeliveryDate;
            int priorSequence = InventoryStore.GlobalBatchSequence;
            var snapshots = request.items.ToDictionary(item => item.product_ID, item =>
            {
                var product = Products.First(product => product.product_ID == item.product_ID);
                return (product.product_quantity, product.product_status);
            });

            try
            {
                string today = DateTime.Now.ToString("yyyyMMdd");
                if (InventoryStore.LastDeliveryDate != today)
                {
                    InventoryStore.LastDeliveryDate = today;
                    InventoryStore.GlobalBatchSequence = 1;
                }
                else
                {
                    InventoryStore.GlobalBatchSequence++;
                }

                var delivery = new Delivery
                {
                    delivery_ID = Deliveries.Any() ? Deliveries.Max(item => item.delivery_ID) + 1 : 1,
                    date_delivered = request.delivery_date?.Date.Add(DateTime.Now.TimeOfDay) ?? DateTime.Now,
                    received_by = request.received_by.Trim(),
                    batch_ID = request.batch_ID.Trim()
                };
                Deliveries.Add(delivery);

                foreach (var requestedItem in request.items)
                {
                    var product = Products.First(item => item.product_ID == requestedItem.product_ID);
                    // A new product displays its planned quantity in the
                    // catalog, but its first physical receipt starts from zero.
                    bool isInitialReceipt = product.product_status == "Unavailable";
                    int previousQuantity = isInitialReceipt ? 0 : product.product_quantity;
                    var detail = new DeliveryDetails
                    {
                        deldetails_ID = DeliveryDetails.Any() ? DeliveryDetails.Max(item => item.deldetails_ID) + 1 : 1,
                        product_quantity = requestedItem.quantity,
                        previous_quantity = previousQuantity,
                        new_quantity = checked(previousQuantity + requestedItem.quantity),
                        product_ID = requestedItem.product_ID,
                        delivery_ID = delivery.delivery_ID
                    };
                    DeliveryDetails.Add(detail);
                    delivery.DeliveryDetails.Add(detail);
                    product.DeliveryDetails.Add(detail);

                    product.product_quantity = detail.new_quantity;
                    // Completing the first receipt ends the Unavailable state.
                    product.product_status = "Available";
                    product.product_status = ProductController.EvaluateProductStatus(product);
                }

                string deliveryCode = delivery.batch_ID.Replace("BATCH-", "DEL-");
                return Json(new
                {
                    success = true,
                    message = "Delivery completed and recorded.",
                    redirectUrl = Url.Action(nameof(Details), new { id = delivery.delivery_ID }),
                    delivery = new
                    {
                        delivery_ID = delivery.delivery_ID,
                        delivery_code = deliveryCode,
                        batch_ID = delivery.batch_ID,
                        date_delivered = delivery.date_delivered,
                        received_by = delivery.received_by,
                        items = delivery.DeliveryDetails.Select(detail => new
                        {
                            detail.product_ID,
                            quantity = detail.product_quantity,
                            detail.previous_quantity,
                            detail.new_quantity
                        }).ToList()
                    }
                });
            }
            catch (Exception exception)
            {
                Deliveries.RemoveRange(deliveryCount, Deliveries.Count - deliveryCount);
                DeliveryDetails.RemoveRange(detailCount, DeliveryDetails.Count - detailCount);
                ProductSerials.RemoveRange(productSerialCount, ProductSerials.Count - productSerialCount);
                DeliverySerials.RemoveRange(deliverySerialCount, DeliverySerials.Count - deliverySerialCount);
                InventoryStore.LastDeliveryDate = priorDeliveryDate;
                InventoryStore.GlobalBatchSequence = priorSequence;
                foreach (var snapshot in snapshots)
                {
                    var product = Products.First(item => item.product_ID == snapshot.Key);
                    (product.product_quantity, product.product_status) = snapshot.Value;
                }
                _logger.LogError(exception, "Error completing delivery.");
                return Json(new { success = false, message = "An error occurred while completing the delivery." });
            }
        }

        public class DeliverySerialItemRequest
        {
            public int deldetails_ID { get; set; }
            public List<string>? serialNumbers { get; set; }
        }

        public class AssignDeliverySerialsRequest
        {
            public int delivery_ID { get; set; }
            public List<DeliverySerialItemRequest>? items { get; set; }
        }

        [HttpPost]
        public IActionResult AssignDeliverySerials([FromBody] AssignDeliverySerialsRequest request)
        {
            var delivery = Deliveries.FirstOrDefault(item => item.delivery_ID == request.delivery_ID);
            if (delivery == null) return Json(new { success = false, message = "Delivery receipt not found." });
            if (request.items == null || request.items.Count == 0)
                return Json(new { success = false, message = "No serial numbers were submitted." });

            var normalizedItems = new List<(DeliveryDetails Detail, List<string> Serials)>();
            var allSubmitted = new List<string>();
            foreach (var item in request.items)
            {
                var detail = DeliveryDetails.FirstOrDefault(candidate => candidate.deldetails_ID == item.deldetails_ID && candidate.delivery_ID == request.delivery_ID);
                if (detail == null) return Json(new { success = false, message = "A delivery product was not found." });

                var serials = NormalizeSerials(item.serialNumbers);
                if (serials.Count != detail.product_quantity)
                    return Json(new { success = false, message = $"Enter exactly {detail.product_quantity} serial number(s) for {ProductController.GetFormattedCodeForProduct(Products.First(product => product.product_ID == detail.product_ID))}." });

                var existing = DeliverySerials.Where(serial => serial.deldetails_ID == detail.deldetails_ID).Select(serial => serial.serial_No).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (!existing.IsSubsetOf(serials))
                    return Json(new { success = false, message = "Existing serial numbers cannot be removed or replaced." });

                normalizedItems.Add((detail, serials));
                allSubmitted.AddRange(serials);
            }

            if (allSubmitted.Count != allSubmitted.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                return Json(new { success = false, message = "Each serial number must be unique." });

            var submittedSet = allSubmitted.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var conflicts = ProductSerials.Where(existing => submittedSet.Contains(existing.serial_No)).Where(existing =>
                !normalizedItems.Any(item => item.Detail.product_ID == existing.product_ID &&
                    DeliverySerials.Any(serial => serial.deldetails_ID == item.Detail.deldetails_ID && string.Equals(serial.serial_No, existing.serial_No, StringComparison.OrdinalIgnoreCase))))
                .Select(existing => existing.serial_No).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (conflicts.Count > 0)
                return Json(new { success = false, message = $"Serial number(s) already assigned: {string.Join(", ", conflicts)}" });

            int addedCount = 0;
            foreach (var item in normalizedItems)
            {
                var product = Products.First(entry => entry.product_ID == item.Detail.product_ID);
                var existing = DeliverySerials.Where(serial => serial.deldetails_ID == item.Detail.deldetails_ID).Select(serial => serial.serial_No).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (string serialNumber in item.Serials.Where(serial => !existing.Contains(serial)))
                {
                    var deliverySerial = new DelSerial { serial_No = serialNumber, deldetails_ID = item.Detail.deldetails_ID };
                    var productSerial = new ProductSerial { serial_No = serialNumber, product_ID = product.product_ID, batch_ID = delivery.batch_ID };
                    DeliverySerials.Add(deliverySerial);
                    ProductSerials.Add(productSerial);
                    item.Detail.DelSerials.Add(deliverySerial);
                    product.ProductSerials.Add(productSerial);
                    addedCount++;
                }
            }

            return Json(new { success = true, message = $"{addedCount} serial number(s) assigned.", delivery_ID = delivery.delivery_ID });
        }

        [HttpPost]
        public IActionResult DeleteDeliveryReceipt(int delivery_ID)
        {
            try
            {
                var delivery = Deliveries.FirstOrDefault(item => item.delivery_ID == delivery_ID);
                if (delivery == null) return Json(new { success = false, message = "Delivery not found." });

                foreach (var detail in DeliveryDetails.Where(item => item.delivery_ID == delivery_ID).ToList())
                {
                    var product = Products.FirstOrDefault(item => item.product_ID == detail.product_ID);
                    if (product != null)
                    {
                        product.product_quantity = Math.Max(0, product.product_quantity - detail.product_quantity);
                        product.DeliveryDetails.Remove(detail);
                        ProductSerials.RemoveAll(serial => serial.product_ID == product.product_ID && serial.batch_ID == delivery.batch_ID);
                        // A rolled-back completed receipt represents zero stock,
                        // so its status becomes Out of Stock rather than Unavailable.
                        product.product_status = product.product_quantity <= 0
                            ? "Out of Stock"
                            : ProductController.EvaluateProductStatus(product);
                    }
                    DeliverySerials.RemoveAll(serial => serial.deldetails_ID == detail.deldetails_ID);
                    DeliveryDetails.Remove(detail);
                }
                Deliveries.Remove(delivery);
                return Json(new { success = true, message = "Delivery receipt deleted and inventory rolled back." });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error deleting delivery receipt.");
                return Json(new { success = false, message = "An error occurred while deleting the delivery receipt." });
            }
        }

        [HttpGet]
        public IActionResult GetNextDeliveryInfo()
        {
            string today = DateTime.Now.ToString("yyyyMMdd");
            int nextSequence = InventoryStore.LastDeliveryDate == today ? InventoryStore.GlobalBatchSequence + 1 : 1;
            return Json(new
            {
                success = true,
                delivery_ID = $"DEL-{today}-{nextSequence:D3}",
                batch_ID = $"BATCH-{today}-{nextSequence:D3}"
            });
        }

        private static string BuildNextBatchId()
        {
            string today = DateTime.Now.ToString("yyyyMMdd");
            int nextSequence = InventoryStore.LastDeliveryDate == today ? InventoryStore.GlobalBatchSequence + 1 : 1;
            return $"BATCH-{today}-{nextSequence:D3}";
        }

        [HttpGet]
        public IActionResult SearchProductForDelivery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new { success = false, message = "Please enter a valid Product ID or Code." });

            Product? product = null;
            if (int.TryParse(query, out int parsedId))
                product = Products.FirstOrDefault(item => item.product_ID == parsedId);
            product ??= Products.FirstOrDefault(item => ProductController.GetFormattedCodeForProduct(item).Equals(query, StringComparison.OrdinalIgnoreCase));
            if (product == null)
                return Json(new { success = false, message = $"No product found matching '{query}'." });

            return Json(new
            {
                success = true,
                product_ID = product.product_ID,
                formatted_code = ProductController.GetFormattedCodeForProduct(product),
                product_name = product.product_name,
                product_brand = product.product_brand,
                product_description = product.product_description,
                category_name = Categories.FirstOrDefault(category => category.category_ID == product.category_ID)?.category_name ?? "N/A",
                current_quantity = product.product_quantity
            });
        }

        private static List<string> NormalizeSerials(IEnumerable<string>? serials)
            => (serials ?? Array.Empty<string>()).Where(serial => !string.IsNullOrWhiteSpace(serial)).Select(serial => serial.Trim()).ToList();
    }
}
