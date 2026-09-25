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
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Checkout> Checkouts => Set<Checkout>();
        public DbSet<CheckoutItem> CheckoutItems => Set<CheckoutItem>();
        public DbSet<Installment> Installments => Set<Installment>();
        public DbSet<InstallmentPayment> InstallmentPayments => Set<InstallmentPayment>();
        public DbSet<CustomerPurchaseHistory> CustomerPurchaseHistories => Set<CustomerPurchaseHistory>();
        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>()
                .HasIndex(user => user.Username)
                .IsUnique();

            modelBuilder.Entity<AppUser>()
                .HasIndex(user => user.Email)
                .IsUnique();

            modelBuilder.Entity<PasswordResetCode>()
                .HasIndex(code => code.Email);

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
                .HasFilter("[SerialNo] IS NOT NULL");

            modelBuilder.Entity<Checkout>()
                .HasOne(checkout => checkout.Installment)
                .WithOne(installment => installment.Checkout)
                .HasForeignKey<Installment>(installment => installment.CheckoutID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Installment>()
                .HasIndex(installment => installment.CheckoutID)
                .IsUnique();

            modelBuilder.Entity<InstallmentPayment>()
                .HasOne(payment => payment.Installment)
                .WithMany(installment => installment.InstallmentPayments)
                .HasForeignKey(payment => payment.InstallmentID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CustomerPurchaseHistory>()
                .HasOne(history => history.Customer)
                .WithMany()
                .HasForeignKey(history => history.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CustomerPurchaseHistory>()
                .HasOne(history => history.Checkout)
                .WithMany()
                .HasForeignKey(history => history.CheckoutID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CustomerPurchaseHistory>()
                .HasIndex(history => new { history.PurchaseDate, history.HistoryID });

            modelBuilder.Entity<ProductCategory>().HasData(
                new ProductCategory { category_ID = 1, category_name = "Monitors" },
                new ProductCategory { category_ID = 2, category_name = "Mouses" },
                new ProductCategory { category_ID = 3, category_name = "Keyboards" },
                new ProductCategory { category_ID = 4, category_name = "Headsets" },
                new ProductCategory { category_ID = 5, category_name = "Computer Accessories" });

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