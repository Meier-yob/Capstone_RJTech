using System.ComponentModel.DataAnnotations;

namespace Capstone_RJTech.Models
{
    public class AppNotification
    {
        [Key]
        public int notification_ID { get; set; }
        public int? product_ID { get; set; }

        [Required]
        [StringLength(150)]
        public string title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string message { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string notification_type { get; set; } = "info";

        [Required]
        [StringLength(300)]
        public string action_url { get; set; } = "/Product/ProductManagement";
        public DateTime created_at { get; set; } = DateTime.Now;
        public bool is_read { get; set; }
    }
}
