using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Capstone_RJTech.Services
{
    /// <summary>
    /// Keeps notifications in sync with installment due dates.
    /// Mirrors <see cref="StockNotificationService"/>: called on-demand from the
    /// notification views so the bell flyout / notification page stay current.
    /// </summary>
    public class InstallmentNotificationService
    {
        public const string OverdueType = "installment-overdue";
        public const string DueSoonType = "installment-due-soon";

        /// <summary>A payment is "almost due" when its due date is this many days away or fewer.</summary>
        private const int DueSoonWindowDays = 7;

        private readonly ApplicationDbContext _db;

        public InstallmentNotificationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public void Synchronize()
        {
            var installments = _db.Installments
                .Include(item => item.Checkout)
                    .ThenInclude(checkout => checkout!.Customer)
                .ToList();

            var notifications = _db.Notifications
                .Where(item => item.notification_type == OverdueType || item.notification_type == DueSoonType)
                .ToList();

            // Only installments still being paid can carry an alert.
            var actionableUrls = installments
                .Where(IsActionable)
                .Select(item => InstallmentUrl(item.InstallmentID))
                .ToHashSet();

            // Drop alerts left behind by completed, cancelled, or deleted installments.
            _db.Notifications.RemoveRange(
                notifications.Where(item => !actionableUrls.Contains(item.action_url)));

            var culture = CultureInfo.GetCultureInfo("en-PH");

            foreach (var installment in installments.Where(IsActionable))
            {
                // Next due date is always one month after the last paid month.
                var dueDate = installment.StartDate.AddMonths(installment.MonthsPaid + 1).Date;
                var daysUntilDue = (dueDate - DateTime.Today).Days;

                string? alertType = daysUntilDue < 0 ? OverdueType
                    : daysUntilDue <= DueSoonWindowDays ? DueSoonType
                    : null;

                var url = InstallmentUrl(installment.InstallmentID);

                if (alertType == null)
                {
                    // Back on track (or not yet near the window) — no alert needed.
                    _db.Notifications.RemoveRange(
                        notifications.Where(item => item.action_url == url));
                    continue;
                }

                // The installment moved from one stage to another — clear the stale alert.
                _db.Notifications.RemoveRange(
                    notifications.Where(item => item.action_url == url && item.notification_type != alertType));

                string title = alertType == OverdueType ? "Installment overdue" : "Installment due soon";
                string message = BuildMessage(installment, dueDate, daysUntilDue, alertType, culture);
                var existingAlert = notifications
                    .FirstOrDefault(item => item.action_url == url && item.notification_type == alertType);

                if (existingAlert != null)
                {
                    existingAlert.title = title;
                    existingAlert.message = message;
                    existingAlert.action_url = url;
                    continue;
                }

                _db.Notifications.Add(new AppNotification
                {
                    title = title,
                    message = message,
                    notification_type = alertType,
                    action_url = url,
                    created_at = DateTime.Now
                });
            }

            _db.SaveChanges();
        }

        private static bool IsActionable(Installment installment)
            => installment.Status is not ("Completed" or "Cancelled") && installment.Balance > 0;

        private static string InstallmentUrl(int id) => $"/Installment/Details/{id}";

        private static string BuildMessage(
            Installment installment, DateTime dueDate, int daysUntilDue, string alertType, CultureInfo culture)
        {
            string customerName = installment.Checkout?.Customer?.customer_FullName ?? "Customer";
            string dueText = dueDate.ToString("MMM d, yyyy", culture);
            string balance = "₱" + installment.Balance.ToString("N2", culture);

            if (alertType == OverdueType)
            {
                return $"{installment.FormattedInstallmentID} · {customerName} — payment was due {dueText}. Balance: {balance}.";
            }

            string timing = daysUntilDue <= 0 ? "due today" : $"{daysUntilDue} day{(daysUntilDue == 1 ? "" : "s")} left";
            return $"{installment.FormattedInstallmentID} · {customerName} — payment due {dueText} ({timing}). Balance: {balance}.";
        }
    }
}