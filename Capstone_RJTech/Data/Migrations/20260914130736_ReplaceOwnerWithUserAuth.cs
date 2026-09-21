using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Capstone_RJTech.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceOwnerWithUserAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- Phase 1: Drop foreign keys from all 28 business tables → Owners ---
            migrationBuilder.DropForeignKey(name: "FK_Deliveries_Owners_OwnerId", table: "Deliveries");
            migrationBuilder.DropForeignKey(name: "FK_DeliveryDetails_Owners_OwnerId", table: "DeliveryDetails");
            migrationBuilder.DropForeignKey(name: "FK_Notifications_Owners_OwnerId", table: "Notifications");
            migrationBuilder.DropForeignKey(name: "FK_ProductCategories_Owners_OwnerId", table: "ProductCategories");
            migrationBuilder.DropForeignKey(name: "FK_Products_Owners_OwnerId", table: "Products");
            migrationBuilder.DropForeignKey(name: "FK_tblBestSellingProduct_Owners_OwnerId", table: "tblBestSellingProduct");
            migrationBuilder.DropForeignKey(name: "FK_tblCheckout_Owners_OwnerId", table: "tblCheckout");
            migrationBuilder.DropForeignKey(name: "FK_tblCheckoutItem_Owners_OwnerId", table: "tblCheckoutItem");
            migrationBuilder.DropForeignKey(name: "FK_tblCustomer_Owners_OwnerId", table: "tblCustomer");
            migrationBuilder.DropForeignKey(name: "FK_tblCustomerPurchaseHistory_Owners_OwnerId", table: "tblCustomerPurchaseHistory");
            migrationBuilder.DropForeignKey(name: "FK_tblDeliveryOverview_Owners_OwnerId", table: "tblDeliveryOverview");
            migrationBuilder.DropForeignKey(name: "FK_tblDeliverySummary_Owners_OwnerId", table: "tblDeliverySummary");
            migrationBuilder.DropForeignKey(name: "FK_tblInstallment_Owners_OwnerId", table: "tblInstallment");
            migrationBuilder.DropForeignKey(name: "FK_tblInstallmentPayment_Owners_OwnerId", table: "tblInstallmentPayment");
            migrationBuilder.DropForeignKey(name: "FK_tblInventoryOverview_Owners_OwnerId", table: "tblInventoryOverview");
            migrationBuilder.DropForeignKey(name: "FK_tblLeastSellingProduct_Owners_OwnerId", table: "tblLeastSellingProduct");
            migrationBuilder.DropForeignKey(name: "FK_tblLeastStockedProduct_Owners_OwnerId", table: "tblLeastStockedProduct");
            migrationBuilder.DropForeignKey(name: "FK_tblMonthlySales_Owners_OwnerId", table: "tblMonthlySales");
            migrationBuilder.DropForeignKey(name: "FK_tblMostStockedProduct_Owners_OwnerId", table: "tblMostStockedProduct");
            migrationBuilder.DropForeignKey(name: "FK_tblProductCategoryOverview_Owners_OwnerId", table: "tblProductCategoryOverview");
            migrationBuilder.DropForeignKey(name: "FK_tblProductDeliverySummary_Owners_OwnerId", table: "tblProductDeliverySummary");
            migrationBuilder.DropForeignKey(name: "FK_tblProductStockSummary_Owners_OwnerId", table: "tblProductStockSummary");
            migrationBuilder.DropForeignKey(name: "FK_tblSalesByCategory_Owners_OwnerId", table: "tblSalesByCategory");
            migrationBuilder.DropForeignKey(name: "FK_tblSalesOverview_Owners_OwnerId", table: "tblSalesOverview");
            migrationBuilder.DropForeignKey(name: "FK_tblTransaction_Owners_OwnerId", table: "tblTransaction");
            migrationBuilder.DropForeignKey(name: "FK_tblWeeklySales_Owners_OwnerId", table: "tblWeeklySales");
            migrationBuilder.DropForeignKey(name: "FK_tblYearlySales_Owners_OwnerId", table: "tblYearlySales");

            // --- Phase 2: Create new role table and seed it ---
            migrationBuilder.CreateTable(
                name: "tblUserRole",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblUserRole", x => x.RoleID);
                });

            migrationBuilder.InsertData(
                table: "tblUserRole",
                columns: new[] { "RoleID", "RoleName" },
                values: new object[,]
                {
                    { 1, "Owner" },
                    { 2, "Staff" }
                });

            // --- Phase 3: Create tblUser and copy existing Owner data ---
            migrationBuilder.CreateTable(
                name: "tblUser",
                columns: table => new
                {
                    UserID = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblUser", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_tblUser_tblUserRole_Role",
                        column: x => x.Role,
                        principalTable: "tblUserRole",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblUser_Email",
                table: "tblUser",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblUser_Role",
                table: "tblUser",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_tblUser_Username",
                table: "tblUser",
                column: "Username",
                unique: true);

            // Copy existing Owners into the new tblUser table.
            // The OwnerID values are preserved so the OwnerId FK columns on the 28 business
            // tables remain valid after the FKs are re-pointed in Phase 5.
            migrationBuilder.Sql("""
                INSERT INTO [tblUser] ([UserID], [FullName], [Email], [Username], [Password], [Role], [DateCreated])
                SELECT
                    [OwnerID],
                    COALESCE([FullName], N''),
                    COALESCE([RecoveryEmail], N''),
                    [UserName],
                    [PasswordHash],
                    CASE WHEN [Role] = N'Owner' THEN 1 ELSE 2 END,
                    COALESCE([CreatedAt], SYSUTCDATETIME())
                FROM [Owners];
                """);

            // --- Phase 4: Create the password-reset table and drop old tables ---
            migrationBuilder.CreateTable(
                name: "tblPasswordResetCode",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OtpHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPasswordResetCode", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblPasswordResetCode_Email",
                table: "tblPasswordResetCode",
                column: "Email");

            migrationBuilder.DropTable(name: "Owners");
            migrationBuilder.DropTable(name: "tblOwnerInvitation");

            // --- Phase 5: Re-point the 28 business-table FKs from Owners → tblUser ---
            migrationBuilder.AddForeignKey(name: "FK_Deliveries_tblUser_OwnerId", table: "Deliveries", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_DeliveryDetails_tblUser_OwnerId", table: "DeliveryDetails", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_Notifications_tblUser_OwnerId", table: "Notifications", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_ProductCategories_tblUser_OwnerId", table: "ProductCategories", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_Products_tblUser_OwnerId", table: "Products", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblBestSellingProduct_tblUser_OwnerId", table: "tblBestSellingProduct", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblCheckout_tblUser_OwnerId", table: "tblCheckout", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblCheckoutItem_tblUser_OwnerId", table: "tblCheckoutItem", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblCustomer_tblUser_OwnerId", table: "tblCustomer", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblCustomerPurchaseHistory_tblUser_OwnerId", table: "tblCustomerPurchaseHistory", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblDeliveryOverview_tblUser_OwnerId", table: "tblDeliveryOverview", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblDeliverySummary_tblUser_OwnerId", table: "tblDeliverySummary", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblInstallment_tblUser_OwnerId", table: "tblInstallment", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblInstallmentPayment_tblUser_OwnerId", table: "tblInstallmentPayment", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblInventoryOverview_tblUser_OwnerId", table: "tblInventoryOverview", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblLeastSellingProduct_tblUser_OwnerId", table: "tblLeastSellingProduct", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblLeastStockedProduct_tblUser_OwnerId", table: "tblLeastStockedProduct", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblMonthlySales_tblUser_OwnerId", table: "tblMonthlySales", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblMostStockedProduct_tblUser_OwnerId", table: "tblMostStockedProduct", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblProductCategoryOverview_tblUser_OwnerId", table: "tblProductCategoryOverview", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblProductDeliverySummary_tblUser_OwnerId", table: "tblProductDeliverySummary", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblProductStockSummary_tblUser_OwnerId", table: "tblProductStockSummary", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblSalesByCategory_tblUser_OwnerId", table: "tblSalesByCategory", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblSalesOverview_tblUser_OwnerId", table: "tblSalesOverview", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblTransaction_tblUser_OwnerId", table: "tblTransaction", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblWeeklySales_tblUser_OwnerId", table: "tblWeeklySales", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_tblYearlySales_tblUser_OwnerId", table: "tblYearlySales", column: "OwnerId", principalTable: "tblUser", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_tblUser_OwnerId",
                table: "Deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryDetails_tblUser_OwnerId",
                table: "DeliveryDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_tblUser_OwnerId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategories_tblUser_OwnerId",
                table: "ProductCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_tblUser_OwnerId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_tblBestSellingProduct_tblUser_OwnerId",
                table: "tblBestSellingProduct");

            migrationBuilder.DropForeignKey(
                name: "FK_tblCheckout_tblUser_OwnerId",
                table: "tblCheckout");

            migrationBuilder.DropForeignKey(
                name: "FK_tblCheckoutItem_tblUser_OwnerId",
                table: "tblCheckoutItem");

            migrationBuilder.DropForeignKey(
                name: "FK_tblCustomer_tblUser_OwnerId",
                table: "tblCustomer");

            migrationBuilder.DropForeignKey(
                name: "FK_tblCustomerPurchaseHistory_tblUser_OwnerId",
                table: "tblCustomerPurchaseHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_tblDeliveryOverview_tblUser_OwnerId",
                table: "tblDeliveryOverview");

            migrationBuilder.DropForeignKey(
                name: "FK_tblDeliverySummary_tblUser_OwnerId",
                table: "tblDeliverySummary");

            migrationBuilder.DropForeignKey(
                name: "FK_tblInstallment_tblUser_OwnerId",
                table: "tblInstallment");

            migrationBuilder.DropForeignKey(
                name: "FK_tblInstallmentPayment_tblUser_OwnerId",
                table: "tblInstallmentPayment");

            migrationBuilder.DropForeignKey(
                name: "FK_tblInventoryOverview_tblUser_OwnerId",
                table: "tblInventoryOverview");

            migrationBuilder.DropForeignKey(
                name: "FK_tblLeastSellingProduct_tblUser_OwnerId",
                table: "tblLeastSellingProduct");

            migrationBuilder.DropForeignKey(
                name: "FK_tblLeastStockedProduct_tblUser_OwnerId",
                table: "tblLeastStockedProduct");

            migrationBuilder.DropForeignKey(
                name: "FK_tblMonthlySales_tblUser_OwnerId",
                table: "tblMonthlySales");

            migrationBuilder.DropForeignKey(
                name: "FK_tblMostStockedProduct_tblUser_OwnerId",
                table: "tblMostStockedProduct");

            migrationBuilder.DropForeignKey(
                name: "FK_tblProductCategoryOverview_tblUser_OwnerId",
                table: "tblProductCategoryOverview");

            migrationBuilder.DropForeignKey(
                name: "FK_tblProductDeliverySummary_tblUser_OwnerId",
                table: "tblProductDeliverySummary");

            migrationBuilder.DropForeignKey(
                name: "FK_tblProductStockSummary_tblUser_OwnerId",
                table: "tblProductStockSummary");

            migrationBuilder.DropForeignKey(
                name: "FK_tblSalesByCategory_tblUser_OwnerId",
                table: "tblSalesByCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_tblSalesOverview_tblUser_OwnerId",
                table: "tblSalesOverview");

            migrationBuilder.DropForeignKey(
                name: "FK_tblTransaction_tblUser_OwnerId",
                table: "tblTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_tblWeeklySales_tblUser_OwnerId",
                table: "tblWeeklySales");

            migrationBuilder.DropForeignKey(
                name: "FK_tblYearlySales_tblUser_OwnerId",
                table: "tblYearlySales");

            migrationBuilder.DropTable(
                name: "tblPasswordResetCode");

            migrationBuilder.DropTable(
                name: "tblUser");

            migrationBuilder.DropTable(
                name: "tblUserRole");

            migrationBuilder.CreateTable(
                name: "Owners",
                columns: table => new
                {
                    OwnerID = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecoveryEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Owners", x => x.OwnerID);
                });

            migrationBuilder.CreateTable(
                name: "tblOwnerInvitation",
                columns: table => new
                {
                    InvitationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByClerkUserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblOwnerInvitation", x => x.InvitationID);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_tblOwnerInvitation_Email_Status_ExpiresAt",
                table: "tblOwnerInvitation",
                columns: new[] { "Email", "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tblOwnerInvitation_TokenHash",
                table: "tblOwnerInvitation",
                column: "TokenHash",
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
    }
}
