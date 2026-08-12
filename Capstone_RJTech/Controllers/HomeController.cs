using Capstone_RJTech.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Capstone_RJTech.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // In-Memory Categories
        private static readonly List<ProductCategory> _categories = new List<ProductCategory>
        {
            new ProductCategory { category_ID = 1, category_name = "Monitors" },
            new ProductCategory { category_ID = 2, category_name = "Mouses" },
            new ProductCategory { category_ID = 3, category_name = "Keyboards" },
            new ProductCategory { category_ID = 4, category_name = "Headsets" }
        };

        // In-Memory Static List for Products
        private static List<Product> _products = new List<Product>
        {
            new Product { product_ID = 1, product_name = "Optical Wired Mouse", product_brand = "A4 Tech", product_description = "Optical Wired Mouse", product_quantity = 0, Product_price = 200.00M, product_status = "Unavailable", has_received_initial_delivery = false, reorder_level = 5, category_ID = 2 },
            new Product { product_ID = 2, product_name = "Mechanical Keyboard", product_brand = "Logitech", product_description = "Mechanical Keyboard", product_quantity = 0, Product_price = 1200.00M, product_status = "Unavailable", has_received_initial_delivery = false, reorder_level = 5, category_ID = 3 }
        };

        // In-memory delivery & serial trackers
        private static List<Delivery> _deliveries = new List<Delivery>();
        private static List<DeliveryDetails> _deliveryDetails = new List<DeliveryDetails>();
        private static List<ProductSerial> _productSerials = new List<ProductSerial>();
        private static List<DelSerial> _delSerials = new List<DelSerial>();
        private static List<Reorder> _reorders = new List<Reorder>();

        // Sequence Trackers
        private static string _lastDeliveryDate = "";
        private static int _globalBatchSequence = 0;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Load Delivery Details Partial View (Supports both deliveryId and id query parameters)
        [HttpGet]
        public IActionResult SelectedDeliveryDetailView(int? deliveryId, int? id)
        {
            var key = deliveryId ?? id;
            if (!key.HasValue)
            {
                return Content("<div class='p-3 text-center text-danger'>Missing delivery identifier.</div>", "text/html");
            }

            var delivery = _deliveries.FirstOrDefault(d => d.delivery_ID == key.Value);
            if (delivery == null)
            {
                return Content($"<div class='p-3 text-center text-danger'>Delivery receipt #{key.Value} not found.</div>", "text/html");
            }

            // Hydrate DeliveryDetails
            delivery.DeliveryDetails = _deliveryDetails
                .Where(dd => dd.delivery_ID == key.Value)
                .ToList();

            foreach (var detail in delivery.DeliveryDetails)
            {
                // Hydrate Product reference to avoid NullReferenceException in Razor view
                detail.Product = _products.FirstOrDefault(p => p.product_ID == detail.product_ID);

                // Hydrate Serials
                detail.DelSerials = _delSerials
                    .Where(s => s.deldetails_ID == detail.deldetails_ID)
                    .ToList();
            }

            return PartialView("SelectedDeliveryDetailView", delivery);
        }

        // GET: Fetch list of delivery receipts (summary)
        [HttpGet]
        public IActionResult GetDeliveries()
        {
            try
            {
                var list = _deliveries.Select(d =>
                {
                    var items = _deliveryDetails
                        .Where(dd => dd.delivery_ID == d.delivery_ID)
                        .Select(dd =>
                        {
                            var product = _products.FirstOrDefault(p => p.product_ID == dd.product_ID);
                            var category = product != null ? _categories.FirstOrDefault(c => c.category_ID == product.category_ID) : null;

                            var serials = _delSerials
                                .Where(ds => ds.deldetails_ID == dd.deldetails_ID)
                                .Select(ds => ds.serial_No)
                                .ToList();

                            return new
                            {
                                deldetails_ID = dd.deldetails_ID,
                                product_ID = dd.product_ID,
                                formatted_code = GetFormattedCodeForProduct(product),
                                product_brand = product?.product_brand ?? "Unknown",
                                product_description = product?.product_description ?? "N/A",
                                category_name = category?.category_name ?? "N/A",
                                quantity = dd.product_quantity,
                                serials = serials
                            };
                        })
                        .ToList();

                    var deliveryCode = d.batch_ID?.Replace("BATCH-", "DEL-") ?? $"DEL-{d.date_delivered:yyyyMMdd}-{d.delivery_ID:D3}";

                    return new
                    {
                        delivery_ID = d.delivery_ID,
                        delivery_code = deliveryCode,
                        batch_ID = d.batch_ID,
                        date_delivered = d.date_delivered.ToString("yyyy-MM-dd HH:mm:ss"),
                        received_by = d.received_by,
                        items = items
                    };
                }).ToList();

                return Json(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching delivery receipts list.");
                return Json(new { success = false, message = "Failed to load delivery receipts." });
            }
        }

        [HttpPost]
        public IActionResult CheckSerials([FromBody] List<string> serials)
        {
            try
            {
                if (serials == null || !serials.Any())
                {
                    return Json(new { success = true, data = new List<object>() });
                }

                var results = new List<object>();
                foreach (var s in serials.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var existsInProductSerials = _productSerials.FirstOrDefault(ps => string.Equals(ps.serial_No, s, StringComparison.OrdinalIgnoreCase));
                    var existsInDelSerials = _delSerials.FirstOrDefault(ds => string.Equals(ds.serial_No, s, StringComparison.OrdinalIgnoreCase));

                    if (existsInProductSerials != null)
                    {
                        results.Add(new { serial = s, exists = true, location = "product", product_ID = existsInProductSerials.product_ID, batch_ID = existsInProductSerials.batch_ID });
                        continue;
                    }

                    if (existsInDelSerials != null)
                    {
                        results.Add(new { serial = s, exists = true, location = "delivery", deldetails_ID = existsInDelSerials.deldetails_ID });
                        continue;
                    }

                    results.Add(new { serial = s, exists = false });
                }

                return Json(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking serials.");
                return Json(new { success = false, message = "Failed to validate serials." });
            }
        }

        public IActionResult Dashboard()
        {
            foreach (var product in _products)
            {
                product.product_status = EvaluateProductStatus(product);
            }

            ViewBag.TotalProducts = _products.Count;
            ViewBag.UnavailableCount = _products.Count(p => p.product_status == "Unavailable");
            ViewBag.LowStockCount = _products.Count(p => p.product_status == "Low Stock");
            ViewBag.OutOfStockCount = _products.Count(p => p.product_status == "Out of Stock");
            ViewBag.PendingOrderCount = _reorders.Count(r => r.reorder_status == "Pending" || r.reorder_status == "Partially Received");
            ViewBag.TodayDeliveryCount = _deliveries.Count(d => d.date_delivered.Date == DateTime.Today);
            return View();
        }
        public IActionResult Privacy() => View();
        public IActionResult DeliveryManagement()
        {
            ViewBag.Categories = _categories;
            return View();
        }

        public IActionResult ProductManagement()
        {
            ViewBag.Categories = _categories;
            ViewBag.PendingReorderProductIds = _reorders
                .Where(r => r.reorder_status == "Pending" || r.reorder_status == "Partially Received")
                .Select(r => r.product_ID)
                .ToHashSet();

            foreach (var prod in _products)
            {
                prod.Category = _categories.FirstOrDefault(c => c.category_ID == prod.category_ID);
                prod.product_status = EvaluateProductStatus(prod);
            }

            return View(_products);
        }

        [HttpGet]
        public IActionResult GetReorders(bool pendingOnly = false)
        {
            var reorders = _reorders
                .Where(r => !pendingOnly || r.reorder_status == "Pending" || r.reorder_status == "Partially Received")
                .OrderByDescending(r => r.date_requested)
                .Select(r =>
                {
                    var product = _products.FirstOrDefault(p => p.product_ID == r.product_ID);
                    return new
                    {
                        reorder_ID = r.reorder_ID,
                        product_ID = r.product_ID,
                        product_code = product == null ? "N/A" : GetFormattedCodeForProduct(product),
                        product_name = product?.product_name ?? "Unknown",
                        product_brand = product?.product_brand ?? "Unknown",
                        product_description = product?.product_description ?? "",
                        category_name = product == null ? "N/A" : _categories.FirstOrDefault(c => c.category_ID == product.category_ID)?.category_name ?? "N/A",
                        current_quantity = product?.product_quantity ?? 0,
                        reorder_level = product?.reorder_level ?? 0,
                        ordered_quantity = r.ordered_quantity,
                        received_quantity = r.received_quantity,
                        remaining_quantity = Math.Max(0, r.ordered_quantity - r.received_quantity),
                        reorder_status = r.reorder_status,
                        date_requested = r.date_requested
                    };
                })
                .ToList();

            return Json(new { success = true, data = reorders });
        }

        [HttpPost]
        public IActionResult CreateReorder(int product_ID, int quantity)
        {
            var product = _products.FirstOrDefault(p => p.product_ID == product_ID);
            if (product == null)
                return Json(new { success = false, message = "Product not found." });

            product.product_status = EvaluateProductStatus(product);
            if (product.product_status != "Low Stock" && product.product_status != "Out of Stock")
                return Json(new { success = false, message = "Only Low Stock or Out of Stock items can be reordered." });

            var existing = _reorders.FirstOrDefault(r =>
                r.product_ID == product_ID &&
                (r.reorder_status == "Pending" || r.reorder_status == "Partially Received"));

            if (existing != null)
            {
                return Json(new
                {
                    success = false,
                    message = "This product already has a pending reorder.",
                    reorder_ID = existing.reorder_ID
                });
            }

            if (quantity <= 0)
                return Json(new { success = false, message = "Enter a valid reorder quantity." });

            var reorder = new Reorder
            {
                reorder_ID = _reorders.Any() ? _reorders.Max(r => r.reorder_ID) + 1 : 1,
                product_ID = product_ID,
                ordered_quantity = quantity,
                received_quantity = 0,
                reorder_status = "Pending",
                date_requested = DateTime.Now
            };

            _reorders.Add(reorder);
            return Json(new
            {
                success = true,
                message = "Reorder submitted.",
                data = new { reorder_ID = reorder.reorder_ID, reorder_status = reorder.reorder_status }
            });
        }

        [HttpGet]
        public IActionResult SelectedItemView(int id)
        {
            var product = _products.FirstOrDefault(p => p.product_ID == id);
            if (product == null) return NotFound();

            product.Category = _categories.FirstOrDefault(c => c.category_ID == product.category_ID);
            product.product_status = EvaluateProductStatus(product);
            ViewBag.FormattedCode = GetFormattedCodeForProduct(product);
            ViewBag.CategoryName = product.Category?.category_name ?? "N/A";
            ViewBag.Serials = _productSerials
                .Where(s => s.product_ID == id)
                .Select(s => new { s.serial_No, s.batch_ID })
                .ToList();

            return PartialView("SelectedItemView", product);
        }

        [HttpGet]
        public IActionResult AddProductView()
        {
            ViewBag.Categories = _categories;
            return View();
        }

        public IActionResult Archive() => View();
        public IActionResult CreateDelivery() => View();

        public static string GetCategoryPrefix(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return "P";

            if (categoryName.StartsWith("Keyboard", StringComparison.OrdinalIgnoreCase)) return "K";
            if (categoryName.StartsWith("Mouse", StringComparison.OrdinalIgnoreCase) || categoryName.StartsWith("Mouses", StringComparison.OrdinalIgnoreCase)) return "M";
            if (categoryName.StartsWith("Headset", StringComparison.OrdinalIgnoreCase)) return "H";
            if (categoryName.StartsWith("Monitor", StringComparison.OrdinalIgnoreCase)) return "Mo";

            return categoryName.Substring(0, 1).ToUpper();
        }

        public static string FormatProductCode(string categoryName, int productId)
        {
            string prefix = GetCategoryPrefix(categoryName);
            return $"{prefix}-{productId:D3}";
        }

        public static string GetFormattedCodeForProduct(Product product)
        {
            if (product == null) return "P-000";

            var category = _categories.FirstOrDefault(c => c.category_ID == product.category_ID);
            string categoryName = category?.category_name ?? "";

            return FormatProductCode(categoryName, product.product_ID);
        }

        [HttpGet]
        public IActionResult GetNextCategoryCode(int categoryId)
        {
            var category = _categories.FirstOrDefault(c => c.category_ID == categoryId);
            if (category == null) return Json(new { success = false, message = "Invalid category." });

            int nextProductId = _products.Any() ? _products.Max(p => p.product_ID) + 1 : 1;
            string formattedCode = FormatProductCode(category.category_name, nextProductId);

            return Json(new { success = true, formattedCode = formattedCode });
        }

        [HttpPost]
        public IActionResult Create([FromForm] Product product)
        {
            
            ModelState.Remove("Category");
            ModelState.Remove("product_quantity");

            if (ModelState.IsValid)
            {
                product.product_name = NormalizeProductIdentity(product.product_name);
                product.product_brand = NormalizeProductIdentity(product.product_brand);

                if (string.IsNullOrWhiteSpace(product.product_name) || string.IsNullOrWhiteSpace(product.product_brand))
                {
                    return Json(new { success = false, message = "Validation failed.", errors = new[] { "Product name and brand are required." } });
                }

                bool itemAlreadyExists = _products.Any(existing =>
                    existing.category_ID == product.category_ID &&
                    string.Equals(NormalizeProductIdentity(existing.product_name), product.product_name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(NormalizeProductIdentity(existing.product_brand), product.product_brand, StringComparison.OrdinalIgnoreCase));

                if (itemAlreadyExists)
                {
                    return Json(new { success = false, message = "Item Already Exists" });
                }

                product.product_ID = _products.Any() ? _products.Max(p => p.product_ID) + 1 : 1;
                product.product_quantity = 0;
                product.product_status = "Unavailable";
                product.has_received_initial_delivery = false;
                _products.Add(product);

                var category = _categories.FirstOrDefault(c => c.category_ID == product.category_ID);
                string formattedCode = FormatProductCode(category?.category_name, product.product_ID);

                return Json(new
                {
                    success = true,
                    message = "Product created successfully!",
                    data = new
                    {
                        product_ID = product.product_ID,
                        formatted_code = formattedCode,
                        product_name = product.product_name,
                        product_brand = product.product_brand,
                        category_name = category?.category_name ?? "N/A",
                        product_description = product.product_description,
                        product_quantity = product.product_quantity,
                        product_price = product.Product_price,
                        product_status = product.product_status,
                        reorder_level = product.reorder_level
                    }
                });
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = "Validation failed.", errors = errors });
        }

        private static string NormalizeProductIdentity(string? value)
        {
            return string.Join(" ", (value ?? string.Empty)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        [HttpGet]
        public IActionResult GetDetails(int id)
        {
            var product = _products.FirstOrDefault(p => p.product_ID == id);
            if (product == null) return Json(new { success = false, message = "Product not found." });

            product.product_status = EvaluateProductStatus(product);
            string formattedCode = GetFormattedCodeForProduct(product);
            var categoryName = _categories.FirstOrDefault(c => c.category_ID == product.category_ID)?.category_name ?? "N/A";

            var serials = _productSerials
                .Where(s => s.product_ID == id)
                .Select(s => new { s.serial_No, s.batch_ID })
                .ToList();

            return Json(new
            {
                success = true,
                product_ID = product.product_ID,
                formatted_code = formattedCode,
                product_name = product.product_name,
                product_brand = product.product_brand,
                category_name = categoryName,
                product_quantity = product.product_quantity,
                product_price = product.Product_price,
                product_description = product.product_description,
                product_status = product.product_status,
                reorder_level = product.reorder_level,
                serials = serials
            });
        }

        [HttpPost]
        public IActionResult UpdateProductDetails(int product_ID, string product_name, string product_brand, decimal product_price, string product_description, string product_status, int reorder_level)
        {

            try
            {

                var product = _products.FirstOrDefault(p => p.product_ID == product_ID);

                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                string normalizedName = NormalizeProductIdentity(product_name);
                string normalizedBrand = NormalizeProductIdentity(product_brand);

                if (string.IsNullOrWhiteSpace(normalizedName) || string.IsNullOrWhiteSpace(normalizedBrand))
                {
                    return Json(new { success = false, message = "Product name and brand are required." });
                }

                bool itemAlreadyExists = _products.Any(existing =>
                    existing.product_ID != product_ID &&
                    existing.category_ID == product.category_ID &&
                    string.Equals(NormalizeProductIdentity(existing.product_name), normalizedName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(NormalizeProductIdentity(existing.product_brand), normalizedBrand, StringComparison.OrdinalIgnoreCase));

                if (itemAlreadyExists)
                {
                    return Json(new { success = false, message = "Item Already Exists" });
                }

                product.product_name = normalizedName;
                product.product_brand = normalizedBrand;
                product.Product_price = product_price;
                product.product_description = product_description?.Trim();
                product.reorder_level = reorder_level;
                product.product_status = EvaluateProductStatus(product);

                return Json(new { success = true, message = "Product updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product details.");
                return Json(new { success = false, message = "An error occurred while updating the product." });
            }
        }

        private static string EvaluateProductStatus(Product product)
        {
            if (product == null) return "Available";

            if (!product.has_received_initial_delivery)
            {
                return "Unavailable";
            }

            if (product.product_quantity <= 0)
            {
                return "Out of Stock";
            }

            if (product.product_quantity <= product.reorder_level && product.product_quantity > 0)
            {
                return "Low Stock";
            }

            return "Available";
        }

        [HttpPost]
        public IActionResult DeleteProduct(int id)
        {
            try
            {
                var product = _products.FirstOrDefault(p => p.product_ID == id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                _productSerials.RemoveAll(ps => ps.product_ID == id);
                _reorders.RemoveAll(r => r.product_ID == id);
                _products.Remove(product);

                return Json(new { success = true, message = "Product deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product.");
                return Json(new { success = false, message = "An error occurred while deleting the product." });
            }
        }

        [HttpGet]
        public IActionResult SearchProducts(string query)
        {
            try
            {
                var filteredProducts = _products.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    string searchTerm = query.Trim().ToLower();

                    filteredProducts = filteredProducts.Where(p =>
                    {
                        var categoryName = _categories.FirstOrDefault(c => c.category_ID == p.category_ID)?.category_name ?? "";
                        var formattedCode = GetFormattedCodeForProduct(p);

                        return formattedCode.ToLower().Contains(searchTerm) ||
                               (!string.IsNullOrEmpty(p.product_name) && p.product_name.ToLower().Contains(searchTerm)) ||
                               (!string.IsNullOrEmpty(p.product_brand) && p.product_brand.ToLower().Contains(searchTerm)) ||
                               (!string.IsNullOrEmpty(categoryName) && categoryName.ToLower().Contains(searchTerm)) ||
                               (!string.IsNullOrEmpty(p.product_description) && p.product_description.ToLower().Contains(searchTerm));
                    });
                }

                var result = filteredProducts.Select(p =>
                {
                    p.product_status = EvaluateProductStatus(p);
                    return new
                    {
                        product_ID = p.product_ID,
                        formatted_code = GetFormattedCodeForProduct(p),
                        product_name = p.product_name,
                        product_brand = p.product_brand,
                        category_name = _categories.FirstOrDefault(c => c.category_ID == p.category_ID)?.category_name ?? "N/A",
                        product_description = p.product_description,
                        product_quantity = p.product_quantity,
                        product_price = p.Product_price,
                        product_status = p.product_status,
                        reorder_level = p.reorder_level,
                        has_pending_reorder = _reorders.Any(r => r.product_ID == p.product_ID &&
                            (r.reorder_status == "Pending" || r.reorder_status == "Partially Received"))
                    };
                }).ToList();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return Json(new { success = false, message = "Failed to fetch search results." });
            }
        }

        public IActionResult CategoryManagement()
        {
            ViewBag.Products = _products;
            return View(_categories);
        }

        public class DeliveryItemRequest
        {
            public int product_ID { get; set; }
            public int quantity { get; set; }
            public int? reorder_ID { get; set; }
            public List<string>? serialNumbers { get; set; }
            public List<string>? serials { get; set; }
        }

        public class DeliveryCompleteRequest
        {
            public string? received_by { get; set; }
            public string? batch_ID { get; set; }
            public List<DeliveryItemRequest>? items { get; set; }
        }
        [HttpPost]
        public IActionResult CompleteDelivery([FromBody] DeliveryCompleteRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.received_by) || string.IsNullOrWhiteSpace(request.batch_ID) || request.items == null || !request.items.Any())
                {
                    return Json(new { success = false, message = "Invalid delivery payload." });
                }

                foreach (var requestedItem in request.items)
                {
                    if (requestedItem.quantity <= 0)
                        return Json(new { success = false, message = "Every delivery item must have a valid quantity." });

                    if (requestedItem.reorder_ID.HasValue)
                    {
                        var requestedReorder = _reorders.FirstOrDefault(r =>
                            r.reorder_ID == requestedItem.reorder_ID.Value &&
                            r.product_ID == requestedItem.product_ID &&
                            (r.reorder_status == "Pending" || r.reorder_status == "Partially Received"));

                        if (requestedReorder == null)
                            return Json(new { success = false, message = "The selected pending order is no longer active." });

                        int remainingQuantity = requestedReorder.ordered_quantity - requestedReorder.received_quantity;
                        if (requestedItem.quantity > remainingQuantity)
                            return Json(new { success = false, message = $"Received quantity cannot exceed the remaining order quantity of {remainingQuantity}." });
                    }
                }

                // Advance forward sequence tracker permanently
                string todayDateStr = DateTime.Now.ToString("yyyyMMdd");
                if (_lastDeliveryDate != todayDateStr)
                {
                    _lastDeliveryDate = todayDateStr;
                    _globalBatchSequence = 1;
                }
                else
                {
                    _globalBatchSequence++;
                }

                var delivery = new Delivery
                {
                    delivery_ID = _deliveries.Any() ? _deliveries.Max(d => d.delivery_ID) + 1 : 1,
                    date_delivered = DateTime.Now,
                    received_by = request.received_by,
                    batch_ID = request.batch_ID
                };

                _deliveries.Add(delivery);

                foreach (var item in request.items)
                {
                    var product = _products.FirstOrDefault(p => p.product_ID == item.product_ID);
                    if (product == null) continue;

                    var detail = new DeliveryDetails
                    {
                        deldetails_ID = _deliveryDetails.Any() ? _deliveryDetails.Max(dd => dd.deldetails_ID) + 1 : 1,
                        product_quantity = item.quantity,
                        product_ID = item.product_ID,
                        reorder_ID = item.reorder_ID,
                        delivery_ID = delivery.delivery_ID
                    };

                    _deliveryDetails.Add(detail);
                    delivery.DeliveryDetails.Add(detail);
                    product.DeliveryDetails.Add(detail);

                    var sns = item.serialNumbers ?? item.serials;
                    if (sns != null)
                    {
                        foreach (var sn in sns)
                        {
                            if (string.IsNullOrWhiteSpace(sn)) continue;

                            var delSerial = new DelSerial
                            {
                                serial_No = sn,
                                deldetails_ID = detail.deldetails_ID
                            };
                            _delSerials.Add(delSerial);
                            detail.DelSerials.Add(delSerial);

                            var prodSerial = new ProductSerial
                            {
                                serial_No = sn,
                                product_ID = item.product_ID,
                                batch_ID = request.batch_ID
                            };
                            _productSerials.Add(prodSerial);
                            product.ProductSerials.Add(prodSerial);
                        }
                    }

                    product.product_quantity += item.quantity;
                    product.has_received_initial_delivery = true;
                    product.product_status = EvaluateProductStatus(product);

                    var reorder = item.reorder_ID.HasValue
                        ? _reorders.FirstOrDefault(r => r.reorder_ID == item.reorder_ID.Value && r.product_ID == item.product_ID)
                        : _reorders.FirstOrDefault(r => r.product_ID == item.product_ID &&
                            (r.reorder_status == "Pending" || r.reorder_status == "Partially Received"));

                    if (reorder != null && (reorder.reorder_status == "Pending" || reorder.reorder_status == "Partially Received"))
                    {
                        detail.reorder_ID = reorder.reorder_ID;
                        reorder.received_quantity = Math.Min(reorder.ordered_quantity, reorder.received_quantity + item.quantity);
                        if (reorder.received_quantity >= reorder.ordered_quantity)
                        {
                            reorder.reorder_status = "Received";
                            reorder.date_completed = DateTime.Now;
                        }
                        else
                        {
                            reorder.reorder_status = "Partially Received";
                        }
                    }
                }

                var deliveryCode = delivery.batch_ID?.Replace("BATCH-", "DEL-") ?? $"DEL-{todayDateStr}-{_globalBatchSequence:D3}";

                return Json(new
                {
                    success = true,
                    message = "Delivery completed and recorded.",
                    delivery = new
                    {
                        delivery_ID = delivery.delivery_ID,
                        delivery_code = deliveryCode,
                        batch_ID = delivery.batch_ID,
                        date_delivered = delivery.date_delivered,
                        received_by = delivery.received_by
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing delivery.");
                return Json(new { success = false, message = "An error occurred while completing the delivery." });
            }
        }
        // POST: Hard delete a delivery receipt and associated records
        [HttpPost]
        public IActionResult DeleteDeliveryReceipt(int delivery_ID)
        {
            try
            {
                var delivery = _deliveries.FirstOrDefault(d => d.delivery_ID == delivery_ID);
                if (delivery == null) return Json(new { success = false, message = "Delivery not found." });

                var details = _deliveryDetails.Where(dd => dd.delivery_ID == delivery_ID).ToList();
                bool cancelledLinkedReorder = false;

                foreach (var dd in details)
                {
                    var product = _products.FirstOrDefault(p => p.product_ID == dd.product_ID);
                    if (product != null)
                    {
                        // 1. Rollback stock quantity
                        product.product_quantity = Math.Max(0, product.product_quantity - dd.product_quantity);

                        // 2. Remove associated serial numbers from the product
                        _productSerials.RemoveAll(ps => ps.product_ID == product.product_ID && ps.batch_ID == delivery.batch_ID);
                        // Receiving the first delivery permanently activates the item.
                        // Deleting a receipt rolls back stock, but does not make the item
                        // newly created again. A rollback to zero is therefore Out of Stock.
                        product.product_status = EvaluateProductStatus(product);
                    }

                    if (dd.reorder_ID.HasValue)
                    {
                        var reorder = _reorders.FirstOrDefault(r => r.reorder_ID == dd.reorder_ID.Value);
                        if (reorder != null)
                        {
                            reorder.received_quantity = Math.Max(0, reorder.received_quantity - dd.product_quantity);
                            reorder.date_completed = null;
                            // A deleted receipt invalidates its linked reorder transaction.
                            // Never reopen it as Pending, otherwise the same completed order
                            // returns to the receiving queue and creates a reorder loop.
                            reorder.reorder_status = "Cancelled";
                            reorder.date_cancelled = DateTime.Now;
                            cancelledLinkedReorder = true;
                        }
                    }

                    // 3. Clean up delivery serials and delivery detail records
                    _delSerials.RemoveAll(ds => ds.deldetails_ID == dd.deldetails_ID);
                    _deliveryDetails.Remove(dd);
                }

                // 4. Hard delete delivery receipt record from list
               
                _deliveries.Remove(delivery);

                return Json(new
                {
                    success = true,
                    message = cancelledLinkedReorder
                        ? "Delivery receipt deleted, inventory rolled back, and linked reorder cancelled."
                        : "Delivery receipt deleted and inventory rolled back."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting delivery receipt.");
                return Json(new { success = false, message = "An error occurred while deleting the delivery receipt." });
            }
        }
        //Category Creation
        [HttpPost]
        public IActionResult CreateCategory([FromForm] ProductCategory category)
        {
            string normalizedCategoryName = NormalizeProductIdentity(category.category_name);

            if (string.IsNullOrWhiteSpace(normalizedCategoryName))
                return Json(new { success = false, message = "Invalid category name." });

            bool categoryAlreadyExists = _categories.Any(existing =>
                string.Equals(
                    NormalizeProductIdentity(existing.category_name),
                    normalizedCategoryName,
                    StringComparison.OrdinalIgnoreCase));

            if (categoryAlreadyExists)
                return Json(new { success = false, message = "Category already exists" });

            category.category_name = normalizedCategoryName;
            category.category_ID = _categories.Any() ? _categories.Max(c => c.category_ID) + 1 : 1;
            _categories.Add(category);

            return Json(new { success = true, message = "Category created successfully!" });
        }

        [HttpPost]
        public IActionResult EditCategory(int category_ID, string category_name)
        {
            var cat = _categories.FirstOrDefault(c => c.category_ID == category_ID);
            if (cat != null && !string.IsNullOrWhiteSpace(category_name))
            {
                cat.category_name = category_name;
                return Json(new { success = true, message = "Category updated successfully!" });
            }
            return Json(new { success = false, message = "Category not found or invalid input." });
        }
        // GET: Fetch next delivery_ID and batch_ID for the current date
        [HttpGet]
        public IActionResult GetNextDeliveryInfo()
        {
            string todayDateStr = DateTime.Now.ToString("yyyyMMdd");

            // Reset daily sequence counter if the date changes, otherwise peek at next sequence
            int nextSeq = (_lastDeliveryDate == todayDateStr) ? _globalBatchSequence + 1 : 1;

            return Json(new
            {
                success = true,
                delivery_ID = $"DEL-{todayDateStr}-{nextSeq:D3}",
                batch_ID = $"BATCH-{todayDateStr}-{nextSeq:D3}"
            });
        }

        [HttpGet]
        public IActionResult SearchProductForDelivery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new { success = false, message = "Please enter a valid Product ID or Code." });

            Product product = null;

            if (int.TryParse(query, out int parsedId))
            {
                product = _products.FirstOrDefault(p => p.product_ID == parsedId);
            }

            if (product == null)
            {
                product = _products.FirstOrDefault(p =>
                    GetFormattedCodeForProduct(p).Equals(query, StringComparison.OrdinalIgnoreCase));
            }

            if (product == null)
                return Json(new { success = false, message = $"No product found matching '{query}'." });

            var categoryName = _categories.FirstOrDefault(c => c.category_ID == product.category_ID)?.category_name ?? "N/A";

            return Json(new
            {
                success = true,
                product_ID = product.product_ID,
                formatted_code = GetFormattedCodeForProduct(product),
                product_name = product.product_name,
                product_brand = product.product_brand,
                product_description = product.product_description,
                category_name = categoryName,
                current_quantity = product.product_quantity
            });
        }
    }
}
