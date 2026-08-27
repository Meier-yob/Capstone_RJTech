using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Capstone_RJTech.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialLocalDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Deliveries",
                columns: table => new
                {
                    delivery_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    date_delivered = table.Column<DateTime>(type: "datetime2", nullable: false),
                    received_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    batch_ID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_archived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deliveries", x => x.delivery_ID);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    category_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    category_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.category_ID);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleEvents",
                columns: table => new
                {
                    event_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    event_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleEvents", x => x.event_ID);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    product_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    product_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    product_brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    product_description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    product_image_path = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    product_quantity = table.Column<int>(type: "int", nullable: false),
                    reorder_level = table.Column<int>(type: "int", nullable: false),
                    Product_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    product_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    category_ID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.product_ID);
                    table.ForeignKey(
                        name: "FK_Products_ProductCategories_category_ID",
                        column: x => x.category_ID,
                        principalTable: "ProductCategories",
                        principalColumn: "category_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryDetails",
                columns: table => new
                {
                    deldetails_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    product_quantity = table.Column<int>(type: "int", nullable: false),
                    previous_quantity = table.Column<int>(type: "int", nullable: false),
                    new_quantity = table.Column<int>(type: "int", nullable: false),
                    product_ID = table.Column<int>(type: "int", nullable: false),
                    delivery_ID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryDetails", x => x.deldetails_ID);
                    table.ForeignKey(
                        name: "FK_DeliveryDetails_Deliveries_delivery_ID",
                        column: x => x.delivery_ID,
                        principalTable: "Deliveries",
                        principalColumn: "delivery_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryDetails_Products_product_ID",
                        column: x => x.product_ID,
                        principalTable: "Products",
                        principalColumn: "product_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    notification_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    product_ID = table.Column<int>(type: "int", nullable: true),
                    title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    notification_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    action_url = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_read = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.notification_ID);
                    table.ForeignKey(
                        name: "FK_Notifications_Products_product_ID",
                        column: x => x.product_ID,
                        principalTable: "Products",
                        principalColumn: "product_ID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "ProductCategories",
                columns: new[] { "category_ID", "category_name" },
                values: new object[,]
                {
                    { 1, "Monitors" },
                    { 2, "Mouses" },
                    { 3, "Keyboards" },
                    { 4, "Headsets" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "product_ID", "Product_price", "category_ID", "product_brand", "product_description", "product_image_path", "product_name", "product_quantity", "product_status", "reorder_level" },
                values: new object[,]
                {
                    { 1, 200.00m, 2, "A4 Tech", "Optical Wired Mouse", null, "Optical Wired Mouse", 0, "Unavailable", 5 },
                    { 2, 1200.00m, 3, "Logitech", "Mechanical Keyboard", null, "Mechanical Keyboard", 0, "Unavailable", 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_batch_ID",
                table: "Deliveries",
                column: "batch_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryDetails_delivery_ID",
                table: "DeliveryDetails",
                column: "delivery_ID");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryDetails_product_ID",
                table: "DeliveryDetails",
                column: "product_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_product_ID",
                table: "Notifications",
                column: "product_ID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_category_name",
                table: "ProductCategories",
                column: "category_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_category_ID_product_name_product_brand",
                table: "Products",
                columns: new[] { "category_ID", "product_name", "product_brand" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryDetails");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ScheduleEvents");

            migrationBuilder.DropTable(
                name: "Deliveries");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "ProductCategories");
        }
    }
}
