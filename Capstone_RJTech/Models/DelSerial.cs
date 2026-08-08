using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone_RJTech.Models
{
    public class DelSerial
    {
        [Key]
        [StringLength(100)]
        public string serial_No { get; set; } = string.Empty;

        // Foreign Key
        [Required]
        public int deldetails_ID { get; set; }

        // Navigation Property
        [ForeignKey("deldetails_ID")]
        public virtual DeliveryDetails? DeliveryDetails { get; set; }
    }
}
