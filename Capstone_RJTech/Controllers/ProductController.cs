using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone_RJTech.Controllers
{
    public class ProductController : Controller
    {
        private const long MaxProductImageBytes = 5 * 1024 * 1024;
        private const long ProductImageRequestLimitBytes = 7 * 1024 * 1024;

        private readonly ApplicationDbContext _db;
        private readonly ILogger<ProductController> _logger;

        public ProductController(ApplicationDbContext db, ILogger<ProductController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public IActionResult Index() => RedirectToAction(nameof(ProductManagement));

        public IActionResult ProductManagement()
        {
            var categories = _db.ProductCategories.OrderBy(category => category.category_name).ToList();
            var products = _db.Products.Include(product => product.Category).ToList();

            bool statusChanged = false;
            foreach (var product in products)
            {
                string status = EvaluateProductStatus(product);
                if (product.product_status != status)
                {
                    product.product_status = status;
                    statusChanged = true;
                }
            }
            if (statusChanged) _db.SaveChanges();

            ViewBag.Categories = categories;
            return View(products);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = _db.ProductCategories.OrderBy(category => category.category_name).ToList();
            return View("NewProductView", new Product());
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var product = _db.Products
                .Include(item => item.Category)
                .FirstOrDefault(item => item.product_ID == id);
            if (product == null) return NotFound();

            product.product_status = EvaluateProductStatus(product);
            _db.SaveChanges();

            var latestDelivery = _db.DeliveryDetails
                .Where(detail => detail.product_ID == id)
                .Select(detail => detail.Delivery)
                .Where(delivery => delivery != null)
                .OrderByDescending(delivery => delivery!.date_delivered)
                .ThenByDescending(delivery => delivery!.delivery_ID)
                .FirstOrDefault();

            ViewBag.LastDelivery = latestDelivery?.date_delivered;
            ViewBag.LatestBatchId = latestDelivery?.batch_ID;
            return View("SelectedProductView", product);
        }

        public IActionResult CategoryManagement()
        {
            ViewBag.Products = _db.Products.AsNoTracking().ToList();
            return View(_db.ProductCategories.AsNoTracking().OrderBy(category => category.category_name).ToList());
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
            => product == null ? "P-000" : FormatProductCode(product.Category?.category_name, product.product_ID);

        internal static string EvaluateProductStatus(Product product)
        {
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
            var category = _db.ProductCategories.AsNoTracking().FirstOrDefault(item => item.category_ID == categoryId);
            if (category == null) return Json(new { success = false, message = "Invalid category." });

            int nextProductId = (_db.Products.Max(product => (int?)product.product_ID) ?? 0) + 1;
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

            bool categoryExists = _db.ProductCategories.Any(category => category.category_ID == product.category_ID);
            if (!categoryExists) return Json(new { success = false, message = "Select a valid category." });

            bool alreadyExists = _db.Products.Any(existing =>
                existing.category_ID == product.category_ID &&
                existing.product_name == product.product_name &&
                existing.product_brand == product.product_brand);
            if (alreadyExists) return Json(new { success = false, message = "Item Already Exists" });

            product.product_ID = 0;
            product.product_quantity = 0;
            product.product_status = "Unavailable";
            _db.Products.Add(product);
            _db.SaveChanges();

            var category = _db.ProductCategories.AsNoTracking().First(item => item.category_ID == product.category_ID);
            return Json(new
            {
                success = true,
                message = "Product created successfully!",
                redirectUrl = Url.Action(nameof(Details), new { id = product.product_ID }),
                data = ToProductResponse(product, category)
            });
        }

        [HttpPost]
        [RequestSizeLimit(ProductImageRequestLimitBytes)]
        public async Task<IActionResult> UploadImage(int id, IFormFile? image)
        {
            try
            {
                var product = await _db.Products.FindAsync(id);
                if (product == null)
                {
                    TempData["ImageError"] = "Product not found.";
                    return RedirectToAction(nameof(ProductManagement));
                }

                var imageResult = await ReadProductImageAsync(image, required: true);
                if (imageResult.Error != null)
                {
                    TempData["ImageError"] = imageResult.Error;
                    return RedirectToAction(nameof(Details), new { id });
                }

                product.product_Image = imageResult.Data;
                product.product_ImageContentType = imageResult.ContentType;
                await _db.SaveChangesAsync();
                TempData["ImageSuccess"] = "Product image updated.";
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unable to store the image for product {ProductId}.", id);
                TempData["ImageError"] = "The image could not be uploaded. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Image(int id)
        {
            var image = await _db.Products
                .AsNoTracking()
                .Where(product => product.product_ID == id)
                .Select(product => new
                {
                    product.product_Image,
                    product.product_ImageContentType
                })
                .FirstOrDefaultAsync();

            if (image?.product_Image == null || image.product_Image.Length == 0)
                return NotFound();

            return File(
                image.product_Image,
                image.product_ImageContentType ?? "application/octet-stream");
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

            var categories = _db.ProductCategories.AsNoTracking().ToDictionary(category => category.category_ID);
            var existingProducts = _db.Products.AsNoTracking().ToList();
            var errors = new List<string>();
            var normalizedRows = new List<(ProductCreateRequest Row, string Name, string Brand)>();
            var batchKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                string name = NormalizeProductIdentity(row.product_name);
                string brand = NormalizeProductIdentity(row.product_brand);
                string label = $"Row {index + 1}";

                if (!categories.ContainsKey(row.category_ID)) errors.Add($"{label}: select a valid category.");
                if (string.IsNullOrWhiteSpace(name)) errors.Add($"{label}: product name is required.");
                if (string.IsNullOrWhiteSpace(brand)) errors.Add($"{label}: brand is required.");
                if (row.Product_price <= 0) errors.Add($"{label}: price must be greater than zero.");
                if (row.reorder_level < 0) errors.Add($"{label}: reorder level cannot be negative.");

                string key = $"{row.category_ID}|{name}|{brand}";
                if (!batchKeys.Add(key)) errors.Add($"{label}: this product is duplicated in the bulk list.");
                if (existingProducts.Any(existing => existing.category_ID == row.category_ID &&
                    string.Equals(NormalizeProductIdentity(existing.product_name), name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(NormalizeProductIdentity(existing.product_brand), brand, StringComparison.OrdinalIgnoreCase)))
                    errors.Add($"{label}: item already exists.");

                normalizedRows.Add((row, name, brand));
            }

            if (errors.Count > 0)
                return Json(new { success = false, message = "Please correct the bulk product list.", errors });

            var products = normalizedRows.Select(entry => new Product
            {
                category_ID = entry.Row.category_ID,
                product_name = entry.Name,
                product_brand = entry.Brand,
                product_description = entry.Row.product_description?.Trim(),
                product_quantity = 0,
                reorder_level = entry.Row.reorder_level,
                Product_price = entry.Row.Product_price,
                product_status = "Unavailable"
            }).ToList();

            _db.Products.AddRange(products);
            _db.SaveChanges();

            var createdProducts = products
                .Select(product => ToProductResponse(product, categories[product.category_ID]))
                .ToList();
            return Json(new { success = true, message = $"{createdProducts.Count} products created successfully.", data = createdProducts });
        }

        [HttpGet]
        public IActionResult GetDetails(int id)
        {
            var product = _db.Products.Include(item => item.Category).FirstOrDefault(item => item.product_ID == id);
            if (product == null) return Json(new { success = false, message = "Product not found." });

            product.product_status = EvaluateProductStatus(product);
            _db.SaveChanges();
            return Json(new
            {
                success = true,
                product_ID = product.product_ID,
                formatted_code = GetFormattedCodeForProduct(product),
                product_name = product.product_name,
                product_brand = product.product_brand,
                category_name = product.Category?.category_name ?? "N/A",
                product_quantity = product.product_quantity,
                reorder_level = product.reorder_level,
                product_price = product.Product_price,
                product_description = product.product_description,
                product_status = product.product_status
            });
        }

        [HttpPost]
        public IActionResult UpdateProductDetails(int product_ID, string product_name, string product_brand, decimal product_price, string product_description, string product_status, int reorder_level)
        {
            try
            {
                var product = _db.Products.FirstOrDefault(item => item.product_ID == product_ID);
                if (product == null) return Json(new { success = false, message = "Product not found." });

                string normalizedName = NormalizeProductIdentity(product_name);
                string normalizedBrand = NormalizeProductIdentity(product_brand);
                if (string.IsNullOrWhiteSpace(normalizedName) || string.IsNullOrWhiteSpace(normalizedBrand))
                    return Json(new { success = false, message = "Product name and brand are required." });

                bool alreadyExists = _db.Products.Any(existing => existing.product_ID != product_ID &&
                    existing.category_ID == product.category_ID &&
                    existing.product_name == normalizedName &&
                    existing.product_brand == normalizedBrand);
                if (alreadyExists) return Json(new { success = false, message = "Item Already Exists" });

                product.product_name = normalizedName;
                product.product_brand = normalizedBrand;
                product.Product_price = product_price;
                product.product_description = product_description?.Trim();
                product.reorder_level = Math.Max(0, reorder_level);
                product.product_status = EvaluateProductStatus(product);
                _db.SaveChanges();
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
                var product = _db.Products.Find(id);
                if (product == null) return Json(new { success = false, message = "Product not found." });
                if (_db.DeliveryDetails.Any(detail => detail.product_ID == id))
                    return Json(new { success = false, message = "Products with delivery history cannot be deleted." });
                if (_db.CheckoutItems.Any(item => item.ProductID == id))
                    return Json(new { success = false, message = "Products with sales history cannot be deleted." });

                _db.Products.Remove(product);
                _db.SaveChanges();
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
                var filteredProducts = _db.Products.Include(product => product.Category).AsNoTracking().AsEnumerable();
                if (!string.IsNullOrWhiteSpace(query))
                {
                    string term = query.Trim().ToLowerInvariant();
                    filteredProducts = filteredProducts.Where(product =>
                        GetFormattedCodeForProduct(product).ToLowerInvariant().Contains(term) ||
                        product.product_name.ToLowerInvariant().Contains(term) ||
                        product.product_brand.ToLowerInvariant().Contains(term) ||
                        (product.Category?.category_name.ToLowerInvariant().Contains(term) ?? false) ||
                        (product.product_description?.ToLowerInvariant().Contains(term) ?? false));
                }

                var result = filteredProducts.Select(product =>
                {
                    product.product_status = EvaluateProductStatus(product);
                    return ToProductResponse(product, product.Category);
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
            if (_db.ProductCategories.Any(existing => existing.category_name == normalizedName))
                return Json(new { success = false, message = "Category already exists" });

            category.category_ID = 0;
            category.category_name = normalizedName;
            _db.ProductCategories.Add(category);
            _db.SaveChanges();
            return Json(new { success = true, message = "Category created successfully!" });
        }

        [HttpPost]
        public IActionResult EditCategory(int category_ID, string category_name)
        {
            var category = _db.ProductCategories.Find(category_ID);
            string normalizedName = NormalizeProductIdentity(category_name);
            if (category == null || string.IsNullOrWhiteSpace(normalizedName))
                return Json(new { success = false, message = "Category not found or invalid input." });
            if (_db.ProductCategories.Any(existing => existing.category_ID != category_ID && existing.category_name == normalizedName))
                return Json(new { success = false, message = "Category already exists" });

            category.category_name = normalizedName;
            _db.SaveChanges();
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

        private static async Task<(byte[]? Data, string? ContentType, string? Error)> ReadProductImageAsync(
            IFormFile? image,
            bool required)
        {
            if (image == null || image.Length == 0)
            {
                return required
                    ? (null, null, "Choose an image to upload.")
                    : (null, null, null);
            }

            if (image.Length > MaxProductImageBytes)
                return (null, null, "Use a JPG, PNG, or WebP image up to 5 MB.");

            await using var imageStream = new MemoryStream((int)image.Length);
            await image.CopyToAsync(imageStream);
            byte[] imageBytes = imageStream.ToArray();
            string? contentType = DetectImageContentType(imageBytes);

            return contentType == null
                ? (null, null, "The selected file is not a valid JPG, PNG, or WebP image.")
                : (imageBytes, contentType, null);
        }

        private static string? DetectImageContentType(ReadOnlySpan<byte> imageBytes)
        {
            if (imageBytes.Length >= 3 &&
                imageBytes[0] == 0xFF && imageBytes[1] == 0xD8 && imageBytes[2] == 0xFF)
                return "image/jpeg";

            if (imageBytes.Length >= 8 &&
                imageBytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
                return "image/png";

            if (imageBytes.Length >= 12 &&
                imageBytes[..4].SequenceEqual("RIFF"u8) &&
                imageBytes.Slice(8, 4).SequenceEqual("WEBP"u8))
                return "image/webp";

            return null;
        }
    }
}
