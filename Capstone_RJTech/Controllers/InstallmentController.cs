using Capstone_RJTech.Data;
using Capstone_RJTech.Services;
using Capstone_RJTech.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_RJTech.Controllers
{
    public class InstallmentController : Controller
    {
        private readonly InstallmentService _installments;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<InstallmentController> _logger;

        public InstallmentController(
            InstallmentService installments,
            ApplicationDbContext db,
            ILogger<InstallmentController> logger)
        {
            _installments = installments;
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
            => View(await _installments.GetInstallmentsAsync());

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var model = await _installments.GetInstallmentDetailsAsync(id);
            return model == null ? NotFound() : View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(RecordInstallmentPaymentViewModel request)
        {
            if (!ModelState.IsValid)
            {
                string message = ModelState.Values
                    .SelectMany(value => value.Errors)
                    .Select(error => error.ErrorMessage)
                    .FirstOrDefault() ?? "Enter valid payment information.";
                return Json(new { success = false, message });
            }

            var result = await _installments.RecordPaymentAsync(
                request.InstallmentID,
                request.PaymentAmount,
                request.PaymentMethod);

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                balance = result.Balance,
                redirectUrl = result.Success
                    ? Url.Action(nameof(Details), new { id = result.InstallmentID })
                    : null
            });
        }

        [HttpPost]
        public IActionResult DeleteInstallments([FromBody] int[]? ids)
        {
            int[] selectedIds = ids?
                .Where(id => id > 0)
                .Distinct()
                .ToArray() ?? Array.Empty<int>();

            if (selectedIds.Length == 0)
                return Json(new { success = false, message = "Select at least one installment to delete." });

            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var installments = _db.Installments
                    .Where(installment => selectedIds.Contains(installment.InstallmentID))
                    .ToList();
                if (installments.Count != selectedIds.Length)
                    return Json(new { success = false, message = "One or more selected installments could not be found." });

                var checkoutIds = installments.Select(installment => installment.CheckoutID).ToArray();
                if (_db.CustomerPurchaseHistories.Any(payment => checkoutIds.Contains(payment.CheckoutID)))
                    return Json(new { success = false, message = "These installments have payment history and cannot be deleted." });

                _db.Installments.RemoveRange(installments);
                _db.SaveChanges();
                transaction.Commit();
                return Json(new
                {
                    success = true,
                    message = $"{installments.Count} installment details deleted. Sales records and inventory were not changed."
                });
            }
            catch (Exception exception)
            {
                transaction.Rollback();
                _logger.LogError(exception, "Unable to delete selected installment details.");
                return Json(new { success = false, message = "Unable to delete the selected installment details." });
            }
        }

    }
}
