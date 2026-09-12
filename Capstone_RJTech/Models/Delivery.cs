using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone_RJTech.Models
{
    public class Delivery : OwnedEntity
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

        public bool is_archived { get; set; }

        [NotMapped]
        public string FormattedDeliveryID => $"DEL-{delivery_ID:D3}";

        [NotMapped]
        public string FormattedBatchID => $"BATCH-{delivery_ID:D3}";

        // Navigation Property (One delivery has many details)
        public virtual ICollection<DeliveryDetails> DeliveryDetails { get; set; } = new List<DeliveryDetails>();
    }
}
