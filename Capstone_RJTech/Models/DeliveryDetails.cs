using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone_RJTech.Models
{
    public class DeliveryDetails
    {
        [Key]
        public int deldetails_ID { get; set; }

        [Required]
        public int product_quantity { get; set; }

        // Foreign Keys
        [Required]
        public int product_ID { get; set; }

        [Required]
        public int delivery_ID { get; set; }

        // Navigation Properties
        [ForeignKey("product_ID")]
        public virtual Product? Product { get; set; }

        [ForeignKey("delivery_ID")]
        public virtual Delivery? Delivery { get; set; }

        public virtual ICollection<DelSerial> DelSerials { get; set; } = new List<DelSerial>();
    }
}
