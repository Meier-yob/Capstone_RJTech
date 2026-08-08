using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone_RJTech.Models
{
    public class ProductSerial
    {
        [Key]
        [StringLength(100)]
        public string serial_No { get; set; } = string.Empty;

        // Foreign Key
        [Required]
        public int product_ID { get; set; }

        // Batch identifier that groups serials inserted together
        [Required]
        [StringLength(100)]
        public string batch_ID { get; set; } = string.Empty;

        // Navigation Property
        [ForeignKey("product_ID")]
        public virtual Product? Product { get; set; }
    }
}
