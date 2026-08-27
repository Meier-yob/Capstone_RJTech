using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_RJTech.Data.Migrations
{
    /// <inheritdoc />
    public partial class SalesCheckoutFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblCustomer",
                columns: table => new
                {
                    customer_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    customer_FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    customer_Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    customer_Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCustomer", x => x.customer_ID);
                });

            migrationBuilder.CreateTable(
                name: "tblCheckout",
                columns: table => new
                {
                    CheckoutID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DatePurchased = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCheckout", x => x.CheckoutID);
                    table.ForeignKey(
                        name: "FK_tblCheckout_tblCustomer_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "tblCustomer",
                        principalColumn: "customer_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblCheckoutItem",
                columns: table => new
                {
                    CheckoutItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckoutID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    SerialNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ItemQuantity = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCheckoutItem", x => x.CheckoutItemID);
                    table.ForeignKey(
                        name: "FK_tblCheckoutItem_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "product_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblCheckoutItem_tblCheckout_CheckoutID",
                        column: x => x.CheckoutID,
                        principalTable: "tblCheckout",
                        principalColumn: "CheckoutID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblCheckout_CustomerID",
                table: "tblCheckout",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_tblCheckoutItem_CheckoutID",
                table: "tblCheckoutItem",
                column: "CheckoutID");

            migrationBuilder.CreateIndex(
                name: "IX_tblCheckoutItem_ProductID",
                table: "tblCheckoutItem",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_tblCheckoutItem_SerialNo",
                table: "tblCheckoutItem",
                column: "SerialNo",
                unique: true,
                filter: "[SerialNo] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomer_customer_Email",
                table: "tblCustomer",
                column: "customer_Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblCheckoutItem");

            migrationBuilder.DropTable(
                name: "tblCheckout");

            migrationBuilder.DropTable(
                name: "tblCustomer");
        }
    }
}
