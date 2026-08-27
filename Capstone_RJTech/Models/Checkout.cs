using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone_RJTech.Models
{
    [Table("tblCheckout")]
    public class Checkout
    {
        [Key]
        public int CheckoutID { get; set; }

        public int CheckoutNumber { get; set; }

        [Required]
        public int CustomerID { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required, StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash";

        public DateTime DatePurchased { get; set; } = DateTime.Now;

        [Required, StringLength(30)]
        public string Status { get; set; } = "Completed";

        [ForeignKey(nameof(CustomerID))]
        public virtual Customer? Customer { get; set; }

        public virtual ICollection<CheckoutItem> CheckoutItems { get; set; } = new List<CheckoutItem>();

        [NotMapped]
        public string FormattedCheckoutID => $"CHK-{CheckoutNumber:D3}";
    }
}
