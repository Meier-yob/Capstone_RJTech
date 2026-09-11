using Capstone_RJTech.Models;
using System.ComponentModel.DataAnnotations;

namespace Capstone_RJTech.ViewModels
{
    public class InstallmentManagementViewModel
    {
        public IReadOnlyList<InstallmentListViewModel> Installments { get; set; }
            = Array.Empty<InstallmentListViewModel>();
    }

    public class InstallmentListViewModel
    {
        public int InstallmentID { get; set; }
        public string FormattedInstallmentID => $"INS-{InstallmentID:D3}";
        public int CheckoutID { get; set; }
        public string FormattedCheckoutID => $"CHK-{CheckoutID:D3}";
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal MonthlyPayment { get; set; }
        public decimal Balance { get; set; }
        public decimal TotalPaid { get; set; }
        public int Months { get; set; }
        public int MonthsPaid { get; set; }
        public int MonthsRemaining { get; set; }
        public DateTime? DisplayDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal ProgressPercentage { get; set; }
    }

    public class InstallmentDetailsViewModel
    {
        public required Installment Installment { get; set; }
        public required Checkout Checkout { get; set; }
        public Customer? Customer { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal ProgressPercentage { get; set; }
        public DateTime? DisplayDate { get; set; }
        public DateTime? CompletionDate { get; set; }
    }

    public class RecordInstallmentPaymentViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a valid installment.")]
        public int InstallmentID { get; set; }

        [Range(typeof(decimal), "0.01", "79228162514264337593543950335",
            ErrorMessage = "Payment amount must be greater than zero.")]
        public decimal PaymentAmount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = "Cash";
    }
}
