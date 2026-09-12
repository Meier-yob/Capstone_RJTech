using Capstone_RJTech.Models;
using Capstone_RJTech.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Capstone_RJTech.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ReportUpdateTracker? _reportUpdates;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ReportUpdateTracker? reportUpdates = null,
            IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            _reportUpdates = reportUpdates;
            _httpContextAccessor = httpContextAccessor ?? new HttpContextAccessor();
        }

        public override int SaveChanges()
            => SaveChanges(acceptAllChangesOnSuccess: true);

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ApplyOwnerToNewEntities();
            bool reportsChanged = HasReportSourceChanges();
            int savedCount = base.SaveChanges(acceptAllChangesOnSuccess);
            if (reportsChanged && savedCount > 0)
                _reportUpdates?.MarkSourceChanged();
            return savedCount;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

        public override async Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            ApplyOwnerToNewEntities();
            bool reportsChanged = HasReportSourceChanges();
            int savedCount = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            if (reportsChanged && savedCount > 0)
                _reportUpdates?.MarkSourceChanged();
            return savedCount;
        }

        private bool HasReportSourceChanges()
            => ChangeTracker.Entries().Any(entry =>
                (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted) &&
                entry.Entity is Product or ProductCategory or Delivery or Capstone_RJTech.Models.DeliveryDetails or
                    Customer or Checkout or CheckoutItem);

        private void ApplyOwnerToNewEntities()
        {
            var ownerId = CurrentOwnerId;
            var isOwner = _httpContextAccessor.HttpContext?.User.IsInRole("Owner") == true;

            foreach (var entry in ChangeTracker.Entries<OwnedEntity>()
                         .Where(entry => entry.State == EntityState.Added))
            {
                if (string.IsNullOrWhiteSpace(ownerId) || !isOwner)
                {
                    throw new InvalidOperationException(
                        "An authenticated Owner account is required to create business data.");
                }

                entry.Entity.OwnerID = ownerId;
            }
        }

        private string? CurrentOwnerId
            => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

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
        public DbSet<SalesOverviewReport> SalesOverviews => Set<SalesOverviewReport>();
        public DbSet<WeeklySalesReport> WeeklySales => Set<WeeklySalesReport>();
        public DbSet<MonthlySalesReport> MonthlySales => Set<MonthlySalesReport>();
        public DbSet<YearlySalesReport> YearlySales => Set<YearlySalesReport>();
        public DbSet<BestSellingProductReport> BestSellingProducts => Set<BestSellingProductReport>();
        public DbSet<LeastSellingProductReport> LeastSellingProducts => Set<LeastSellingProductReport>();
        public DbSet<SalesByCategoryReport> SalesByCategories => Set<SalesByCategoryReport>();
        public DbSet<TransactionReport> ReportTransactions => Set<TransactionReport>();
        public DbSet<InventoryOverviewReport> InventoryOverviews => Set<InventoryOverviewReport>();
        public DbSet<ProductStockSummaryReport> ProductStockSummaries => Set<ProductStockSummaryReport>();
        public DbSet<ProductCategoryOverviewReport> ProductCategoryOverviews => Set<ProductCategoryOverviewReport>();
        public DbSet<MostStockedProductReport> MostStockedProducts => Set<MostStockedProductReport>();
        public DbSet<LeastStockedProductReport> LeastStockedProducts => Set<LeastStockedProductReport>();
        public DbSet<DeliveryOverviewReport> DeliveryOverviews => Set<DeliveryOverviewReport>();
        public DbSet<DeliverySummaryReport> DeliverySummaries => Set<DeliverySummaryReport>();
        public DbSet<ProductDeliverySummaryReport> ProductDeliverySummaries => Set<ProductDeliverySummaryReport>();
        public DbSet<Owner> Owners => Set<Owner>();
        public DbSet<OwnerInvitation> OwnerInvitations => Set<OwnerInvitation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Owner>()
                .HasIndex(owner => owner.UserName)
                .IsUnique();

            modelBuilder.Entity<Owner>()
                .HasIndex(owner => owner.RecoveryEmail)
                .IsUnique();

            modelBuilder.Entity<OwnerInvitation>()
                .Property(invitation => invitation.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<OwnerInvitation>()
                .HasIndex(invitation => invitation.TokenHash)
                .IsUnique();

            modelBuilder.Entity<OwnerInvitation>()
                .HasIndex(invitation => new { invitation.Email, invitation.Status, invitation.ExpiresAt });

            ConfigureOwnerScope<ProductCategory>(modelBuilder);
            ConfigureOwnerScope<Product>(modelBuilder);
            ConfigureOwnerScope<Delivery>(modelBuilder);
            ConfigureOwnerScope<DeliveryDetails>(modelBuilder);
            ConfigureOwnerScope<AppNotification>(modelBuilder);
            ConfigureOwnerScope<Customer>(modelBuilder);
            ConfigureOwnerScope<Checkout>(modelBuilder);
            ConfigureOwnerScope<CheckoutItem>(modelBuilder);
            ConfigureOwnerScope<Installment>(modelBuilder);
            ConfigureOwnerScope<InstallmentPayment>(modelBuilder);
            ConfigureOwnerScope<CustomerPurchaseHistory>(modelBuilder);
            ConfigureOwnerScope<SalesOverviewReport>(modelBuilder);
            ConfigureOwnerScope<WeeklySalesReport>(modelBuilder);
            ConfigureOwnerScope<MonthlySalesReport>(modelBuilder);
            ConfigureOwnerScope<YearlySalesReport>(modelBuilder);
            ConfigureOwnerScope<BestSellingProductReport>(modelBuilder);
            ConfigureOwnerScope<LeastSellingProductReport>(modelBuilder);
            ConfigureOwnerScope<SalesByCategoryReport>(modelBuilder);
            ConfigureOwnerScope<TransactionReport>(modelBuilder);
            ConfigureOwnerScope<InventoryOverviewReport>(modelBuilder);
            ConfigureOwnerScope<ProductStockSummaryReport>(modelBuilder);
            ConfigureOwnerScope<ProductCategoryOverviewReport>(modelBuilder);
            ConfigureOwnerScope<MostStockedProductReport>(modelBuilder);
            ConfigureOwnerScope<LeastStockedProductReport>(modelBuilder);
            ConfigureOwnerScope<DeliveryOverviewReport>(modelBuilder);
            ConfigureOwnerScope<DeliverySummaryReport>(modelBuilder);
            ConfigureOwnerScope<ProductDeliverySummaryReport>(modelBuilder);

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

            modelBuilder.Entity<WeeklySalesReport>()
                .HasIndex(report => report.WeekStartDate)
                .IsUnique();

            modelBuilder.Entity<MonthlySalesReport>()
                .HasIndex(report => new { report.Year, report.Month })
                .IsUnique();

            modelBuilder.Entity<YearlySalesReport>()
                .HasIndex(report => report.Year)
                .IsUnique();

            modelBuilder.Entity<BestSellingProductReport>()
                .HasIndex(report => report.Period)
                .IsUnique();

            modelBuilder.Entity<LeastSellingProductReport>()
                .HasIndex(report => report.Period)
                .IsUnique();

            modelBuilder.Entity<SalesByCategoryReport>()
                .HasIndex(report => new { report.Period, report.CategoryID })
                .IsUnique();

            modelBuilder.Entity<TransactionReport>()
                .HasIndex(report => report.CheckoutID)
                .IsUnique();

            modelBuilder.Entity<BestSellingProductReport>()
                .HasOne(report => report.Product)
                .WithMany()
                .HasForeignKey(report => report.ProductID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeastSellingProductReport>()
                .HasOne(report => report.Product)
                .WithMany()
                .HasForeignKey(report => report.ProductID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesByCategoryReport>()
                .HasOne(report => report.Category)
                .WithMany()
                .HasForeignKey(report => report.CategoryID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransactionReport>()
                .HasOne(report => report.Checkout)
                .WithMany()
                .HasForeignKey(report => report.CheckoutID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransactionReport>()
                .HasOne(report => report.Customer)
                .WithMany()
                .HasForeignKey(report => report.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductStockSummaryReport>()
                .HasOne(report => report.Product)
                .WithMany()
                .HasForeignKey(report => report.ProductID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductCategoryOverviewReport>()
                .HasOne(report => report.Category)
                .WithMany()
                .HasForeignKey(report => report.CategoryID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MostStockedProductReport>()
                .HasOne(report => report.Product)
                .WithMany()
                .HasForeignKey(report => report.ProductID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeastStockedProductReport>()
                .HasOne(report => report.Product)
                .WithMany()
                .HasForeignKey(report => report.ProductID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DeliverySummaryReport>()
                .HasOne(report => report.Delivery)
                .WithMany()
                .HasForeignKey(report => report.DeliveryID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductDeliverySummaryReport>()
                .HasOne(report => report.Product)
                .WithMany()
                .HasForeignKey(report => report.ProductID)
                .OnDelete(DeleteBehavior.Cascade);

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

        private void ConfigureOwnerScope<TEntity>(ModelBuilder modelBuilder)
            where TEntity : OwnedEntity
        {
            var entity = modelBuilder.Entity<TEntity>();
            entity.Property(item => item.OwnerID)
                .HasMaxLength(450)
                .HasColumnName("OwnerId")
                .IsRequired();
            entity.HasIndex(item => item.OwnerID);
            entity.HasOne<Owner>()
                .WithMany()
                .HasPrincipalKey(owner => owner.OwnerID)
                .HasForeignKey(item => item.OwnerID)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(entityInstance =>
                _httpContextAccessor.HttpContext != null &&
                _httpContextAccessor.HttpContext.User.Identity != null &&
                _httpContextAccessor.HttpContext.User.Identity.IsAuthenticated &&
                entityInstance.OwnerID == CurrentOwnerId);
        }
    }
}
