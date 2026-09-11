using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_RJTech.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPurchaseHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblCustomerPurchaseHistory",
                columns: table => new
                {
                    HistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    CheckoutID = table.Column<int>(type: "int", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCustomerPurchaseHistory", x => x.HistoryID);
                    table.ForeignKey(
                        name: "FK_tblCustomerPurchaseHistory_tblCheckout_CheckoutID",
                        column: x => x.CheckoutID,
                        principalTable: "tblCheckout",
                        principalColumn: "CheckoutID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblCustomerPurchaseHistory_tblCustomer_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "tblCustomer",
                        principalColumn: "customer_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerPurchaseHistory_CheckoutID",
                table: "tblCustomerPurchaseHistory",
                column: "CheckoutID");

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerPurchaseHistory_CustomerID",
                table: "tblCustomerPurchaseHistory",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerPurchaseHistory_PurchaseDate_HistoryID",
                table: "tblCustomerPurchaseHistory",
                columns: new[] { "PurchaseDate", "HistoryID" });

            // Runs once as part of this migration, in the migration transaction.
            // Down payments are stored on plans; later receipts are stored on payments.
            // Preserve receipts for refunded/cancelled orders as historical money received.
            migrationBuilder.Sql("""
                INSERT INTO [tblCustomerPurchaseHistory]
                    ([CustomerID], [CheckoutID], [PurchaseDate], [TotalAmount], [PaymentMethod])
                SELECT TOP (2147483647)
                    [CustomerID], [CheckoutID], [PurchaseDate], [TotalAmount], [PaymentMethod]
                FROM (
                    SELECT c.[CustomerID], c.[CheckoutID], c.[DatePurchased] AS [PurchaseDate],
                           c.[TotalAmount], c.[PaymentMethod], 0 AS [SourceKind], c.[CheckoutID] AS [SourceID]
                    FROM [tblCheckout] c
                    WHERE c.[PaymentType] = N'Full Payment' AND c.[TotalAmount] > 0
                    UNION ALL
                    SELECT c.[CustomerID], c.[CheckoutID], c.[DatePurchased],
                           i.[DownPayment], c.[PaymentMethod], 1, i.[InstallmentID]
                    FROM [tblInstallment] i
                    INNER JOIN [tblCheckout] c ON c.[CheckoutID] = i.[CheckoutID]
                    WHERE c.[PaymentType] = N'Installment' AND i.[DownPayment] > 0
                    UNION ALL
                    SELECT c.[CustomerID], c.[CheckoutID], p.[PaymentDate],
                           p.[PaymentAmount], p.[PaymentMethod], 2, p.[PaymentID]
                    FROM [tblInstallmentPayment] p
                    INNER JOIN [tblInstallment] i ON i.[InstallmentID] = p.[InstallmentID]
                    INNER JOIN [tblCheckout] c ON c.[CheckoutID] = i.[CheckoutID]
                    WHERE p.[Status] = N'Paid' AND p.[PaymentAmount] > 0
                ) receipts
                ORDER BY [PurchaseDate], [CheckoutID], [SourceKind], [SourceID];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblCustomerPurchaseHistory");
        }
    }
}
