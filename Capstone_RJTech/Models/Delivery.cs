using System.ComponentModel.DataAnnotations;

namespace Capstone_RJTech.Models
{
    public class Delivery
    {
        [Key]
        public int delivery_ID { get; set; }

        [Required]
        public DateTime date_delivered { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string received_by { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string batch_ID { get; set; } = string.Empty;

        // Navigation Property (One delivery has many details)
        public virtual ICollection<DeliveryDetails> DeliveryDetails { get; set; } = new List<DeliveryDetails>();
    }
}
