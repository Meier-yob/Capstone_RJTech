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
            new Product { product_ID = 1, product_brand = "A4 Tech", product_description = "Optical Wired Mouse", product_quantity = 0, Product_price = 200.00M, product_status = "Pending Delivery", reorder_level = 5, category_ID = 2 },
            new Product { product_ID = 2, product_brand = "Logitech", product_description = "Mechanical Keyboard", product_quantity = 0, Product_price = 1200.00M, product_status = "Pending Delivery", reorder_level = 5, category_ID = 3 }
        };

        // In-memory delivery & serial trackers
        private static List<Delivery> _deliveries = new List<Delivery>();
        private static List<DeliveryDetails> _deliveryDetails = new List<DeliveryDetails>();
        private static List<ProductSerial> _productSerials = new List<ProductSerial>();
        private static List<DelSerial> _delSerials = new List<DelSerial>();

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

        public IActionResult Dashboard() => View();
        public IActionResult Privacy() => View();
        public IActionResult DeliveryManagement()
        {
            ViewBag.Categories = _categories;
            return View();
        }

        public IActionResult ProductManagement()
        {
            ViewBag.Categories = _categories;

            foreach (var prod in _products)
            {
                prod.Category = _categories.FirstOrDefault(c => c.category_ID == prod.category_ID);
                prod.product_status = EvaluateProductStatus(prod);
            }

            return View(_products);
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
            // The Category navigation property is not submitted from the form and
            // may cause ModelState validation to fail. Also product_quantity is
            // set server-side to 0 for new items so remove it from model state
            // to avoid a required/conversion error when the readonly input is
            // empty in the submitted form.
            ModelState.Remove("Category");
            ModelState.Remove("product_quantity");

            if (ModelState.IsValid)
            {
                product.product_ID = _products.Any() ? _products.Max(p => p.product_ID) + 1 : 1;
                product.product_quantity = 0;
                product.product_status = "Pending Delivery";
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
        public IActionResult UpdateProductDetails(int product_ID, string product_brand, decimal product_price, string product_description, string product_status, int reorder_level)
        {
            try
            {
                var product = _products.FirstOrDefault(p => p.product_ID == product_ID);

                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                product.product_brand = product_brand;
                product.Product_price = product_price;
                product.product_description = product_description;
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

            bool hasBeenDeliveredBefore = _deliveryDetails.Any(dd => dd.product_ID == product.product_ID);

            if (product.product_quantity == 0)
            {
                if (!hasBeenDeliveredBefore) return "Pending Delivery";
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
                        product_brand = p.product_brand,
                        category_name = _categories.FirstOrDefault(c => c.category_ID == p.category_ID)?.category_name ?? "N/A",
                        product_description = p.product_description,
                        product_quantity = p.product_quantity,
                        product_price = p.Product_price,
                        product_status = p.product_status,
                        reorder_level = p.reorder_level
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
                    product.product_status = EvaluateProductStatus(product);
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

                foreach (var dd in details)
                {
                    var product = _products.FirstOrDefault(p => p.product_ID == dd.product_ID);
                    if (product != null)
                    {
                        // 1. Rollback stock quantity
                        product.product_quantity = Math.Max(0, product.product_quantity - dd.product_quantity);

                        // 2. Remove associated serial numbers from the product
                        _productSerials.RemoveAll(ps => ps.product_ID == product.product_ID && ps.batch_ID == delivery.batch_ID);
                        product.product_status = EvaluateProductStatus(product);
                    }

                    // 3. Clean up delivery serials and delivery detail records
                    _delSerials.RemoveAll(ds => ds.deldetails_ID == dd.deldetails_ID);
                    _deliveryDetails.Remove(dd);
                }

                // 4. Hard delete delivery receipt record from list
                // NOTE: _globalBatchSequence is NOT modified here, ensuring sequence numbers never go backward!
                _deliveries.Remove(delivery);

                return Json(new { success = true, message = "Delivery receipt permanently deleted and inventory rolled back." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting delivery receipt.");
                return Json(new { success = false, message = "An error occurred while deleting the delivery receipt." });
            }
        }
        [HttpPost]
        public IActionResult CreateCategory([FromForm] ProductCategory category)
        {
            if (!string.IsNullOrWhiteSpace(category.category_name))
            {
                category.category_ID = _categories.Any() ? _categories.Max(c => c.category_ID) + 1 : 1;
                _categories.Add(category);
                return Json(new { success = true, message = "Category created successfully!" });
            }
            return Json(new { success = false, message = "Invalid category name." });
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
                product_brand = product.product_brand,
                product_description = product.product_description,
                category_name = categoryName,
                current_quantity = product.product_quantity
            });
        }
    }
}
