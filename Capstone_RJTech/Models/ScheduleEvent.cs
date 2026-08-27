using System.ComponentModel.DataAnnotations;

namespace Capstone_RJTech.Models
{
    public class ScheduleEvent
    {
        [Key]
        public int event_ID { get; set; }

        [Required]
        [StringLength(120)]
        public string title { get; set; } = string.Empty;
        public DateTime event_date { get; set; }
        public TimeSpan start_time { get; set; }
        public TimeSpan end_time { get; set; }
        [StringLength(500)]
        public string notes { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string color { get; set; } = "blue";
    }
}
