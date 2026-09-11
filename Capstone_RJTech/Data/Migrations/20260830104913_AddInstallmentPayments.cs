using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_RJTech.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallmentPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tblCheckoutItem_SerialNo",
                table: "tblCheckoutItem");

            migrationBuilder.AddColumn<string>(
                name: "PaymentType",
                table: "tblCheckout",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Full Payment");

            migrationBuilder.Sql(
                "UPDATE [tblCheckout] SET [Status] = N'Paid' WHERE [Status] = N'Completed';");

            migrationBuilder.Sql(
                "UPDATE [tblCheckout] SET [PaymentMethod] = N'Cash' WHERE [PaymentMethod] NOT IN (N'Cash', N'GCash', N'Maya');");

            migrationBuilder.CreateTable(
                name: "tblInstallment",
                columns: table => new
                {
                    InstallmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckoutID = table.Column<int>(type: "int", nullable: false),
                    Months = table.Column<int>(type: "int", nullable: false),
                    DownPayment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MonthlyPayment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MonthsPaid = table.Column<int>(type: "int", nullable: false),
                    MonthsRemaining = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblInstallment", x => x.InstallmentID);
                    table.ForeignKey(
                        name: "FK_tblInstallment_tblCheckout_CheckoutID",
                        column: x => x.CheckoutID,
                        principalTable: "tblCheckout",
                        principalColumn: "CheckoutID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblInstallmentPayment",
                columns: table => new
                {
                    PaymentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstallmentID = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblInstallmentPayment", x => x.PaymentID);
                    table.ForeignKey(
                        name: "FK_tblInstallmentPayment_tblInstallment_InstallmentID",
                        column: x => x.InstallmentID,
                        principalTable: "tblInstallment",
                        principalColumn: "InstallmentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblCheckoutItem_SerialNo",
                table: "tblCheckoutItem",
                column: "SerialNo",
                filter: "[SerialNo] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tblInstallment_CheckoutID",
                table: "tblInstallment",
                column: "CheckoutID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblInstallmentPayment_InstallmentID",
                table: "tblInstallmentPayment",
                column: "InstallmentID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblInstallmentPayment");

            migrationBuilder.DropTable(
                name: "tblInstallment");

            migrationBuilder.DropIndex(
                name: "IX_tblCheckoutItem_SerialNo",
                table: "tblCheckoutItem");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "tblCheckout");

            migrationBuilder.CreateIndex(
                name: "IX_tblCheckoutItem_SerialNo",
                table: "tblCheckoutItem",
                column: "SerialNo",
                unique: true,
                filter: "[SerialNo] IS NOT NULL");
        }
    }
}
