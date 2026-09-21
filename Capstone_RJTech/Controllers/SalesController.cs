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
            new(StringComparer.OrdinalIgnoreCase) { "Cash", "E-Wallet", "Bank Transfer" };
        private static readonly HashSet<string> PaymentTypes =
            new(StringComparer.OrdinalIgnoreCase) { "Full Payment", "Installment" };

        private readonly ApplicationDbContext _db;
        private readonly ILogger<SalesController> _logger;
        private readonly StockNotificationService _stockNotifications;
        private readonly InstallmentService _installments;

        public SalesController(
            ApplicationDbContext db,
            ILogger<SalesController> logger,
            StockNotificationService stockNotifications,
            InstallmentService installments)
        {
            _db = db;
            _logger = logger;
            _stockNotifications = stockNotifications;
            _installments = installments;
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

        [HttpPost]
        public IActionResult DeleteCustomers([FromBody] int[]? ids)
        {
            int[] selectedIds = ids?
                .Where(id => id > 0)
                .Distinct()
                .ToArray() ?? Array.Empty<int>();

            if (selectedIds.Length == 0)
                return Json(new { success = false, message = "Select at least one customer to delete." });

            try
            {
                if (_db.Checkouts.Any(checkout => selectedIds.Contains(checkout.CustomerID)))
                    return Json(new { success = false, message = "One or more selected customers have sales history and cannot be deleted." });

                var customers = _db.Customers
                    .Where(customer => selectedIds.Contains(customer.customer_ID))
                    .ToList();
                if (customers.Count != selectedIds.Length)
                    return Json(new { success = false, message = "One or more selected customers could not be found." });

                _db.Customers.RemoveRange(customers);
                _db.SaveChanges();
                return Json(new { success = true, message = $"{customers.Count} customers deleted successfully." });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unable to delete selected customers.");
                return Json(new { success = false, message = "Unable to delete the selected customers." });
            }
        }

        public IActionResult SalesOrders()
        {
            _installments.SynchronizeOverdueStatuses();
            var checkouts = _db.Checkouts
                .Include(checkout => checkout.Customer)
                .Include(checkout => checkout.Installment)
                .Where(checkout => checkout.Status != "Cancelled")
                .AsNoTracking()
                .OrderByDescending(checkout => checkout.DatePurchased)
                .ThenByDescending(checkout => checkout.CheckoutID)
                .ToList();

            return View(checkouts);
        }

        public async Task<IActionResult> SalesHistory(CancellationToken cancellationToken)
        {
            var history = await _db.CustomerPurchaseHistories
                .Include(payment => payment.Customer)
                .Include(payment => payment.Checkout)
                .AsNoTracking()
                .OrderByDescending(payment => payment.PurchaseDate)
                .ThenByDescending(payment => payment.HistoryID)
                .ToListAsync(cancellationToken);

            return View(history);
        }

        public IActionResult Checkout()
        {
            var products = _db.Products
                .Include(product => product.Category)
                .AsNoTracking()
                .Where(product => product.product_quantity > 0 && product.product_status != "Unavailable")
                .OrderBy(product => product.product_name)
                .Take(6)
                .ToList();

            var model = new CheckoutFormViewModel
            {
                DatePurchased = DateTime.Now,
                AvailableProducts = products.Select(product => new CheckoutProductOptionViewModel
                {
                    ProductId = product.product_ID,
                    Code = product.formatted_code,
                    Name = product.product_name,
                    Brand = product.product_brand,
                    Category = product.Category?.category_name ?? "Uncategorized",
                    Stock = product.product_quantity,
                    Price = product.Product_price,
                    ImageUrl = Url.Action("Image", "Product", new { id = product.product_ID }) ?? string.Empty,
                    IsSerialized = product.is_serialized
                }).ToList()
            };

            return View(model);
        }

        public IActionResult SelectedCheckoutDetails(int id)
        {
            var checkout = _db.Checkouts
                .Include(item => item.Customer)
                .Include(item => item.CheckoutItems)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(product => product!.Category)
                .Include(item => item.Installment)
                .AsNoTracking()
                .FirstOrDefault(item => item.CheckoutID == id);

            if (checkout == null) return NotFound();
            return View(new CheckoutDetailsViewModel { Checkout = checkout });
        }

        public IActionResult InstallmentDetails(int id)
        {
            int? installmentId = _db.Installments
                .AsNoTracking()
                .Where(item => item.CheckoutID == id)
                .Select(item => (int?)item.InstallmentID)
                .FirstOrDefault();

            return installmentId.HasValue
                ? RedirectToAction("Details", "Installment", new { id = installmentId.Value })
                : NotFound();
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
                .Take(6)
                .Select(item => new
                {
                    productId = item.Product.product_ID,
                    code = item.Product.formatted_code,
                    name = item.Product.product_name,
                    brand = item.Product.product_brand,
                    category = item.Product.Category?.category_name ?? "Uncategorized",
                    stock = item.AvailableStock,
                    price = item.Product.Product_price,
                    imageUrl = Url.Action("Image", "Product", new { id = item.Product.product_ID }),
                    isSerialized = item.Product.is_serialized
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
                .Any(item => item.SerialNo == normalizedSerial &&
                    item.Checkout != null &&
                    (item.Checkout.Status == "Paid" || item.Checkout.Status == "Ongoing"));

            return Json(new { success = true, duplicate });
        }

        [HttpPost]
        public IActionResult RefundCheckout(int id)
            => TransitionCheckout(id, "Refunded");

        [HttpPost]
        public IActionResult DeleteCheckout(int id)
        {
            using var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                var checkout = _db.Checkouts.Find(id);
                if (checkout == null)
                    return Json(new { success = false, message = "Sales details not found." });

                if (_db.CustomerPurchaseHistories.Any(payment => payment.CheckoutID == id))
                    return Json(new { success = false, message = "This checkout has payment history and cannot be deleted." });

                _db.Checkouts.Remove(checkout);
                _db.SaveChanges();
                transaction.Commit();
                return Json(new { success = true, message = "Sales details deleted. Product inventory was not changed." });
            }
            catch (Exception exception)
            {
                transaction.Rollback();
                _logger.LogError(exception, "Unable to delete sales details for checkout {CheckoutId}.", id);
                return Json(new { success = false, message = "Unable to delete the sales details." });
            }
        }

        [HttpPost]
        public IActionResult DeleteCheckouts([FromBody] int[]? ids)
        {
            int[] selectedIds = ids?
                .Where(id => id > 0)
                .Distinct()
                .ToArray() ?? Array.Empty<int>();

            if (selectedIds.Length == 0)
                return Json(new { success = false, message = "Select at least one sales record to delete." });

            using var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                var checkouts = _db.Checkouts
                    .Where(checkout => selectedIds.Contains(checkout.CheckoutID))
                    .ToList();
                if (checkouts.Count != selectedIds.Length)
                    return Json(new { success = false, message = "One or more selected sales records could not be found." });

                if (_db.CustomerPurchaseHistories.Any(payment => selectedIds.Contains(payment.CheckoutID)))
                    return Json(new { success = false, message = "One or more selected checkouts have payment history and cannot be deleted." });

                _db.Checkouts.RemoveRange(checkouts);
                _db.SaveChanges();
                transaction.Commit();
                return Json(new
                {
                    success = true,
                    message = $"{checkouts.Count} sales details deleted. Product inventory was not changed."
                });
            }
            catch (Exception exception)
            {
                transaction.Rollback();
                _logger.LogError(exception, "Unable to delete selected sales details.");
                return Json(new { success = false, message = "Unable to delete the selected sales details." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RecordInstallmentPayment(
            [FromBody] RecordInstallmentPaymentRequest? request)
        {
            if (request == null)
                return Json(new { success = false, message = "Payment information is required." });

            int? installmentId = await _db.Installments
                .AsNoTracking()
                .Where(item => item.CheckoutID == request.CheckoutID)
                .Select(item => (int?)item.InstallmentID)
                .FirstOrDefaultAsync();
            if (!installmentId.HasValue)
                return Json(new { success = false, message = "Installment record not found." });

            var result = await _installments.RecordPaymentAsync(
                installmentId.Value,
                request.PaymentAmount,
                request.PaymentMethod);

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                balance = result.Balance,
                redirectUrl = result.Success
                    ? Url.Action("Details", "Installment", new { id = result.InstallmentID })
                    : null
            });
        }

        public IActionResult Refund() => View();

        private IActionResult SaveCheckout(SaveCheckoutRequest request)
        {
            using var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                int[] productIds = (request.Items ?? new List<CheckoutItemRequest>())
                    .Select(item => item.ProductID)
                    .Distinct()
                    .ToArray();

                var products = _db.Products
                    .Where(product => productIds.Contains(product.product_ID))
                    .ToDictionary(product => product.product_ID);

                if (request.Items == null || request.Items.Count == 0)
                    return Json(new { success = false, message = "Add at least one product to the order." });

                if (request.Items.Any(item => !products.ContainsKey(item.ProductID)))
                    return Json(new { success = false, message = "One or more selected products no longer exist." });

                var validationError = ValidateRequest(request, products);
                if (validationError != null)
                    return Json(new { success = false, message = validationError });

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
                    .Where(item => products[item.ProductID].is_serialized)
                    .SelectMany(item => item.SerialNumbers)
                    .Select(serial => NormalizeSerial(serial)!)
                    .ToArray();

                bool serialExists = _db.CheckoutItems.Any(item =>
                    item.SerialNo != null && serialNumbers.Contains(item.SerialNo) &&
                    item.Checkout != null &&
                    (item.Checkout.Status == "Paid" || item.Checkout.Status == "Ongoing"));

                if (serialExists)
                    return Json(new { success = false, message = "One or more serial numbers have already been sold." });

                Customer customer = FindOrCreateCustomer(request);

                foreach (var requested in requestedQuantities)
                    products[requested.Key].product_quantity -= requested.Value;

                foreach (var product in products.Values)
                    product.product_status = ProductController.EvaluateProductStatus(product);

                decimal totalAmount = request.Items.Sum(item =>
                    products[item.ProductID].Product_price * item.Quantity);
                string paymentType = NormalizePaymentType(request.PaymentType);
                InstallmentCalculation? installmentCalculation = null;
                if (paymentType == "Installment")
                {
                    try
                    {
                        installmentCalculation = _installments.Calculate(
                            totalAmount,
                            request.InstallmentMonths!.Value);
                    }
                    catch (ArgumentException exception)
                    {
                        return Json(new { success = false, message = exception.Message });
                    }
                }

                var checkout = new Checkout
                {
                    DatePurchased = DateTime.Now,
                    Customer = customer,
                    TotalAmount = totalAmount,
                    PaymentType = paymentType,
                    PaymentMethod = NormalizePaymentMethod(request.PaymentMethod),
                    Status = paymentType == "Installment" ? "Ongoing" : "Paid"
                };
                _db.Checkouts.Add(checkout);

                if (installmentCalculation != null)
                {
                    checkout.Installment = new Installment
                    {
                        Months = installmentCalculation.Months,
                        DownPayment = installmentCalculation.DownPayment,
                        InterestRate = installmentCalculation.InterestRate,
                        TotalAmount = installmentCalculation.InstallmentTotal,
                        Balance = installmentCalculation.InstallmentTotal,
                        MonthlyPayment = installmentCalculation.MonthlyPayment,
                        MonthsPaid = 0,
                        MonthsRemaining = installmentCalculation.Months,
                        StartDate = DateTime.Now,
                        Status = "Active"
                    };
                }

                // Save the initial receipt in the same transaction as the checkout.
                decimal receivedAmount = installmentCalculation?.DownPayment ?? totalAmount;
                if (receivedAmount > 0)
                {
                    _db.CustomerPurchaseHistories.Add(new CustomerPurchaseHistory
                    {
                        Checkout = checkout,
                        Customer = customer,
                        PurchaseDate = checkout.DatePurchased,
                        TotalAmount = receivedAmount,
                        PaymentMethod = checkout.PaymentMethod
                    });
                }

                checkout.CheckoutItems = request.Items
                    .SelectMany(item => BuildCheckoutItems(item, products[item.ProductID]))
                    .ToList();

                _db.SaveChanges();
                transaction.Commit();
                SynchronizeStockNotifications();

                return Json(new
                {
                    success = true,
                    message = paymentType == "Installment"
                        ? "Installment created successfully."
                        : "Full payment completed successfully.",
                    redirectUrl = paymentType == "Installment"
                        ? Url.Action("Details", "Installment", new { id = checkout.Installment!.InstallmentID })
                        : Url.Action(nameof(SelectedCheckoutDetails), new { id = checkout.CheckoutID })
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

        private string? ValidateRequest(SaveCheckoutRequest request, Dictionary<int, Product> products)
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
            if (!PaymentTypes.Contains(request.PaymentType ?? string.Empty))
                return "Select a valid payment type.";
            if (!PaymentMethods.Contains(request.PaymentMethod ?? string.Empty))
                return "Select a valid payment method.";
            if (string.Equals(request.PaymentType, "Installment", StringComparison.OrdinalIgnoreCase) &&
                !request.InstallmentMonths.HasValue)
                return "Select an installment term.";
            if (request.Items == null || request.Items.Count == 0)
                return "Add at least one product to the order.";
            if (request.Items.Any(item => item.Quantity <= 0))
                return "Every item quantity must be at least one.";
            if (request.Items.Select(item => item.ProductID).Distinct().Count() != request.Items.Count)
                return "The same product cannot be added more than once.";

            // Serial numbers are only required for serialized products; non-serialized
            // products bypass serial validation entirely.
            var serializedItems = request.Items
                .Where(item => products[item.ProductID].is_serialized)
                .ToList();

            if (serializedItems.Any(item =>
                item.SerialNumbers == null ||
                item.SerialNumbers.Count != item.Quantity ||
                item.SerialNumbers.Any(serial => string.IsNullOrWhiteSpace(serial))))
            {
                return "Enter one serial number for every unit of serialized products.";
            }

            var serials = serializedItems
                .SelectMany(item => item.SerialNumbers)
                .Select(serial => NormalizeSerial(serial)!)
                .ToList();

            if (serials.Count != serials.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                return "Duplicate serial numbers are not allowed.";

            return null;
        }

        private static IEnumerable<CheckoutItem> BuildCheckoutItems(
            CheckoutItemRequest item, Product product)
        {
            if (product.is_serialized)
            {
                return item.SerialNumbers.Select(serial => new CheckoutItem
                {
                    ProductID = item.ProductID,
                    SerialNo = NormalizeSerial(serial),
                    ItemQuantity = 1,
                    Price = product.Product_price,
                    SubTotal = product.Product_price
                });
            }

            // A non-serialized product has no serial numbers: store it as a single
            // line item whose quantity covers every unit.
            return new[]
            {
                new CheckoutItem
                {
                    ProductID = item.ProductID,
                    SerialNo = null,
                    ItemQuantity = item.Quantity,
                    Price = product.Product_price,
                    SubTotal = product.Product_price * item.Quantity
                }
            };
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

        private static string NormalizePaymentType(string value)
            => PaymentTypes.First(type => type.Equals(value, StringComparison.OrdinalIgnoreCase));

        private IActionResult TransitionCheckout(int id, string newStatus)
        {
            using var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                var checkout = _db.Checkouts
                    .Include(item => item.CheckoutItems)
                        .ThenInclude(item => item.Product)
                    .Include(item => item.Installment)
                    .FirstOrDefault(item => item.CheckoutID == id);

                if (checkout == null)
                    return Json(new { success = false, message = "Sales transaction not found." });
                if (checkout.Status is not ("Paid" or "Ongoing"))
                    return Json(new { success = false, message = $"A {checkout.Status.ToLowerInvariant()} transaction cannot be changed." });

                foreach (var item in checkout.CheckoutItems)
                {
                    if (item.Product == null) continue;
                    item.Product.product_quantity += item.ItemQuantity;
                    item.Product.product_status = ProductController.EvaluateProductStatus(item.Product);
                }

                checkout.Status = newStatus;
                if (checkout.Installment != null)
                    checkout.Installment.Status = "Cancelled";

                _db.SaveChanges();
                transaction.Commit();
                SynchronizeStockNotifications();

                return Json(new
                {
                    success = true,
                    message = newStatus == "Refunded"
                        ? "Sale marked as refunded and inventory restored."
                        : "Sale cancelled and inventory restored."
                });
            }
            catch (Exception exception)
            {
                transaction.Rollback();
                _logger.LogError(exception, "Unable to mark checkout {CheckoutId} as {Status}.", id, newStatus);
                return Json(new { success = false, message = "Unable to update the sales transaction." });
            }
        }

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
