using Capstone_RJTech.Controllers;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone_RJTech.Models
{
    public class Product
    {
        [Key]
        public int product_ID { get; set; }

        [Required(ErrorMessage = "Brand is required")]
        [StringLength(100)]
        public string product_brand { get; set; } = string.Empty;

        [StringLength(500)]
        public string? product_description { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative")]
        public int product_quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero")]
        public decimal Product_price { get; set; }

        [Required]
        [StringLength(50)]
        public string product_status { get; set; } = "Available";

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Reorder level cannot be negative")]
        public int reorder_level { get; set; }

        // Foreign Key
        [Required]
        public int category_ID { get; set; }

        // Navigation Properties
        [ForeignKey("category_ID")]
        public virtual ProductCategory? Category { get; set; }

        public virtual ICollection<ProductSerial> ProductSerials { get; set; } = new List<ProductSerial>();
        public virtual ICollection<DeliveryDetails> DeliveryDetails { get; set; } = new List<DeliveryDetails>();

        // Computed formatted code (not mapped to the database)
        [NotMapped]
        public string formatted_code => HomeController.GetFormattedCodeForProduct(this);

        // Backwards-compatible alias expected by some views/controllers
        // Exposes the same formatted code using the older snake_case name
        [NotMapped]
        public string product_code => formatted_code;
    }
}