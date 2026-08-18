namespace Capstone_RJTech.Models
{
    /// <summary>
    /// Shared in-memory state for the prototype controllers. Keeping the data in
    /// one store prevents ProductController and DeliveryController from creating
    /// separate inventories while the project is not yet connected to a database.
    /// </summary>
    internal static class InventoryStore
    {
        internal static readonly List<ProductCategory> Categories = new()
        {
            new ProductCategory { category_ID = 1, category_name = "Monitors" },
            new ProductCategory { category_ID = 2, category_name = "Mouses" },
            new ProductCategory { category_ID = 3, category_name = "Keyboards" },
            new ProductCategory { category_ID = 4, category_name = "Headsets" }
        };

        internal static readonly List<Product> Products = new()
        {
            new Product { product_ID = 1, product_name = "Optical Wired Mouse", product_brand = "A4 Tech", product_description = "Optical Wired Mouse", product_quantity = 0, reorder_level = 5, Product_price = 200.00M, product_status = "Unavailable", category_ID = 2 },
            new Product { product_ID = 2, product_name = "Mechanical Keyboard", product_brand = "Logitech", product_description = "Mechanical Keyboard", product_quantity = 0, reorder_level = 5, Product_price = 1200.00M, product_status = "Unavailable", category_ID = 3 }
        };

        internal static readonly List<Delivery> Deliveries = new();
        internal static readonly List<DeliveryDetails> DeliveryDetails = new();
        internal static readonly List<ProductSerial> ProductSerials = new();
        internal static readonly List<DelSerial> DeliverySerials = new();

        internal static string LastDeliveryDate { get; set; } = string.Empty;
        internal static int GlobalBatchSequence { get; set; }
    }
}
