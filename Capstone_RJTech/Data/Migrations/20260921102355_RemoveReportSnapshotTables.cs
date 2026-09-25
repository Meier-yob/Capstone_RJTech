using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_RJTech.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReportSnapshotTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblBestSellingProduct");

            migrationBuilder.DropTable(
                name: "tblDeliveryOverview");

            migrationBuilder.DropTable(
                name: "tblDeliverySummary");

            migrationBuilder.DropTable(
                name: "tblInventoryOverview");

            migrationBuilder.DropTable(
                name: "tblLeastSellingProduct");

            migrationBuilder.DropTable(
                name: "tblLeastStockedProduct");

            migrationBuilder.DropTable(
                name: "tblMonthlySales");

            migrationBuilder.DropTable(
                name: "tblMostStockedProduct");

            migrationBuilder.DropTable(
                name: "tblProductCategoryOverview");

            migrationBuilder.DropTable(
                name: "tblProductDeliverySummary");

            migrationBuilder.DropTable(
                name: "tblProductStockSummary");

            migrationBuilder.DropTable(
                name: "tblSalesByCategory");

            migrationBuilder.DropTable(
                name: "tblSalesOverview");

            migrationBuilder.DropTable(
                name: "tblTransaction");

            migrationBuilder.DropTable(
                name: "tblWeeklySales");

            migrationBuilder.DropTable(
                name: "tblYearlySales");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblBestSellingProduct",
                columns: table => new
                {
                    BestSellingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    DateGenerated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalQuantitySold = table.Column<int>(type: "int", nullable: false),
                    TotalSalesAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblBestSellingProduct", x => x.BestSellingID);
                    table.ForeignKey(
                        name: "FK_tblBestSellingProduct_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "product_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblDeliveryOverview",
                columns: table => new
                {
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalDeliveries = table.Column<int>(type: "int", nullable: false),
                    TotalItemsDelivered = table.Column<int>(type: "int", nullable: false),
                    TotalProductsDelivered = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblDeliveryOverview", x => x.LastUpdated);
                });

            migrationBuilder.CreateTable(
                name: "tblDeliverySummary",
                columns: table => new
                {
                    DeliveryID = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalItems = table.Column<int>(type: "int", nullable: false),
                    TotalProducts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblDeliverySummary", x => x.DeliveryID);
                    table.ForeignKey(
                        name: "FK_tblDeliverySummary_Deliveries_DeliveryID",
                        column: x => x.DeliveryID,
                        principalTable: "Deliveries",
                        principalColumn: "delivery_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblInventoryOverview",
                columns: table => new
                {
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AvailableProducts = table.Column<int>(type: "int", nullable: false),
                    LowStockProducts = table.Column<int>(type: "int", nullable: false),
                    OutOfStockProducts = table.Column<int>(type: "int", nullable: false),
                    TotalProducts = table.Column<int>(type: "int", nullable: false),
                    TotalQuantity = table.Column<int>(type: "int", nullable: false),
                    UnavailableProducts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblInventoryOverview", x => x.LastUpdated);
                });

            migrationBuilder.CreateTable(
                name: "tblLeastSellingProduct",
                columns: table => new
                {
                    LeastSellingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    DateGenerated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalQuantitySold = table.Column<int>(type: "int", nullable: false),
                    TotalSalesAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblLeastSellingProduct", x => x.LeastSellingID);
                    table.ForeignKey(
                        name: "FK_tblLeastSellingProduct_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "product_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblLeastStockedProduct",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    CurrentQuantity = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblLeastStockedProduct", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_tblLeastStockedProduct_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "product_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblMonthlySales",
                columns: table => new
                {
                    MonthlySalesID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Month = table.Column<int>(type: "int", nullable: false),
                    TotalItemsSold = table.Column<int>(type: "int", nullable: false),
                    TotalSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTransactions = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblMonthlySales", x => x.MonthlySalesID);
                });

            migrationBuilder.CreateTable(
                name: "tblMostStockedProduct",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    CurrentQuantity = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblMostStockedProduct", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_tblMostStockedProduct_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "product_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblProductCategoryOverview",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    AvailableQuantity = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalProducts = table.Column<int>(type: "int", nullable: false),
                    TotalQuantity = table.Column<int>(type: "int", nullable: false),
                    UnavailableQuantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblProductCategoryOverview", x => x.CategoryID);
                    table.ForeignKey(
                        name: "FK_tblProductCategoryOverview_ProductCategories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "ProductCategories",
                        principalColumn: "category_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblProductDeliverySummary",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    LastDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalDeliveries = table.Column<int>(type: "int", nullable: false),
                    TotalQuantityDelivered = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblProductDeliverySummary", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_tblProductDeliverySummary_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "product_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblProductStockSummary",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    CurrentQuantity = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReorderLevel = table.Column<int>(type: "int", nullable: false),
                    StockStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblProductStockSummary", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_tblProductStockSummary_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "product_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblSalesByCategory",
                columns: table => new
                {
                    SalesCategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    DateGenerated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalQuantitySold = table.Column<int>(type: "int", nullable: false),
                    TotalSalesAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblSalesByCategory", x => x.SalesCategoryID);
                    table.ForeignKey(
                        name: "FK_tblSalesByCategory_ProductCategories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "ProductCategories",
                        principalColumn: "category_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblSalesOverview",
                columns: table => new
                {
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSaleDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MonthlySales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TodaySales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCustomers = table.Column<int>(type: "int", nullable: false),
                    TotalItemsSold = table.Column<int>(type: "int", nullable: false),
                    TotalSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTransactions = table.Column<int>(type: "int", nullable: false),
                    WeeklySales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    YearlySales = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblSalesOverview", x => x.LastUpdated);
                });

            migrationBuilder.CreateTable(
                name: "tblTransaction",
                columns: table => new
                {
                    TransactionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckoutID = table.Column<int>(type: "int", nullable: false),
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblTransaction", x => x.TransactionID);
                    table.ForeignKey(
                        name: "FK_tblTransaction_tblCheckout_CheckoutID",
                        column: x => x.CheckoutID,
                        principalTable: "tblCheckout",
                        principalColumn: "CheckoutID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblTransaction_tblCustomer_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "tblCustomer",
                        principalColumn: "customer_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblWeeklySales",
                columns: table => new
                {
                    WeeklySalesID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TotalItemsSold = table.Column<int>(type: "int", nullable: false),
                    TotalSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTransactions = table.Column<int>(type: "int", nullable: false),
                    WeekEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WeekStartDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblWeeklySales", x => x.WeeklySalesID);
                });

            migrationBuilder.CreateTable(
                name: "tblYearlySales",
                columns: table => new
                {
                    YearlySalesID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TotalItemsSold = table.Column<int>(type: "int", nullable: false),
                    TotalSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTransactions = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblYearlySales", x => x.YearlySalesID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblBestSellingProduct_Period",
                table: "tblBestSellingProduct",
                column: "Period",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblBestSellingProduct_ProductID",
                table: "tblBestSellingProduct",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_tblLeastSellingProduct_Period",
                table: "tblLeastSellingProduct",
                column: "Period",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblLeastSellingProduct_ProductID",
                table: "tblLeastSellingProduct",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_tblMonthlySales_Year_Month",
                table: "tblMonthlySales",
                columns: new[] { "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblSalesByCategory_CategoryID",
                table: "tblSalesByCategory",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_tblSalesByCategory_Period_CategoryID",
                table: "tblSalesByCategory",
                columns: new[] { "Period", "CategoryID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblTransaction_CheckoutID",
                table: "tblTransaction",
                column: "CheckoutID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblTransaction_CustomerID",
                table: "tblTransaction",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_tblWeeklySales_WeekStartDate",
                table: "tblWeeklySales",
                column: "WeekStartDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblYearlySales_Year",
                table: "tblYearlySales",
                column: "Year",
                unique: true);
        }
    }
}
