using System.ComponentModel.DataAnnotations;

namespace Capstone_RJTech.Models
{
    public class ProductCategory
    {
        [Key]
        public int category_ID { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100)]
        public string category_name { get; set; } = string.Empty;

        // Navigation Property (One category has many products)
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    }
}
