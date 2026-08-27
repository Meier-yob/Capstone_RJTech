using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone_RJTech.Models
{
    [Table("tblCheckoutItem")]
    public class CheckoutItem
    {
        [Key]
        public int CheckoutItemID { get; set; }

        [Required]
        public int CheckoutID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [StringLength(100)]
        public string? SerialNo { get; set; }

        [Range(1, int.MaxValue)]
        public int ItemQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [ForeignKey(nameof(CheckoutID))]
        public virtual Checkout? Checkout { get; set; }

        [ForeignKey(nameof(ProductID))]
        public virtual Product? Product { get; set; }
    }
}
