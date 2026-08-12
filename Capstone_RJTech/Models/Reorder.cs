namespace Capstone_RJTech.Models
{
    public class Reorder
    {
        public int reorder_ID { get; set; }

        public int product_ID { get; set; }
        public int ordered_quantity { get; set; }
        public int received_quantity { get; set; }
        public string reorder_status { get; set; } = "Pending";
        public DateTime date_requested { get; set; }
        public DateTime? date_completed { get; set; }
        public DateTime? date_cancelled { get; set; }
    }
}
