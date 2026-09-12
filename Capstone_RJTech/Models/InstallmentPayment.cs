using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone_RJTech.Models
{
    [Table("tblInstallmentPayment")]
    public class InstallmentPayment : OwnedEntity
    {
        [Key]
        public int PaymentID { get; set; }

        [Required]
        public int InstallmentID { get; set; }

        [Required, StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash";

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaymentAmount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required, StringLength(30)]
        public string Status { get; set; } = "Paid";

        [ForeignKey(nameof(InstallmentID))]
        public virtual Installment? Installment { get; set; }
    }
}
