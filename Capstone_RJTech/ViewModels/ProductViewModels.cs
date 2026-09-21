using System.ComponentModel.DataAnnotations;

namespace Capstone_RJTech.ViewModels;

/// <summary>
/// Form model for creating a single product (/Product/Create).
/// Mirrors the Product entity fields so existing form inputs keep binding,
/// and adds <see cref="IsSerialized"/> for the inventory tracking type.
/// </summary>
public sealed class ProductCreateViewModel
{
    public int category_ID { get; set; }

    [Required(ErrorMessage = "Product name is required")]
    [StringLength(150)]
    public string product_name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Brand is required")]
    [StringLength(100)]
    public string product_brand { get; set; } = string.Empty;

    [StringLength(500)]
    public string? product_description { get; set; }

    public int product_quantity { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Reorder level cannot be negative")]
    public int reorder_level { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero")]
    public decimal Product_price { get; set; }

    public string product_status { get; set; } = "Unavailable";

    /// <summary>
    /// Inventory tracking type: true = Serialized (individual serial numbers),
    /// false = Non-Serialized (standard bulk stock).
    /// </summary>
    public bool IsSerialized { get; set; }
}

/// <summary>
/// One row of the Bulk Add Products modal (POSTed to /Product/BulkCreate).
/// </summary>
public sealed class BulkProductRowDto
{
    public int category_ID { get; set; }
    public string? product_name { get; set; }
    public string? product_brand { get; set; }
    public string? product_description { get; set; }
    public int reorder_level { get; set; }
    public decimal Product_price { get; set; }

    /// <summary>
    /// Inventory tracking type: true = Serialized (individual serial numbers),
    /// false = Non-Serialized (standard bulk stock).
    /// </summary>
    public bool IsSerialized { get; set; }
}