using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone_RJTech.Models;

[Table("tblCustomerPurchaseHistory")]
public class CustomerPurchaseHistory : OwnedEntity
{
    [Key]
    public int HistoryID { get; set; }

    public int CustomerID { get; set; }
    public int CheckoutID { get; set; }
    public DateTime PurchaseDate { get; set; }

    // The amount received in this payment, not the entire installment contract.
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Required, StringLength(50)]
    public string PaymentMethod { get; set; } = "Cash";

    public Customer? Customer { get; set; }
    public Checkout? Checkout { get; set; }

    [NotMapped]
    public string FormattedHistoryID => $"PAY-{HistoryID:D3}";
}
