using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_RJTech.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIdentityWithOwnerAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_AspNetUsers_OwnerId",
                table: "Deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryDetails_AspNetUsers_OwnerId",
                table: "DeliveryDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_OwnerId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategories_AspNetUsers_OwnerId",
                table: "ProductCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_AspNetUsers_OwnerId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_tblBestSellingProduct_AspNetUsers_OwnerId",
                table: "tblBestSellingProduct");

            migrationBuilder.DropForeignKey(
                name: "FK_tblCheckout_AspNetUsers_OwnerId",
                table: "tblCheckout");

            migrationBuilder.DropForeignKey(
                name: "FK_tblCheckoutItem_AspNetUsers_OwnerId",
                table: "tblCheckoutItem");

            migrationBuilder.DropForeignKey(
                name: "FK_tblCustomer_AspNetUsers_OwnerId",
                table: "tblCustomer");

            migrationBuilder.DropForeignKey(
                name: "FK_tblCustomerPurchaseHistory_AspNetUsers_OwnerId",
                table: "tblCustomerPurchaseHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_tblDeliveryOverview_AspNetUsers_OwnerId",
                table: "tblDeliveryOverview");

            migrationBuilder.DropForeignKey(
                name: "FK_tblDeliverySummary_AspNetUsers_OwnerId",
                table: "tblDeliverySummary");

            migrationBuilder.DropForeignKey(
                name: "FK_tblInstallment_AspNetUsers_OwnerId",
                table: "tblInstallment");

            migrationBuilder.DropForeignKey(
                name: "FK_tblInstallmentPayment_AspNetUsers_OwnerId",
                table: "tblInstallmentPayment");

            migrationBuilder.DropForeignKey(
                name: "FK_tblInventoryOverview_AspNetUsers_OwnerId",
                table: "tblInventoryOverview");

            migrationBuilder.DropForeignKey(
                name: "FK_tblLeastSellingProduct_AspNetUsers_OwnerId",
                table: "tblLeastSellingProduct");

            migrationBuilder.DropForeignKey(
                name: "FK_tblLeastStockedProduct_AspNetUsers_OwnerId",
                table: "tblLeastStockedProduct");

            migrationBuilder.DropForeignKey(
                name: "FK_tblMonthlySales_AspNetUsers_OwnerId",
                table: "tblMonthlySales");

            migrationBuilder.DropForeignKey(
                name: "FK_tblMostStockedProduct_AspNetUsers_OwnerId",
                table: "tblMostStockedProduct");

            migrationBuilder.DropForeignKey(
                name: "FK_tblProductCategoryOverview_AspNetUsers_OwnerId",
                table: "tblProductCategoryOverview");

            migrationBuilder.DropForeignKey(
                name: "FK_tblProductDeliverySummary_AspNetUsers_OwnerId",
                table: "tblProductDeliverySummary");

            migrationBuilder.DropForeignKey(
                name: "FK_tblProductStockSummary_AspNetUsers_OwnerId",
                table: "tblProductStockSummary");

            migrationBuilder.DropForeignKey(
                name: "FK_tblSalesByCategory_AspNetUsers_OwnerId",
                table: "tblSalesByCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_tblSalesOverview_AspNetUsers_OwnerId",
                table: "tblSalesOverview");

            migrationBuilder.DropForeignKey(
                name: "FK_tblTransaction_AspNetUsers_OwnerId",
                table: "tblTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_tblWeeklySales_AspNetUsers_OwnerId",
                table: "tblWeeklySales");

            migrationBuilder.DropForeignKey(
                name: "FK_tblYearlySales_AspNetUsers_OwnerId",
                table: "tblYearlySales");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AccessFailedCount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LockoutEnabled",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NormalizedUserName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhoneNumberConfirmed",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "Owners");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "Owners",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Owners",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerID",
                table: "Owners",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecoveryEmail",
                table: "Owners",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Owners",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Owners",
                table: "Owners",
                column: "OwnerID");

            migrationBuilder.CreateIndex(
                name: "IX_Owners_RecoveryEmail",
                table: "Owners",
                column: "RecoveryEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Owners_UserName",
                table: "Owners",
                column: "UserName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_Owners_OwnerId",
                table: "Deliveries",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryDetails_Owners_OwnerId",
                table: "DeliveryDetails",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Owners_OwnerId",
                table: "Notifications",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_Owners_OwnerId",
                table: "ProductCategories",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Owners_OwnerId",
                table: "Products",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblBestSellingProduct_Owners_OwnerId",
                table: "tblBestSellingProduct",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblCheckout_Owners_OwnerId",
                table: "tblCheckout",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblCheckoutItem_Owners_OwnerId",
                table: "tblCheckoutItem",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblCustomer_Owners_OwnerId",
                table: "tblCustomer",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblCustomerPurchaseHistory_Owners_OwnerId",
                table: "tblCustomerPurchaseHistory",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblDeliveryOverview_Owners_OwnerId",
                table: "tblDeliveryOverview",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblDeliverySummary_Owners_OwnerId",
                table: "tblDeliverySummary",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblInstallment_Owners_OwnerId",
                table: "tblInstallment",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblInstallmentPayment_Owners_OwnerId",
                table: "tblInstallmentPayment",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblInventoryOverview_Owners_OwnerId",
                table: "tblInventoryOverview",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblLeastSellingProduct_Owners_OwnerId",
                table: "tblLeastSellingProduct",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblLeastStockedProduct_Owners_OwnerId",
                table: "tblLeastStockedProduct",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblMonthlySales_Owners_OwnerId",
                table: "tblMonthlySales",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblMostStockedProduct_Owners_OwnerId",
                table: "tblMostStockedProduct",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblProductCategoryOverview_Owners_OwnerId",
                table: "tblProductCategoryOverview",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblProductDeliverySummary_Owners_OwnerId",
                table: "tblProductDeliverySummary",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblProductStockSummary_Owners_OwnerId",
                table: "tblProductStockSummary",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblSalesByCategory_Owners_OwnerId",
                table: "tblSalesByCategory",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblSalesOverview_Owners_OwnerId",
                table: "tblSalesOverview",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblTransaction_Owners_OwnerId",
                table: "tblTransaction",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblWeeklySales_Owners_OwnerId",
                table: "tblWeeklySales",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblYearlySales_Owners_OwnerId",
                table: "tblYearlySales",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "OwnerID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_Owners_OwnerId",
                table: "Deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryDetails_Owners_OwnerId",
                table: "DeliveryDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Owners_OwnerId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategories_Owners_OwnerId",
                table: "ProductCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Owners_OwnerId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_tblBestSellingProduct_Owners_OwnerId",
                table: "tblBestSellingProduct");

            migrationBuilder.DropForeignKey(
                name: "FK_tblCheckout_Owners_OwnerId",
                table: "tblCheckout");

            migrationBuilder.DropForeignKey(
                name: "FK_tblCheckoutItem_Owners_OwnerId",
                table: "tblCheckoutItem");

            migrationBuilder.DropForeignKey(
                name: "FK_tblCustomer_Owners_OwnerId",
                table: "tblCustomer");

            migrationBuilder.DropForeignKey(
                name: "FK_tblCustomerPurchaseHistory_Owners_OwnerId",
                table: "tblCustomerPurchaseHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_tblDeliveryOverview_Owners_OwnerId",
                table: "tblDeliveryOverview");

            migrationBuilder.DropForeignKey(
                name: "FK_tblDeliverySummary_Owners_OwnerId",
                table: "tblDeliverySummary");

            migrationBuilder.DropForeignKey(
                name: "FK_tblInstallment_Owners_OwnerId",
                table: "tblInstallment");

            migrationBuilder.DropForeignKey(
                name: "FK_tblInstallmentPayment_Owners_OwnerId",
                table: "tblInstallmentPayment");

            migrationBuilder.DropForeignKey(
                name: "FK_tblInventoryOverview_Owners_OwnerId",
                table: "tblInventoryOverview");

            migrationBuilder.DropForeignKey(
                name: "FK_tblLeastSellingProduct_Owners_OwnerId",
                table: "tblLeastSellingProduct");

            migrationBuilder.DropForeignKey(
                name: "FK_tblLeastStockedProduct_Owners_OwnerId",
                table: "tblLeastStockedProduct");

            migrationBuilder.DropForeignKey(
                name: "FK_tblMonthlySales_Owners_OwnerId",
                table: "tblMonthlySales");

            migrationBuilder.DropForeignKey(
                name: "FK_tblMostStockedProduct_Owners_OwnerId",
                table: "tblMostStockedProduct");

            migrationBuilder.DropForeignKey(
                name: "FK_tblProductCategoryOverview_Owners_OwnerId",
                table: "tblProductCategoryOverview");

            migrationBuilder.DropForeignKey(
                name: "FK_tblProductDeliverySummary_Owners_OwnerId",
                table: "tblProductDeliverySummary");

            migrationBuilder.DropForeignKey(
                name: "FK_tblProductStockSummary_Owners_OwnerId",
                table: "tblProductStockSummary");

            migrationBuilder.DropForeignKey(
                name: "FK_tblSalesByCategory_Owners_OwnerId",
                table: "tblSalesByCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_tblSalesOverview_Owners_OwnerId",
                table: "tblSalesOverview");

            migrationBuilder.DropForeignKey(
                name: "FK_tblTransaction_Owners_OwnerId",
                table: "tblTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_tblWeeklySales_Owners_OwnerId",
                table: "tblWeeklySales");

            migrationBuilder.DropForeignKey(
                name: "FK_tblYearlySales_Owners_OwnerId",
                table: "tblYearlySales");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Owners",
                table: "Owners");

            migrationBuilder.DropIndex(
                name: "IX_Owners_RecoveryEmail",
                table: "Owners");

            migrationBuilder.DropIndex(
                name: "IX_Owners_UserName",
                table: "Owners");

            migrationBuilder.DropColumn(
                name: "OwnerID",
                table: "Owners");

            migrationBuilder.DropColumn(
                name: "RecoveryEmail",
                table: "Owners");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Owners");

            migrationBuilder.RenameTable(
                name: "Owners",
                newName: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AccessFailedCount",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LockoutEnabled",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUserName",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_AspNetUsers_OwnerId",
                table: "Deliveries",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryDetails_AspNetUsers_OwnerId",
                table: "DeliveryDetails",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_OwnerId",
                table: "Notifications",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_AspNetUsers_OwnerId",
                table: "ProductCategories",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_AspNetUsers_OwnerId",
                table: "Products",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblBestSellingProduct_AspNetUsers_OwnerId",
                table: "tblBestSellingProduct",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblCheckout_AspNetUsers_OwnerId",
                table: "tblCheckout",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblCheckoutItem_AspNetUsers_OwnerId",
                table: "tblCheckoutItem",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblCustomer_AspNetUsers_OwnerId",
                table: "tblCustomer",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblCustomerPurchaseHistory_AspNetUsers_OwnerId",
                table: "tblCustomerPurchaseHistory",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblDeliveryOverview_AspNetUsers_OwnerId",
                table: "tblDeliveryOverview",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblDeliverySummary_AspNetUsers_OwnerId",
                table: "tblDeliverySummary",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblInstallment_AspNetUsers_OwnerId",
                table: "tblInstallment",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblInstallmentPayment_AspNetUsers_OwnerId",
                table: "tblInstallmentPayment",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblInventoryOverview_AspNetUsers_OwnerId",
                table: "tblInventoryOverview",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblLeastSellingProduct_AspNetUsers_OwnerId",
                table: "tblLeastSellingProduct",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblLeastStockedProduct_AspNetUsers_OwnerId",
                table: "tblLeastStockedProduct",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblMonthlySales_AspNetUsers_OwnerId",
                table: "tblMonthlySales",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblMostStockedProduct_AspNetUsers_OwnerId",
                table: "tblMostStockedProduct",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblProductCategoryOverview_AspNetUsers_OwnerId",
                table: "tblProductCategoryOverview",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblProductDeliverySummary_AspNetUsers_OwnerId",
                table: "tblProductDeliverySummary",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblProductStockSummary_AspNetUsers_OwnerId",
                table: "tblProductStockSummary",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblSalesByCategory_AspNetUsers_OwnerId",
                table: "tblSalesByCategory",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblSalesOverview_AspNetUsers_OwnerId",
                table: "tblSalesOverview",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblTransaction_AspNetUsers_OwnerId",
                table: "tblTransaction",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblWeeklySales_AspNetUsers_OwnerId",
                table: "tblWeeklySales",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblYearlySales_AspNetUsers_OwnerId",
                table: "tblYearlySales",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
