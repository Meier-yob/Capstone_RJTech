using System.ComponentModel.DataAnnotations;

namespace Capstone_RJTech.Models
{
    public class AppNotification
    {
        [Key]
        public int notification_ID { get; set; }
        public int? product_ID { get; set; }
        public string title { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public string notification_type { get; set; } = "info";
        public string action_url { get; set; } = "/Product/ProductManagement";
        public DateTime created_at { get; set; } = DateTime.Now;
        public bool is_read { get; set; }
    }
}
