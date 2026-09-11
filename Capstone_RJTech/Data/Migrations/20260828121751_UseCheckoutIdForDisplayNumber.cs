using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_RJTech.Data.Migrations
{
    /// <inheritdoc />
    public partial class UseCheckoutIdForDisplayNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tblCheckout_CheckoutNumber",
                table: "tblCheckout");

            migrationBuilder.DropColumn(
                name: "CheckoutNumber",
                table: "tblCheckout");

            migrationBuilder.Sql(
                "DELETE FROM [DocumentSequences] WHERE [sequence_name] = N'checkout';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CheckoutNumber",
                table: "tblCheckout",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [tblCheckout] SET [CheckoutNumber] = [CheckoutID];");

            migrationBuilder.AlterColumn<int>(
                name: "CheckoutNumber",
                table: "tblCheckout",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblCheckout_CheckoutNumber",
                table: "tblCheckout",
                column: "CheckoutNumber",
                unique: true);
        }
    }
}
