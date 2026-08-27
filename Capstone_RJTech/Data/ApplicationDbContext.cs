using Capstone_RJTech.Models;
using Microsoft.EntityFrameworkCore;

namespace Capstone_RJTech.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
        public DbSet<Delivery> Deliveries => Set<Delivery>();
        public DbSet<DeliveryDetails> DeliveryDetails => Set<DeliveryDetails>();
        public DbSet<AppNotification> Notifications => Set<AppNotification>();
        public DbSet<ScheduleEvent> ScheduleEvents => Set<ScheduleEvent>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Checkout> Checkouts => Set<Checkout>();
        public DbSet<CheckoutItem> CheckoutItems => Set<CheckoutItem>();
        public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductCategory>()
                .HasIndex(category => category.category_name)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(product => new { product.category_ID, product.product_name, product.product_brand })
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.category_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Delivery>()
                .HasIndex(delivery => delivery.batch_ID)
                .IsUnique();

            modelBuilder.Entity<DeliveryDetails>()
                .HasOne(detail => detail.Product)
                .WithMany(product => product.DeliveryDetails)
                .HasForeignKey(detail => detail.product_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DeliveryDetails>()
                .HasOne(detail => detail.Delivery)
                .WithMany(delivery => delivery.DeliveryDetails)
                .HasForeignKey(detail => detail.delivery_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AppNotification>()
                .HasOne<Product>()
                .WithMany()
                .HasForeignKey(notification => notification.product_ID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Customer>()
                .HasIndex(customer => customer.customer_Email)
                .IsUnique();

            modelBuilder.Entity<Checkout>()
                .HasIndex(checkout => checkout.CheckoutNumber)
                .IsUnique();

            modelBuilder.Entity<Checkout>()
                .HasOne(checkout => checkout.Customer)
                .WithMany(customer => customer.Checkouts)
                .HasForeignKey(checkout => checkout.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CheckoutItem>()
                .HasOne(item => item.Checkout)
                .WithMany(checkout => checkout.CheckoutItems)
                .HasForeignKey(item => item.CheckoutID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CheckoutItem>()
                .HasOne(item => item.Product)
                .WithMany(product => product.CheckoutItems)
                .HasForeignKey(item => item.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CheckoutItem>()
                .HasIndex(item => item.SerialNo)
                .HasFilter("[SerialNo] IS NOT NULL")
                .IsUnique();

            modelBuilder.Entity<ProductCategory>().HasData(
                new ProductCategory { category_ID = 1, category_name = "Monitors" },
                new ProductCategory { category_ID = 2, category_name = "Mouses" },
                new ProductCategory { category_ID = 3, category_name = "Keyboards" },
                new ProductCategory { category_ID = 4, category_name = "Headsets" });

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    product_ID = 1,
                    product_name = "Optical Wired Mouse",
                    product_brand = "A4 Tech",
                    product_description = "Optical Wired Mouse",
                    product_quantity = 0,
                    reorder_level = 5,
                    Product_price = 200.00M,
                    product_status = "Unavailable",
                    category_ID = 2
                },
                new Product
                {
                    product_ID = 2,
                    product_name = "Mechanical Keyboard",
                    product_brand = "Logitech",
                    product_description = "Mechanical Keyboard",
                    product_quantity = 0,
                    reorder_level = 5,
                    Product_price = 1200.00M,
                    product_status = "Unavailable",
                    category_ID = 3
                });
        }
    }
}
