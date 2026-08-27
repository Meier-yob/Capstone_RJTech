using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Capstone_RJTech.Services;
using Capstone_RJTech.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace Capstone_RJTech.Controllers
{
    public class SalesController : Controller
    {
        private static readonly HashSet<string> PaymentMethods =
            new(StringComparer.OrdinalIgnoreCase) { "Cash", "GCash", "Maya", "Bank Transfer" };

        private readonly ApplicationDbContext _db;
        private readonly ILogger<SalesController> _logger;
        private readonly StockNotificationService _stockNotifications;
        private readonly DocumentSequenceService _documentSequences;

        public SalesController(
            ApplicationDbContext db,
            ILogger<SalesController> logger,
            StockNotificationService stockNotifications,
            DocumentSequenceService documentSequences)
        {
            _db = db;
            _logger = logger;
            _stockNotifications = stockNotifications;
            _documentSequences = documentSequences;
        }

        public IActionResult Index() => RedirectToAction(nameof(SalesOrders));

        public IActionResult Customer()
        {
            var customers = _db.Customers
                .AsNoTracking()
                .OrderBy(customer => customer.customer_FullName)
                .ToList();

            return View(customers);
        }

        public IActionResult SalesOrders()
        {
            var checkouts = _db.Checkouts
                .Include(checkout => checkout.Customer)
                .AsNoTracking()
                .OrderByDescending(checkout => checkout.DatePurchased)
                .ThenByDescending(checkout => checkout.CheckoutID)
                .ToList();

            return View(checkouts);
        }

        public IActionResult Checkout()
            => View(new CheckoutFormViewModel { DatePurchased = DateTime.Now });

        public IActionResult SelectedCheckoutDetails(int id)
        {
            var checkout = _db.Checkouts
                .Include(item => item.Customer)
                .Include(item => item.CheckoutItems)
                    .ThenInclude(item => item.Product)
                .AsNoTracking()
                .FirstOrDefault(item => item.CheckoutID == id);

            if (checkout == null) return NotFound();
            return View(new CheckoutDetailsViewModel { Checkout = checkout });
        }

        [HttpGet]
        public IActionResult SearchCustomers(string? query)
        {
            string term = Normalize(query).ToLowerInvariant();
            var customers = _db.Customers.AsNoTracking().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                customers = customers.Where(customer =>
                    customer.customer_FullName.ToLowerInvariant().Contains(term) ||
                    customer.customer_Email.ToLowerInvariant().Contains(term) ||
                    customer.customer_Phone.ToLowerInvariant().Contains(term) ||
                    customer.customer_Address.ToLowerInvariant().Contains(term));

            }

            var result = customers
                .OrderBy(customer => customer.customer_FullName)
                .Take(8)
                .Select(customer => new
                {
                    customerId = customer.customer_ID,
                    fullName = customer.customer_FullName,
                    email = customer.customer_Email,
                    phone = customer.customer_Phone,
                    address = customer.customer_Address
                });

            return Json(new { success = true, customers = result });
        }

        [HttpGet]
        public IActionResult SearchProducts(string? query)
        {
            string term = Normalize(query).ToLowerInvariant();
            var products = _db.Products
                .Include(product => product.Category)
                .AsNoTracking()
                .AsEnumerable()
                .Select(product => new
                {
                    Product = product,
                    AvailableStock = product.product_quantity
                })
                .Where(item => item.AvailableStock > 0 && item.Product.product_status != "Unavailable");

            if (!string.IsNullOrWhiteSpace(term))
            {
                products = products.Where(item =>
                    item.Product.product_name.ToLowerInvariant().Contains(term) ||
                    item.Product.product_brand.ToLowerInvariant().Contains(term) ||
                    item.Product.formatted_code.ToLowerInvariant().Contains(term) ||
                    (item.Product.Category?.category_name.ToLowerInvariant().Contains(term) ?? false));
            }

            var result = products
                .OrderBy(item => item.Product.product_name)
                .Take(8)
                .Select(item => new
                {
                    productId = item.Product.product_ID,
                    code = item.Product.formatted_code,
                    name = item.Product.product_name,
                    brand = item.Product.product_brand,
                    category = item.Product.Category?.category_name ?? "Uncategorized",
                    stock = item.AvailableStock,
                    price = item.Product.Product_price
                });

            return Json(new { success = true, products = result });
        }

        [HttpPost]
        public IActionResult CompleteCheckout([FromBody] SaveCheckoutRequest? request)
            => request == null
                ? Json(new { success = false, message = "Checkout information is required." })
                : SaveCheckout(request);

        [HttpGet]
        public IActionResult CheckSerialNumber(string? serialNumber)
        {
            string? normalizedSerial = NormalizeSerial(serialNumber);
            if (string.IsNullOrWhiteSpace(normalizedSerial))
                return Json(new { success = true, duplicate = false });

            bool duplicate = _db.CheckoutItems
                .AsNoTracking()
                .Any(item => item.SerialNo == normalizedSerial);

            return Json(new { success = true, duplicate });
        }

        [HttpPost]
        public IActionResult DeleteCheckout(int id)
        {
            using var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                var checkout = _db.Checkouts
                    .Include(item => item.CheckoutItems)
                        .ThenInclude(item => item.Product)
                    .FirstOrDefault(item => item.CheckoutID == id);

                if (checkout == null)
                    return Json(new { success = false, message = "Sales transaction not found." });

                foreach (var item in checkout.CheckoutItems)
                {
                    if (item.Product == null) continue;
                    item.Product.product_quantity += item.ItemQuantity;
                    item.Product.product_status = ProductController.EvaluateProductStatus(item.Product);
                }

                _db.Checkouts.Remove(checkout);
                _db.SaveChanges();
                transaction.Commit();
                SynchronizeStockNotifications();

                return Json(new { success = true, message = "Sales transaction deleted and inventory restored." });
            }
            catch (Exception exception)
            {
                transaction.Rollback();
                _logger.LogError(exception, "Error deleting checkout {CheckoutId}.", id);
                return Json(new { success = false, message = "Unable to delete the sales transaction." });
            }
        }

        public IActionResult Refund() => View();

        private IActionResult SaveCheckout(SaveCheckoutRequest request)
        {
            var validationError = ValidateRequest(request);
            if (validationError != null)
                return Json(new { success = false, message = validationError });

            using var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                int[] productIds = request.Items
                    .Select(item => item.ProductID)
                    .Distinct()
                    .ToArray();

                var products = _db.Products
                    .Where(product => productIds.Contains(product.product_ID))
                    .ToDictionary(product => product.product_ID);

                if (request.Items.Any(item => !products.ContainsKey(item.ProductID)))
                    return Json(new { success = false, message = "One or more selected products no longer exist." });

                var requestedQuantities = request.Items
                    .GroupBy(item => item.ProductID)
                    .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

                foreach (var requested in requestedQuantities)
                {
                    int available = products[requested.Key].product_quantity;
                    if (requested.Value > available)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Only {available} unit(s) of {products[requested.Key].product_name} are available."
                        });
                    }
                }

                string[] serialNumbers = request.Items
                    .SelectMany(item => item.SerialNumbers)
                    .Select(serial => NormalizeSerial(serial)!)
                    .ToArray();

                bool serialExists = _db.CheckoutItems.Any(item =>
                    item.SerialNo != null && serialNumbers.Contains(item.SerialNo));

                if (serialExists)
                    return Json(new { success = false, message = "One or more serial numbers have already been sold." });

                Customer customer = FindOrCreateCustomer(request);

                foreach (var requested in requestedQuantities)
                    products[requested.Key].product_quantity -= requested.Value;

                foreach (var product in products.Values)
                    product.product_status = ProductController.EvaluateProductStatus(product);

                decimal totalAmount = request.Items.Sum(item =>
                    products[item.ProductID].Product_price * item.Quantity);

                var checkout = new Checkout
                {
                    CheckoutNumber = _documentSequences.AllocateNextCheckoutNumber(),
                    DatePurchased = DateTime.Now,
                    Customer = customer,
                    TotalAmount = totalAmount,
                    PaymentMethod = NormalizePaymentMethod(request.PaymentMethod),
                    Status = "Completed"
                };
                _db.Checkouts.Add(checkout);

                checkout.CheckoutItems = request.Items
                    .SelectMany(item => item.SerialNumbers.Select(serial => new CheckoutItem
                    {
                        ProductID = item.ProductID,
                        SerialNo = NormalizeSerial(serial),
                        ItemQuantity = 1,
                        Price = products[item.ProductID].Product_price,
                        SubTotal = products[item.ProductID].Product_price
                    }))
                    .ToList();

                _db.SaveChanges();
                transaction.Commit();
                SynchronizeStockNotifications();

                return Json(new
                {
                    success = true,
                    message = "Sale completed successfully.",
                    redirectUrl = Url.Action(nameof(SelectedCheckoutDetails), new { id = checkout.CheckoutID })
                });
            }
            catch (Exception exception)
            {
                transaction.Rollback();
                _logger.LogError(exception, "Error saving checkout.");
                return Json(new { success = false, message = "Unable to save the sales transaction. No changes were made." });
            }
        }

        private Customer FindOrCreateCustomer(SaveCheckoutRequest request)
        {
            string email = request.CustomerEmail.Trim().ToLowerInvariant();
            Customer? customer = _db.Customers.FirstOrDefault(item => item.customer_Email == email);

            if (customer == null && request.CustomerID.HasValue)
                customer = _db.Customers.Find(request.CustomerID.Value);

            if (customer == null)
            {
                customer = new Customer();
                _db.Customers.Add(customer);
            }

            customer.customer_FullName = Normalize(request.CustomerFullName);
            customer.customer_Email = email;
            customer.customer_Phone = Normalize(request.CustomerPhone);
            customer.customer_Address = Normalize(request.CustomerAddress);
            return customer;
        }

        private string? ValidateRequest(SaveCheckoutRequest request)
        {
            if (string.IsNullOrWhiteSpace(Normalize(request.CustomerFullName)))
                return "Customer full name is required.";
            string email = request.CustomerEmail?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!new EmailAddressAttribute().IsValid(email) ||
                !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@gmail\.com$"))
                return "Enter a valid @gmail.com email address.";
            if (!System.Text.RegularExpressions.Regex.IsMatch(Normalize(request.CustomerPhone), @"^\d{11}$"))
                return "Phone number must contain exactly 11 numeric digits.";
            if (string.IsNullOrWhiteSpace(Normalize(request.CustomerAddress)))
                return "Customer address is required.";
            if (!PaymentMethods.Contains(request.PaymentMethod ?? string.Empty))
                return "Select a valid payment method.";
            if (request.Items == null || request.Items.Count == 0)
                return "Add at least one product to the order.";
            if (request.Items.Any(item => item.Quantity <= 0))
                return "Every item quantity must be at least one.";

            if (request.Items.Any(item =>
                item.SerialNumbers == null ||
                item.SerialNumbers.Count != item.Quantity ||
                item.SerialNumbers.Any(serial => string.IsNullOrWhiteSpace(serial))))
            {
                return "Enter one serial number for every product unit.";
            }

            var serials = request.Items
                .SelectMany(item => item.SerialNumbers)
                .Select(serial => NormalizeSerial(serial)!)
                .ToList();

            if (serials.Count != serials.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                return "Duplicate serial numbers are not allowed.";

            return null;
        }

        private static string Normalize(string? value)
            => string.Join(" ", (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        private static string? NormalizeSerial(string? value)
        {
            string serial = Normalize(value).ToUpperInvariant();
            return string.IsNullOrWhiteSpace(serial) ? null : serial;
        }

        private static string NormalizePaymentMethod(string value)
            => PaymentMethods.First(method => method.Equals(value, StringComparison.OrdinalIgnoreCase));

        private void SynchronizeStockNotifications()
        {
            try
            {
                _stockNotifications.Synchronize();
            }
            catch (Exception exception)
            {
                // The sale is already committed; notification refresh must not report it as failed.
                _logger.LogError(exception, "Unable to refresh stock notifications after a sales change.");
            }
        }
    }
}
