using Capstone_RJTech.Models;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_RJTech.Controllers
{
    public class ProductController : Controller
    {
        private readonly ILogger<ProductController> _logger;

        private static List<ProductCategory> Categories => InventoryStore.Categories;
        private static List<Product> Products => InventoryStore.Products;
        private static List<ProductSerial> ProductSerials => InventoryStore.ProductSerials;

        public ProductController(ILogger<ProductController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index() => RedirectToAction(nameof(ProductManagement));

        public IActionResult ProductManagement()
        {
            ViewBag.Categories = Categories;
            foreach (var product in Products)
            {
                product.Category = Categories.FirstOrDefault(category => category.category_ID == product.category_ID);
                product.product_status = EvaluateProductStatus(product);
            }

            return View(Products);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = Categories;
            return View("NewProductView", new Product());
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var product = Products.FirstOrDefault(item => item.product_ID == id);
            if (product == null) return NotFound();

            product.Category = Categories.FirstOrDefault(category => category.category_ID == product.category_ID);
            product.product_status = EvaluateProductStatus(product);
            ViewBag.Serials = ProductSerials.Where(serial => serial.product_ID == id).ToList();
            ViewBag.LastDelivery = InventoryStore.DeliveryDetails
                .Where(detail => detail.product_ID == id)
                .Join(InventoryStore.Deliveries, detail => detail.delivery_ID, delivery => delivery.delivery_ID, (_, delivery) => delivery.date_delivered)
                .OrderByDescending(date => date)
                .FirstOrDefault();
            return View("SelectedProductView", product);
        }

        public IActionResult CategoryManagement()
        {
            ViewBag.Products = Products;
            return View(Categories);
        }

        public static string GetCategoryPrefix(string? categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return "P";
            if (categoryName.StartsWith("Keyboard", StringComparison.OrdinalIgnoreCase)) return "K";
            if (categoryName.StartsWith("Mouse", StringComparison.OrdinalIgnoreCase) || categoryName.StartsWith("Mouses", StringComparison.OrdinalIgnoreCase)) return "M";
            if (categoryName.StartsWith("Headset", StringComparison.OrdinalIgnoreCase)) return "H";
            if (categoryName.StartsWith("Monitor", StringComparison.OrdinalIgnoreCase)) return "Mo";
            return categoryName[..1].ToUpperInvariant();
        }

        public static string FormatProductCode(string? categoryName, int productId)
            => $"{GetCategoryPrefix(categoryName)}-{productId:D3}";

        public static string GetFormattedCodeForProduct(Product? product)
        {
            if (product == null) return "P-000";
            var categoryName = Categories.FirstOrDefault(category => category.category_ID == product.category_ID)?.category_name;
            return FormatProductCode(categoryName, product.product_ID);
        }

        internal static string EvaluateProductStatus(Product product)
        {
            // Unavailable identifies a catalog item that has not been received yet.
            if (product.product_status == "Unavailable") return "Unavailable";
            if (product.product_quantity <= 0) return "Out of Stock";
            if (product.product_quantity <= product.reorder_level) return "Low Stock";
            return "Available";
        }

        private static string NormalizeProductIdentity(string? value)
            => string.Join(" ", (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        [HttpGet]
        public IActionResult GetNextCategoryCode(int categoryId)
        {
            var category = Categories.FirstOrDefault(item => item.category_ID == categoryId);
            if (category == null) return Json(new { success = false, message = "Invalid category." });

            int nextProductId = Products.Any() ? Products.Max(product => product.product_ID) + 1 : 1;
            return Json(new { success = true, formattedCode = FormatProductCode(category.category_name, nextProductId) });
        }

        [HttpPost]
        public IActionResult Create([FromForm] Product product)
        {
            ModelState.Remove("Category");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(value => value.Errors).Select(error => error.ErrorMessage);
                return Json(new { success = false, message = "Validation failed.", errors });
            }

            product.product_name = NormalizeProductIdentity(product.product_name);
            product.product_brand = NormalizeProductIdentity(product.product_brand);
            if (string.IsNullOrWhiteSpace(product.product_name) || string.IsNullOrWhiteSpace(product.product_brand))
                return Json(new { success = false, message = "Validation failed.", errors = new[] { "Product name and brand are required." } });

            bool alreadyExists = Products.Any(existing =>
                existing.category_ID == product.category_ID &&
                string.Equals(NormalizeProductIdentity(existing.product_name), product.product_name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(NormalizeProductIdentity(existing.product_brand), product.product_brand, StringComparison.OrdinalIgnoreCase));
            if (alreadyExists) return Json(new { success = false, message = "Item Already Exists" });

            product.product_ID = Products.Any() ? Products.Max(item => item.product_ID) + 1 : 1;
            // Stock can only be added through the Delivery receiving workflow.
            product.product_quantity = 0;
            product.product_status = "Unavailable";
            Products.Add(product);

            var category = Categories.FirstOrDefault(item => item.category_ID == product.category_ID);
            return Json(new
            {
                success = true,
                message = "Product created successfully!",
                redirectUrl = Url.Action(nameof(Details), new { id = product.product_ID }),
                data = ToProductResponse(product, category)
            });
        }

        [HttpPost]
        [RequestSizeLimit(5_242_880)]
        public async Task<IActionResult> UploadImage(int id, IFormFile? image)
        {
            var product = Products.FirstOrDefault(item => item.product_ID == id);
            if (product == null) return NotFound();
            if (image == null || image.Length == 0)
            {
                TempData["ImageError"] = "Choose an image to upload.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            string[] allowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
            if (!allowedExtensions.Contains(extension) || image.Length > 5_242_880)
            {
                TempData["ImageError"] = "Use a JPG, PNG, or WebP image up to 5 MB.";
                return RedirectToAction(nameof(Details), new { id });
            }

            string uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
            Directory.CreateDirectory(uploadDirectory);
            string fileName = $"product-{id}-{Guid.NewGuid():N}{extension}";
            await using (var stream = System.IO.File.Create(Path.Combine(uploadDirectory, fileName)))
                await image.CopyToAsync(stream);

            product.product_image_path = $"/uploads/products/{fileName}";
            TempData["ImageSuccess"] = "Product image updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        public class ProductCreateRequest
        {
            public int category_ID { get; set; }
            public string? product_name { get; set; }
            public string? product_brand { get; set; }
            public string? product_description { get; set; }
            public int reorder_level { get; set; }
            public decimal Product_price { get; set; }
        }

        public class BulkCreateProductsRequest
        {
            public List<ProductCreateRequest>? products { get; set; }
        }

        [HttpPost]
        public IActionResult BulkCreate([FromBody] BulkCreateProductsRequest request)
        {
            var rows = request?.products;
            if (rows == null || rows.Count == 0)
                return Json(new { success = false, message = "Add at least one product." });
            if (rows.Count > 100)
                return Json(new { success = false, message = "A bulk upload is limited to 100 products." });

            var errors = new List<string>();
            var normalizedRows = new List<(ProductCreateRequest Row, string Name, string Brand)>();
            var batchKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                string name = NormalizeProductIdentity(row.product_name);
                string brand = NormalizeProductIdentity(row.product_brand);
                string label = $"Row {index + 1}";

                if (!Categories.Any(category => category.category_ID == row.category_ID)) errors.Add($"{label}: select a valid category.");
                if (string.IsNullOrWhiteSpace(name)) errors.Add($"{label}: product name is required.");
                if (string.IsNullOrWhiteSpace(brand)) errors.Add($"{label}: brand is required.");
                if (row.Product_price <= 0) errors.Add($"{label}: price must be greater than zero.");
                if (row.reorder_level < 0) errors.Add($"{label}: reorder level cannot be negative.");

                string key = $"{row.category_ID}|{name}|{brand}";
                if (!batchKeys.Add(key)) errors.Add($"{label}: this product is duplicated in the bulk list.");
                if (Products.Any(existing => existing.category_ID == row.category_ID &&
                    string.Equals(NormalizeProductIdentity(existing.product_name), name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(NormalizeProductIdentity(existing.product_brand), brand, StringComparison.OrdinalIgnoreCase)))
                    errors.Add($"{label}: item already exists.");

                normalizedRows.Add((row, name, brand));
            }

            if (errors.Count > 0)
                return Json(new { success = false, message = "Please correct the bulk product list.", errors });

            int nextId = Products.Any() ? Products.Max(product => product.product_ID) + 1 : 1;
            var createdProducts = new List<object>();
            foreach (var entry in normalizedRows)
            {
                var product = new Product
                {
                    product_ID = nextId++,
                    category_ID = entry.Row.category_ID,
                    product_name = entry.Name,
                    product_brand = entry.Brand,
                    product_description = entry.Row.product_description?.Trim(),
                    product_quantity = 0,
                    reorder_level = entry.Row.reorder_level,
                    Product_price = entry.Row.Product_price,
                    product_status = "Unavailable"
                };
                Products.Add(product);
                createdProducts.Add(ToProductResponse(product, Categories.First(category => category.category_ID == product.category_ID)));
            }

            return Json(new { success = true, message = $"{createdProducts.Count} products created successfully.", data = createdProducts });
        }

        [HttpGet]
        public IActionResult GetDetails(int id)
        {
            var product = Products.FirstOrDefault(item => item.product_ID == id);
            if (product == null) return Json(new { success = false, message = "Product not found." });

            product.product_status = EvaluateProductStatus(product);
            var categoryName = Categories.FirstOrDefault(category => category.category_ID == product.category_ID)?.category_name ?? "N/A";
            var serials = ProductSerials.Where(serial => serial.product_ID == id).Select(serial => new { serial.serial_No, serial.batch_ID }).ToList();
            return Json(new
            {
                success = true,
                product_ID = product.product_ID,
                formatted_code = GetFormattedCodeForProduct(product),
                product_name = product.product_name,
                product_brand = product.product_brand,
                category_name = categoryName,
                product_quantity = product.product_quantity,
                reorder_level = product.reorder_level,
                product_price = product.Product_price,
                product_description = product.product_description,
                product_status = product.product_status,
                serials
            });
        }

        [HttpPost]
        public IActionResult UpdateProductDetails(int product_ID, string product_name, string product_brand, decimal product_price, string product_description, string product_status, int reorder_level)
        {
            try
            {
                var product = Products.FirstOrDefault(item => item.product_ID == product_ID);
                if (product == null) return Json(new { success = false, message = "Product not found." });

                string normalizedName = NormalizeProductIdentity(product_name);
                string normalizedBrand = NormalizeProductIdentity(product_brand);
                if (string.IsNullOrWhiteSpace(normalizedName) || string.IsNullOrWhiteSpace(normalizedBrand))
                    return Json(new { success = false, message = "Product name and brand are required." });

                bool alreadyExists = Products.Any(existing => existing.product_ID != product_ID &&
                    existing.category_ID == product.category_ID &&
                    string.Equals(NormalizeProductIdentity(existing.product_name), normalizedName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(NormalizeProductIdentity(existing.product_brand), normalizedBrand, StringComparison.OrdinalIgnoreCase));
                if (alreadyExists) return Json(new { success = false, message = "Item Already Exists" });

                product.product_name = normalizedName;
                product.product_brand = normalizedBrand;
                product.Product_price = product_price;
                product.product_description = product_description?.Trim();
                product.reorder_level = Math.Max(0, reorder_level);
                product.product_status = EvaluateProductStatus(product);
                return Json(new { success = true, message = "Product updated successfully!" });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error updating product details.");
                return Json(new { success = false, message = "An error occurred while updating the product." });
            }
        }

        [HttpPost]
        public IActionResult DeleteProduct(int id)
        {
            try
            {
                var product = Products.FirstOrDefault(item => item.product_ID == id);
                if (product == null) return Json(new { success = false, message = "Product not found." });
                ProductSerials.RemoveAll(serial => serial.product_ID == id);
                Products.Remove(product);
                return Json(new { success = true, message = "Product deleted successfully!" });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error deleting product.");
                return Json(new { success = false, message = "An error occurred while deleting the product." });
            }
        }

        [HttpGet]
        public IActionResult SearchProducts(string query)
        {
            try
            {
                var filteredProducts = Products.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(query))
                {
                    string term = query.Trim().ToLowerInvariant();
                    filteredProducts = filteredProducts.Where(product =>
                    {
                        string categoryName = Categories.FirstOrDefault(category => category.category_ID == product.category_ID)?.category_name ?? string.Empty;
                        return GetFormattedCodeForProduct(product).ToLowerInvariant().Contains(term) ||
                               product.product_name.ToLowerInvariant().Contains(term) ||
                               product.product_brand.ToLowerInvariant().Contains(term) ||
                               categoryName.ToLowerInvariant().Contains(term) ||
                               (product.product_description?.ToLowerInvariant().Contains(term) ?? false);
                    });
                }

                var result = filteredProducts.Select(product =>
                {
                    product.product_status = EvaluateProductStatus(product);
                    return ToProductResponse(product, Categories.FirstOrDefault(category => category.category_ID == product.category_ID));
                }).ToList();
                return Json(new { success = true, data = result });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error searching products.");
                return Json(new { success = false, message = "Failed to fetch search results." });
            }
        }

        [HttpPost]
        public IActionResult CreateCategory([FromForm] ProductCategory category)
        {
            string normalizedName = NormalizeProductIdentity(category.category_name);
            if (string.IsNullOrWhiteSpace(normalizedName))
                return Json(new { success = false, message = "Invalid category name." });
            if (Categories.Any(existing => string.Equals(NormalizeProductIdentity(existing.category_name), normalizedName, StringComparison.OrdinalIgnoreCase)))
                return Json(new { success = false, message = "Category already exists" });

            category.category_name = normalizedName;
            category.category_ID = Categories.Any() ? Categories.Max(item => item.category_ID) + 1 : 1;
            Categories.Add(category);
            return Json(new { success = true, message = "Category created successfully!" });
        }

        [HttpPost]
        public IActionResult EditCategory(int category_ID, string category_name)
        {
            var category = Categories.FirstOrDefault(item => item.category_ID == category_ID);
            if (category == null || string.IsNullOrWhiteSpace(category_name))
                return Json(new { success = false, message = "Category not found or invalid input." });

            category.category_name = NormalizeProductIdentity(category_name);
            return Json(new { success = true, message = "Category updated successfully!" });
        }

        private static object ToProductResponse(Product product, ProductCategory? category) => new
        {
            product_ID = product.product_ID,
            formatted_code = FormatProductCode(category?.category_name, product.product_ID),
            product_name = product.product_name,
            product_brand = product.product_brand,
            category_name = category?.category_name ?? "N/A",
            product_description = product.product_description,
            product_quantity = product.product_quantity,
            reorder_level = product.reorder_level,
            product_price = product.Product_price,
            product_status = product.product_status
        };
    }
}
