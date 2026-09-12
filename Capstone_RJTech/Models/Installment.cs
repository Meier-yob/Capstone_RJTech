using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone_RJTech.Models
{
    [Table("tblInstallment")]
    public class Installment : OwnedEntity
    {
        [Key]
        public int InstallmentID { get; set; }

        [Required]
        public int CheckoutID { get; set; }

        [Range(1, int.MaxValue)]
        public int Months { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DownPayment { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal InterestRate { get; set; } = 5m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyPayment { get; set; }

        public int MonthsPaid { get; set; }
        public int MonthsRemaining { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Required, StringLength(30)]
        public string Status { get; set; } = "Active";

        [ForeignKey(nameof(CheckoutID))]
        public virtual Checkout? Checkout { get; set; }

        public virtual ICollection<InstallmentPayment> InstallmentPayments { get; set; }
            = new List<InstallmentPayment>();

        [NotMapped]
        public string FormattedInstallmentID => $"INS-{InstallmentID:D3}";
    }
}
