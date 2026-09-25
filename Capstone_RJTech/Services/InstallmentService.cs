using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Capstone_RJTech.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Capstone_RJTech.Services
{
    public sealed record InstallmentCalculation(
        decimal OriginalAmount,
        decimal DownPayment,
        decimal RemainingPrincipal,
        decimal InterestRate,
        decimal InterestAmount,
        decimal InstallmentTotal,
        decimal MonthlyPayment,
        int Months);

    public sealed record InstallmentPaymentResult(
        bool Success,
        string Message,
        int? InstallmentID = null,
        int? CheckoutID = null,
        decimal? Balance = null,
        int? PaymentID = null,
        string? InstallmentCode = null,
        decimal? PaymentAmount = null,
        bool Completed = false,
        int MonthsPaid = 0,
        int MonthsRemaining = 0,
        string? Status = null,
        DateTime? NextDue = null,
        decimal ProgressPercentage = 0);

    public class InstallmentService
    {
        public static readonly int[] AllowedTerms = [6, 12, 18, 24];
        public static readonly string[] PaymentMethods = ["Cash", "E-Wallet", "Bank Transfer"];
        public const decimal FixedDownPaymentRate = 0.20m;
        public const decimal FixedMonthlyInterestRate = 5m;

        private readonly ApplicationDbContext _db;
        private readonly ILogger<InstallmentService> _logger;

        public InstallmentService(ApplicationDbContext db, ILogger<InstallmentService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public InstallmentCalculation Calculate(
            decimal originalAmount,
            int months)
            => Calculate(originalAmount, Money(originalAmount * FixedDownPaymentRate), months);

        private InstallmentCalculation Calculate(
            decimal originalAmount,
            decimal downPayment,
            int months)
        {
            if (originalAmount <= 0)
                throw new ArgumentException("The checkout total must be greater than zero.");
            if (!AllowedTerms.Contains(months))
                throw new ArgumentException("Select a valid installment term: 6, 12, 18, or 24 months.");
            if (downPayment < 0 || downPayment >= originalAmount)
                throw new ArgumentException("Down payment must be zero or greater and less than the checkout total.");
            decimal interestRate = FixedMonthlyInterestRate;
            decimal remainingPrincipal = Money(originalAmount - downPayment);
            decimal interestAmount = Money(remainingPrincipal * (interestRate / 100m) * months);
            decimal installmentTotal = Money(remainingPrincipal + interestAmount);
            decimal monthlyPayment = Money(installmentTotal / months);

            return new InstallmentCalculation(
                Money(originalAmount), Money(downPayment), remainingPrincipal, interestRate,
                interestAmount, installmentTotal, monthlyPayment, months);
        }

        public async Task<InstallmentManagementViewModel> GetInstallmentsAsync()
        {
            await UpdateInstallmentStatusesAsync();

            var installments = await InstallmentQuery()
                .Where(item => item.Status != "Cancelled")
                .AsNoTracking()
                .OrderByDescending(item => item.InstallmentID)
                .ToListAsync();

            var items = installments.Select(ToListItem).ToList();
            return new InstallmentManagementViewModel
            {
                Installments = items
            };
        }

        public async Task<InstallmentDetailsViewModel?> GetInstallmentDetailsAsync(int installmentId)
        {
            await UpdateInstallmentStatusesAsync();

            var installment = await InstallmentQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.InstallmentID == installmentId);
            if (installment?.Checkout == null) return null;

            decimal totalPaid = CalculateTotalPaid(installment);
            return new InstallmentDetailsViewModel
            {
                Installment = installment,
                Checkout = installment.Checkout,
                Customer = installment.Checkout.Customer,
                TotalPaid = totalPaid,
                ProgressPercentage = CalculateProgressPercentage(installment.MonthsPaid, installment.Months),
                CompletionDate = CalculateCompletionDate(installment),
                DisplayDate = CalculateDisplayDate(installment)
            };
        }

        public async Task<InstallmentPaymentResult> RecordPaymentAsync(
            int installmentId,
            decimal paymentAmount,
            string paymentMethod)
        {
            string? normalizedMethod = PaymentMethods.FirstOrDefault(method =>
                method.Equals(paymentMethod?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (normalizedMethod == null)
                return new(false, "Select a valid payment method.");

            if (paymentAmount <= 0)
                return new(false, "Payment amount must be greater than zero.");
            if (Math.Round(paymentAmount, 2) != paymentAmount)
                return new(false, "Payment amount can have at most two decimal places.");

            paymentAmount = Money(paymentAmount);

            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var installment = await InstallmentQuery()
                    .FirstOrDefaultAsync(item => item.InstallmentID == installmentId);

                if (installment == null)
                    return new(false, "Installment record not found.");
                if (installment.Status is "Completed" or "Cancelled" ||
                    installment.Checkout?.Status is "Paid" or "Cancelled" or "Refunded")
                    return new(false, "This installment no longer accepts payments.");
                if (paymentAmount > installment.Balance)
                    return new(false, $"Payment cannot exceed the remaining balance of ₱{installment.Balance:N2}.");

                decimal newBalance = Money(installment.Balance - paymentAmount);
                if (newBalance < 0)
                    return new(false, "Payment cannot make the installment balance negative.");

                // Double-submit guard: an identical payment on the same installment within the
                // last few seconds is treated as a replay of a submission that already went
                // through, so a double-click or a retry can never record the payment twice.
                const int duplicateWindowSeconds = 10;
                InstallmentPayment? duplicate = await _db.InstallmentPayments
                    .FirstOrDefaultAsync(payment =>
                        payment.InstallmentID == installment.InstallmentID &&
                        payment.Status == "Paid" &&
                        payment.PaymentAmount == paymentAmount &&
                        payment.PaymentDate >= DateTime.Now.AddSeconds(-duplicateWindowSeconds));
                if (duplicate != null)
                    return BuildPaymentResult(installment, paymentAmount, duplicate.PaymentID);

                DateTime paymentDate = DateTime.Now;
                decimal totalPaid = Money(CalculateTotalPaid(installment) + paymentAmount);
                bool completed = newBalance == 0;

                var payment = new InstallmentPayment
                {
                    Installment = installment,
                    PaymentMethod = normalizedMethod,
                    PaymentAmount = paymentAmount,
                    PaymentDate = paymentDate,
                    Status = "Paid"
                };
                _db.InstallmentPayments.Add(payment);

                // Each successful payment produces exactly one receipt, including the final payment.
                // Both records and the balance update are committed or rolled back together.
                _db.CustomerPurchaseHistories.Add(new CustomerPurchaseHistory
                {
                    CheckoutID = installment.CheckoutID,
                    CustomerID = installment.Checkout!.CustomerID,
                    PurchaseDate = paymentDate,
                    TotalAmount = paymentAmount,
                    PaymentMethod = normalizedMethod
                });

                installment.Balance = completed ? 0 : newBalance;
                installment.MonthsPaid = completed
                    ? installment.Months
                    : CalculateMonthsPaid(totalPaid, installment.MonthlyPayment, installment.Months);
                installment.MonthsRemaining = completed ? 0 : installment.Months - installment.MonthsPaid;
                installment.Status = completed
                    ? "Completed"
                    : DetermineInstallmentStatus(installment, totalPaid, paymentDate);

                if (installment.Checkout != null)
                    installment.Checkout.Status = completed ? "Paid" : "Ongoing";

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return BuildPaymentResult(installment, paymentAmount, payment.PaymentID);
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync();
                _logger.LogError(exception, "Unable to record payment for installment {InstallmentId}.", installmentId);
                return new(false, "Unable to record the installment payment. No changes were made.");
            }
        }

        /// <summary>
        /// Composes the post-payment result used to refresh the list row in place. The installment
        /// must already reflect the payment (either just committed or replayed from the duplicate
        /// guard above).
        /// </summary>
        private InstallmentPaymentResult BuildPaymentResult(
            Installment installment,
            decimal paymentAmount,
            int paymentID)
        {
            bool completed = installment.Balance <= 0;
            string message = completed
                ? "Installment completed and checkout marked as paid."
                : "Payment recorded successfully.";
            return new InstallmentPaymentResult(
                true,
                message,
                installment.InstallmentID,
                installment.CheckoutID,
                installment.Balance,
                paymentID,
                $"INS-{installment.InstallmentID:D3}",
                paymentAmount,
                completed,
                installment.MonthsPaid,
                installment.MonthsRemaining,
                installment.Status,
                CalculateDisplayDate(installment),
                CalculateProgressPercentage(installment.MonthsPaid, installment.Months));
        }

        public int CalculateMonthsPaid(decimal totalInstallmentPayments, decimal monthlyPayment, int months)
        {
            if (monthlyPayment <= 0 || months <= 0) return 0;
            int paid = (int)Math.Floor(totalInstallmentPayments / monthlyPayment);
            return Math.Clamp(paid, 0, months);
        }

        public DateTime? CalculateDisplayDate(Installment installment)
        {
            if (installment.Status == "Completed")
                return CalculateCompletionDate(installment);
            if (installment.Status == "Cancelled")
                return null;
            return installment.StartDate.AddMonths(installment.MonthsPaid + 1);
        }

        public DateTime? CalculateCompletionDate(Installment installment)
            => installment.InstallmentPayments
                .Where(payment => payment.Status == "Paid")
                .OrderByDescending(payment => payment.PaymentDate)
                .Select(payment => (DateTime?)payment.PaymentDate)
                .FirstOrDefault();

        public string DetermineInstallmentStatus(Installment installment, decimal actualPaid, DateTime? asOf = null)
        {
            if (installment.Status is "Completed" or "Cancelled") return installment.Status;
            if (installment.Balance <= 0) return "Completed";

            DateTime currentDate = (asOf ?? DateTime.Now).Date;
            int dueMonthsPassed = Enumerable.Range(1, Math.Max(installment.Months, 0))
                .Count(month => installment.StartDate.Date.AddMonths(month) <= currentDate);
            decimal expectedPaid = Math.Min(installment.TotalAmount, dueMonthsPassed * installment.MonthlyPayment);
            return actualPaid < expectedPaid ? "Overdue" : "Active";
        }

        public async Task UpdateInstallmentStatusesAsync(DateTime? asOf = null)
        {
            var installments = await InstallmentQuery()
                .Where(item => item.Status == "Active" ||
                    item.Status == "Overdue" ||
                    item.InterestRate != FixedMonthlyInterestRate)
                .ToListAsync();
            bool changed = false;

            foreach (var installment in installments)
            {
                if (installment.InterestRate != FixedMonthlyInterestRate)
                {
                    installment.InterestRate = FixedMonthlyInterestRate;
                    changed = true;

                    // Correct legacy active plans that were saved with the old 1% rate.
                    // Completed plans retain their settled monetary history.
                    if (installment.Status is "Active" or "Overdue" &&
                        installment.Checkout != null &&
                        installment.Months > 0 &&
                        installment.DownPayment >= 0 &&
                        installment.DownPayment < installment.Checkout.TotalAmount)
                    {
                        var corrected = Calculate(
                            installment.Checkout.TotalAmount,
                            installment.DownPayment,
                            installment.Months);
                        decimal paidBeforeCorrection = CalculateTotalPaid(installment);

                        installment.TotalAmount = corrected.InstallmentTotal;
                        installment.MonthlyPayment = corrected.MonthlyPayment;
                        installment.Balance = Money(Math.Max(0, corrected.InstallmentTotal - paidBeforeCorrection));
                        installment.MonthsPaid = CalculateMonthsPaid(
                            paidBeforeCorrection,
                            corrected.MonthlyPayment,
                            installment.Months);
                        installment.MonthsRemaining = installment.Months - installment.MonthsPaid;
                    }
                }

                if (installment.Balance <= 0)
                {
                    installment.Balance = 0;
                    installment.MonthsPaid = installment.Months;
                    installment.MonthsRemaining = 0;
                    installment.Status = "Completed";
                    if (installment.Checkout != null)
                        installment.Checkout.Status = "Paid";
                    changed = true;
                    continue;
                }

                decimal totalPaid = CalculateTotalPaid(installment);
                int monthsPaid = CalculateMonthsPaid(totalPaid, installment.MonthlyPayment, installment.Months);
                string status = DetermineInstallmentStatus(installment, totalPaid, asOf);

                if (installment.MonthsPaid != monthsPaid ||
                    installment.MonthsRemaining != installment.Months - monthsPaid)
                {
                    installment.MonthsPaid = monthsPaid;
                    installment.MonthsRemaining = installment.Months - monthsPaid;
                    changed = true;
                }
                if (installment.Status != status)
                {
                    installment.Status = status;
                    changed = true;
                }
                if (installment.Checkout != null && installment.Checkout.Status != "Ongoing")
                {
                    installment.Checkout.Status = "Ongoing";
                    changed = true;
                }
            }

            if (changed) await _db.SaveChangesAsync();
        }

        public void SynchronizeOverdueStatuses(DateTime? asOf = null)
            => UpdateInstallmentStatusesAsync(asOf).GetAwaiter().GetResult();

        private IQueryable<Installment> InstallmentQuery()
            => _db.Installments
                .Include(item => item.Checkout)
                    .ThenInclude(checkout => checkout!.Customer)
                .Include(item => item.InstallmentPayments);

        private InstallmentListViewModel ToListItem(Installment installment)
        {
            var customer = installment.Checkout?.Customer;
            decimal totalPaid = CalculateTotalPaid(installment);
            return new InstallmentListViewModel
            {
                InstallmentID = installment.InstallmentID,
                CheckoutID = installment.CheckoutID,
                CustomerName = customer?.customer_FullName ?? "Unknown customer",
                CustomerPhone = customer?.customer_Phone ?? string.Empty,
                CustomerEmail = customer?.customer_Email ?? string.Empty,
                MonthlyPayment = installment.MonthlyPayment,
                Balance = installment.Status == "Completed" ? 0 : installment.Balance,
                TotalPaid = totalPaid,
                Months = installment.Months,
                MonthsPaid = installment.MonthsPaid,
                MonthsRemaining = installment.MonthsRemaining,
                DisplayDate = CalculateDisplayDate(installment),
                Status = installment.Status,
                ProgressPercentage = CalculateProgressPercentage(installment.MonthsPaid, installment.Months)
            };
        }

        private static decimal CalculateTotalPaid(Installment installment)
            => Money(installment.InstallmentPayments
                .Where(payment => payment.Status == "Paid")
                .Sum(payment => payment.PaymentAmount));

        private static decimal CalculateProgressPercentage(int monthsPaid, int months)
            => months <= 0 ? 0 : Math.Clamp(Math.Round(monthsPaid * 100m / months, 1), 0, 100);

        private static decimal Money(decimal value)
            => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
