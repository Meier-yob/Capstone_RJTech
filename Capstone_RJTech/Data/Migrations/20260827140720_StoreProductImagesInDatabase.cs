using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_RJTech.Data.Migrations
{
    /// <inheritdoc />
    public partial class StoreProductImagesInDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "product_image_path",
                table: "Products");

            migrationBuilder.AddColumn<byte[]>(
                name: "product_Image",
                table: "Products",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "product_ImageContentType",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "product_ID",
                keyValue: 1,
                columns: new[] { "product_Image", "product_ImageContentType" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "product_ID",
                keyValue: 2,
                columns: new[] { "product_Image", "product_ImageContentType" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "product_Image",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "product_ImageContentType",
                table: "Products");

            migrationBuilder.AddColumn<string>(
                name: "product_image_path",
                table: "Products",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "product_ID",
                keyValue: 1,
                column: "product_image_path",
                value: null);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "product_ID",
                keyValue: 2,
                column: "product_image_path",
                value: null);
        }
    }
}
