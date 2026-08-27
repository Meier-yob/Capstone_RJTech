using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Capstone_RJTech.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Capstone_RJTech.Controllers
{
    public class DeliveryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DeliveryController> _logger;
        private readonly DocumentSequenceService _documentSequences;

        public DeliveryController(
            ApplicationDbContext db,
            ILogger<DeliveryController> logger,
            DocumentSequenceService documentSequences)
        {
            _db = db;
            _logger = logger;
            _documentSequences = documentSequences;
        }

        public IActionResult Index() => RedirectToAction(nameof(DeliveryManagement));

        public IActionResult DeliveryManagement()
        {
            var deliveries = _db.Deliveries
                .Include(delivery => delivery.DeliveryDetails)
                .OrderByDescending(delivery => delivery.date_delivered)
                .ToList();
            return View(deliveries);
        }

        [HttpGet]
        public IActionResult Receive()
        {
            var products = _db.Products
                .Include(product => product.Category)
                .OrderBy(product => product.product_name)
                .ToList();

            bool statusChanged = false;
            foreach (var product in products)
            {
                string status = ProductController.EvaluateProductStatus(product);
                if (product.product_status != status)
                {
                    product.product_status = status;
                    statusChanged = true;
                }
            }
            if (statusChanged) _db.SaveChanges();

            DateTime now = DateTime.Now;
            ViewBag.NextBatchId = BuildBatchId(now, _documentSequences.PeekNextDeliveryNumber(now));
            return View("ReceiveProduct", products);
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var delivery = _db.Deliveries
                .Include(item => item.DeliveryDetails)
                    .ThenInclude(detail => detail.Product)
                        .ThenInclude(product => product!.Category)
                .FirstOrDefault(item => item.delivery_ID == id);
            if (delivery == null) return NotFound();

            return View("SelectedDeliveryView", delivery);
        }

        [HttpGet]
        public IActionResult GetDeliveries()
        {
            try
            {
                var deliveries = _db.Deliveries
                    .AsNoTracking()
                    .Include(delivery => delivery.DeliveryDetails)
                        .ThenInclude(detail => detail.Product)
                            .ThenInclude(product => product!.Category)
                    .ToList();

                var list = deliveries.Select(delivery => new
                {
                    delivery_ID = delivery.delivery_ID,
                    delivery_code = delivery.batch_ID.Replace("BATCH-", "DEL-"),
                    batch_ID = delivery.batch_ID,
                    date_delivered = delivery.date_delivered.ToString("yyyy-MM-dd HH:mm:ss"),
                    received_by = delivery.received_by,
                    is_archived = delivery.is_archived,
                    items = delivery.DeliveryDetails.Select(detail => new
                    {
                        deldetails_ID = detail.deldetails_ID,
                        product_ID = detail.product_ID,
                        formatted_code = ProductController.GetFormattedCodeForProduct(detail.Product),
                        product_name = detail.Product?.product_name ?? "Unknown",
                        product_brand = detail.Product?.product_brand ?? "Unknown",
                        product_description = detail.Product?.product_description ?? "N/A",
                        category_name = detail.Product?.Category?.category_name ?? "N/A",
                        quantity = detail.product_quantity
                    }).ToList()
                }).ToList();

                return Json(new { success = true, data = list });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error fetching delivery receipts.");
                return Json(new { success = false, message = "Failed to load delivery receipts." });
            }
        }

        public class DeliveryItemRequest
        {
            public int product_ID { get; set; }
            public int quantity { get; set; }
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
                request.items == null || request.items.Count == 0)
                return Json(new { success = false, message = "Complete the delivery information and select at least one product." });

            if (request.items.Any(item => item.quantity <= 0))
                return Json(new { success = false, message = "Every delivery item must have a whole-number quantity of at least one." });
            if (request.items.GroupBy(item => item.product_ID).Any(group => group.Count() > 1))
                return Json(new { success = false, message = "A product can only appear once in a delivery." });

            int[] productIds = request.items.Select(item => item.product_ID).ToArray();
            var products = _db.Products
                .Where(product => productIds.Contains(product.product_ID))
                .ToDictionary(product => product.product_ID);
            if (products.Count != productIds.Length)
                return Json(new { success = false, message = "One or more selected products no longer exist." });

            using var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                DateTime receivedAt = DateTime.Now;
                int documentNumber = _documentSequences.AllocateNextDeliveryNumber(receivedAt);
                string batchId = BuildBatchId(receivedAt, documentNumber);

                var delivery = new Delivery
                {
                    date_delivered = receivedAt,
                    received_by = request.received_by.Trim(),
                    batch_ID = batchId
                };
                _db.Deliveries.Add(delivery);

                foreach (var requestedItem in request.items)
                {
                    var product = products[requestedItem.product_ID];
                    bool isInitialReceipt = product.product_status == "Unavailable";
                    int previousQuantity = isInitialReceipt ? 0 : product.product_quantity;
                    int newQuantity = checked(previousQuantity + requestedItem.quantity);

                    delivery.DeliveryDetails.Add(new DeliveryDetails
                    {
                        product_quantity = requestedItem.quantity,
                        previous_quantity = previousQuantity,
                        new_quantity = newQuantity,
                        product_ID = product.product_ID,
                        Product = product
                    });

                    product.product_quantity = newQuantity;
                    product.product_status = "Available";
                    product.product_status = ProductController.EvaluateProductStatus(product);
                }

                _db.SaveChanges();
                transaction.Commit();

                return Json(new
                {
                    success = true,
                    message = "Delivery completed and recorded.",
                    redirectUrl = Url.Action(nameof(Details), new { id = delivery.delivery_ID }),
                    delivery = new
                    {
                        delivery_ID = delivery.delivery_ID,
                        delivery_code = delivery.batch_ID.Replace("BATCH-", "DEL-"),
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
                transaction.Rollback();
                _logger.LogError(exception, "Error completing delivery.");
                return Json(new { success = false, message = "An error occurred while completing the delivery." });
            }
        }

        [HttpPost]
        public IActionResult ArchiveDeliveryReceipt(int delivery_ID)
        {
            var delivery = _db.Deliveries.Find(delivery_ID);
            if (delivery == null)
                return Json(new { success = false, message = "Delivery not found." });

            delivery.is_archived = true;
            _db.SaveChanges();
            return Json(new { success = true, message = "Delivery archived." });
        }

        [HttpPost]
        public IActionResult DeleteDeliveryReceipt(int delivery_ID)
        {
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var delivery = _db.Deliveries
                    .Include(item => item.DeliveryDetails)
                        .ThenInclude(detail => detail.Product)
                    .FirstOrDefault(item => item.delivery_ID == delivery_ID);
                if (delivery == null) return Json(new { success = false, message = "Delivery not found." });
                if (!delivery.is_archived)
                    return Json(new { success = false, message = "Archive the delivery before deleting it." });

                foreach (var detail in delivery.DeliveryDetails)
                {
                    var product = detail.Product;
                    if (product == null) continue;

                    product.product_quantity = Math.Max(0, product.product_quantity - detail.product_quantity);
                    product.product_status = product.product_quantity <= 0
                        ? "Out of Stock"
                        : ProductController.EvaluateProductStatus(product);
                }

                _db.Deliveries.Remove(delivery);
                _db.SaveChanges();
                transaction.Commit();
                return Json(new { success = true, message = "Delivery receipt deleted and inventory rolled back." });
            }
            catch (Exception exception)
            {
                transaction.Rollback();
                _logger.LogError(exception, "Error deleting delivery receipt.");
                return Json(new { success = false, message = "An error occurred while deleting the delivery receipt." });
            }
        }

        [HttpGet]
        public IActionResult GetNextDeliveryInfo()
        {
            DateTime now = DateTime.Now;
            string batchId = BuildBatchId(now, _documentSequences.PeekNextDeliveryNumber(now));
            return Json(new
            {
                success = true,
                delivery_ID = batchId.Replace("BATCH-", "DEL-"),
                batch_ID = batchId
            });
        }

        private static string BuildBatchId(DateTime date, int sequence)
            => $"BATCH-{date:yyyyMMdd}-{sequence:D3}";

        [HttpGet]
        public IActionResult SearchProductForDelivery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new { success = false, message = "Please enter a valid Product ID or Code." });

            var products = _db.Products.Include(product => product.Category).AsNoTracking().ToList();
            Product? product = null;
            if (int.TryParse(query, out int parsedId))
                product = products.FirstOrDefault(item => item.product_ID == parsedId);
            product ??= products.FirstOrDefault(item => ProductController.GetFormattedCodeForProduct(item).Equals(query, StringComparison.OrdinalIgnoreCase));
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
                category_name = product.Category?.category_name ?? "N/A",
                current_quantity = product.product_quantity
            });
        }
    }
}
