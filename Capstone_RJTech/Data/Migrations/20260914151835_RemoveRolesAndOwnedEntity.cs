using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Capstone_RJTech.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRolesAndOwnedEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "FK_tblUser_tblUserRole_Role",
                table: "tblUser");

            migrationBuilder.DropForeignKey(
                name: "FK_tblWeeklySales_tblUser_OwnerId",
                table: "tblWeeklySales");

            migrationBuilder.DropForeignKey(
                name: "FK_tblYearlySales_tblUser_OwnerId",
                table: "tblYearlySales");

            migrationBuilder.DropTable(
                name: "tblUserRole");

            migrationBuilder.DropIndex(
                name: "IX_tblYearlySales_OwnerId",
                table: "tblYearlySales");

            migrationBuilder.DropIndex(
                name: "IX_tblWeeklySales_OwnerId",
                table: "tblWeeklySales");

            migrationBuilder.DropIndex(
                name: "IX_tblUser_Role",
                table: "tblUser");

            migrationBuilder.DropIndex(
                name: "IX_tblTransaction_OwnerId",
                table: "tblTransaction");

            migrationBuilder.DropIndex(
                name: "IX_tblSalesOverview_OwnerId",
                table: "tblSalesOverview");

            migrationBuilder.DropIndex(
                name: "IX_tblSalesByCategory_OwnerId",
                table: "tblSalesByCategory");

            migrationBuilder.DropIndex(
                name: "IX_tblProductStockSummary_OwnerId",
                table: "tblProductStockSummary");

            migrationBuilder.DropIndex(
                name: "IX_tblProductDeliverySummary_OwnerId",
                table: "tblProductDeliverySummary");

            migrationBuilder.DropIndex(
                name: "IX_tblProductCategoryOverview_OwnerId",
                table: "tblProductCategoryOverview");

            migrationBuilder.DropIndex(
                name: "IX_tblMostStockedProduct_OwnerId",
                table: "tblMostStockedProduct");

            migrationBuilder.DropIndex(
                name: "IX_tblMonthlySales_OwnerId",
                table: "tblMonthlySales");

            migrationBuilder.DropIndex(
                name: "IX_tblLeastStockedProduct_OwnerId",
                table: "tblLeastStockedProduct");

            migrationBuilder.DropIndex(
                name: "IX_tblLeastSellingProduct_OwnerId",
                table: "tblLeastSellingProduct");

            migrationBuilder.DropIndex(
                name: "IX_tblInventoryOverview_OwnerId",
                table: "tblInventoryOverview");

            migrationBuilder.DropIndex(
                name: "IX_tblInstallmentPayment_OwnerId",
                table: "tblInstallmentPayment");

            migrationBuilder.DropIndex(
                name: "IX_tblInstallment_OwnerId",
                table: "tblInstallment");

            migrationBuilder.DropIndex(
                name: "IX_tblDeliverySummary_OwnerId",
                table: "tblDeliverySummary");

            migrationBuilder.DropIndex(
                name: "IX_tblDeliveryOverview_OwnerId",
                table: "tblDeliveryOverview");

            migrationBuilder.DropIndex(
                name: "IX_tblCustomerPurchaseHistory_OwnerId",
                table: "tblCustomerPurchaseHistory");

            migrationBuilder.DropIndex(
                name: "IX_tblCustomer_OwnerId",
                table: "tblCustomer");

            migrationBuilder.DropIndex(
                name: "IX_tblCheckoutItem_OwnerId",
                table: "tblCheckoutItem");

            migrationBuilder.DropIndex(
                name: "IX_tblCheckout_OwnerId",
                table: "tblCheckout");

            migrationBuilder.DropIndex(
                name: "IX_tblBestSellingProduct_OwnerId",
                table: "tblBestSellingProduct");

            migrationBuilder.DropIndex(
                name: "IX_Products_OwnerId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductCategories_OwnerId",
                table: "ProductCategories");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_OwnerId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryDetails_OwnerId",
                table: "DeliveryDetails");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_OwnerId",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblYearlySales");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblWeeklySales");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "tblUser");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblTransaction");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblSalesOverview");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblSalesByCategory");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblProductStockSummary");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblProductDeliverySummary");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblProductCategoryOverview");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblMostStockedProduct");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblMonthlySales");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblLeastStockedProduct");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblLeastSellingProduct");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblInventoryOverview");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblInstallmentPayment");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblInstallment");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblDeliverySummary");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblDeliveryOverview");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblCustomerPurchaseHistory");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblCustomer");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblCheckoutItem");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblCheckout");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tblBestSellingProduct");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "DeliveryDetails");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Deliveries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblYearlySales",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblWeeklySales",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "tblUser",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblTransaction",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblSalesOverview",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblSalesByCategory",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblProductStockSummary",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblProductDeliverySummary",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblProductCategoryOverview",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblMostStockedProduct",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblMonthlySales",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblLeastStockedProduct",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblLeastSellingProduct",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblInventoryOverview",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblInstallmentPayment",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblInstallment",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblDeliverySummary",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblDeliveryOverview",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblCustomerPurchaseHistory",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblCustomer",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblCheckoutItem",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblCheckout",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tblBestSellingProduct",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Products",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "ProductCategories",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Notifications",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "DeliveryDetails",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Deliveries",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "category_ID",
                keyValue: 1,
                column: "OwnerId",
                value: "");

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "category_ID",
                keyValue: 2,
                column: "OwnerId",
                value: "");

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "category_ID",
                keyValue: 3,
                column: "OwnerId",
                value: "");

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "category_ID",
                keyValue: 4,
                column: "OwnerId",
                value: "");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "product_ID",
                keyValue: 1,
                column: "OwnerId",
                value: "");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "product_ID",
                keyValue: 2,
                column: "OwnerId",
                value: "");

            migrationBuilder.InsertData(
                table: "tblUserRole",
                columns: new[] { "RoleID", "RoleName" },
                values: new object[,]
                {
                    { 1, "Owner" },
                    { 2, "Staff" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblYearlySales_OwnerId",
                table: "tblYearlySales",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblWeeklySales_OwnerId",
                table: "tblWeeklySales",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblUser_Role",
                table: "tblUser",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_tblTransaction_OwnerId",
                table: "tblTransaction",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblSalesOverview_OwnerId",
                table: "tblSalesOverview",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblSalesByCategory_OwnerId",
                table: "tblSalesByCategory",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblProductStockSummary_OwnerId",
                table: "tblProductStockSummary",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblProductDeliverySummary_OwnerId",
                table: "tblProductDeliverySummary",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblProductCategoryOverview_OwnerId",
                table: "tblProductCategoryOverview",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblMostStockedProduct_OwnerId",
                table: "tblMostStockedProduct",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblMonthlySales_OwnerId",
                table: "tblMonthlySales",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblLeastStockedProduct_OwnerId",
                table: "tblLeastStockedProduct",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblLeastSellingProduct_OwnerId",
                table: "tblLeastSellingProduct",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblInventoryOverview_OwnerId",
                table: "tblInventoryOverview",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblInstallmentPayment_OwnerId",
                table: "tblInstallmentPayment",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblInstallment_OwnerId",
                table: "tblInstallment",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblDeliverySummary_OwnerId",
                table: "tblDeliverySummary",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblDeliveryOverview_OwnerId",
                table: "tblDeliveryOverview",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerPurchaseHistory_OwnerId",
                table: "tblCustomerPurchaseHistory",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomer_OwnerId",
                table: "tblCustomer",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblCheckoutItem_OwnerId",
                table: "tblCheckoutItem",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblCheckout_OwnerId",
                table: "tblCheckout",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblBestSellingProduct_OwnerId",
                table: "tblBestSellingProduct",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_OwnerId",
                table: "Products",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_OwnerId",
                table: "ProductCategories",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OwnerId",
                table: "Notifications",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryDetails_OwnerId",
                table: "DeliveryDetails",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_OwnerId",
                table: "Deliveries",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_tblUser_OwnerId",
                table: "Deliveries",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryDetails_tblUser_OwnerId",
                table: "DeliveryDetails",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_tblUser_OwnerId",
                table: "Notifications",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_tblUser_OwnerId",
                table: "ProductCategories",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_tblUser_OwnerId",
                table: "Products",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblBestSellingProduct_tblUser_OwnerId",
                table: "tblBestSellingProduct",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblCheckout_tblUser_OwnerId",
                table: "tblCheckout",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblCheckoutItem_tblUser_OwnerId",
                table: "tblCheckoutItem",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblCustomer_tblUser_OwnerId",
                table: "tblCustomer",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblCustomerPurchaseHistory_tblUser_OwnerId",
                table: "tblCustomerPurchaseHistory",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblDeliveryOverview_tblUser_OwnerId",
                table: "tblDeliveryOverview",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblDeliverySummary_tblUser_OwnerId",
                table: "tblDeliverySummary",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblInstallment_tblUser_OwnerId",
                table: "tblInstallment",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblInstallmentPayment_tblUser_OwnerId",
                table: "tblInstallmentPayment",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblInventoryOverview_tblUser_OwnerId",
                table: "tblInventoryOverview",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblLeastSellingProduct_tblUser_OwnerId",
                table: "tblLeastSellingProduct",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblLeastStockedProduct_tblUser_OwnerId",
                table: "tblLeastStockedProduct",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblMonthlySales_tblUser_OwnerId",
                table: "tblMonthlySales",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblMostStockedProduct_tblUser_OwnerId",
                table: "tblMostStockedProduct",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblProductCategoryOverview_tblUser_OwnerId",
                table: "tblProductCategoryOverview",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblProductDeliverySummary_tblUser_OwnerId",
                table: "tblProductDeliverySummary",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblProductStockSummary_tblUser_OwnerId",
                table: "tblProductStockSummary",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblSalesByCategory_tblUser_OwnerId",
                table: "tblSalesByCategory",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblSalesOverview_tblUser_OwnerId",
                table: "tblSalesOverview",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblTransaction_tblUser_OwnerId",
                table: "tblTransaction",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblUser_tblUserRole_Role",
                table: "tblUser",
                column: "Role",
                principalTable: "tblUserRole",
                principalColumn: "RoleID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblWeeklySales_tblUser_OwnerId",
                table: "tblWeeklySales",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblYearlySales_tblUser_OwnerId",
                table: "tblYearlySales",
                column: "OwnerId",
                principalTable: "tblUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
