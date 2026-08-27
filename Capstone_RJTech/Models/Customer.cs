using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone_RJTech.Models
{
    [Table("tblCustomer")]
    public class Customer
    {
        [Key]
        public int customer_ID { get; set; }

        [Required, StringLength(150)]
        public string customer_FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(200)]
        [RegularExpression(@"^[^@\s]+@[Gg][Mm][Aa][Ii][Ll]\.[Cc][Oo][Mm]$",
            ErrorMessage = "Email must be a valid @gmail.com address")]
        public string customer_Email { get; set; } = string.Empty;

        [Required, StringLength(11, MinimumLength = 11)]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must contain exactly 11 digits")]
        public string customer_Phone { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string customer_Address { get; set; } = string.Empty;

        public virtual ICollection<Checkout> Checkouts { get; set; } = new List<Checkout>();
    }
}
