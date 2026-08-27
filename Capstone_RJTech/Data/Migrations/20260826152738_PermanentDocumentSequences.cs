using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_RJTech.Data.Migrations
{
    /// <inheritdoc />
    public partial class PermanentDocumentSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CheckoutNumber",
                table: "tblCheckout",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DocumentSequences",
                columns: table => new
                {
                    sequence_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    last_value = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentSequences", x => x.sequence_name);
                });

            migrationBuilder.Sql(
                """
                WITH NumberedCheckouts AS
                (
                    SELECT CheckoutID,
                           ROW_NUMBER() OVER (ORDER BY DatePurchased, CheckoutID) AS SequenceNumber
                    FROM tblCheckout
                )
                UPDATE checkoutRow
                SET CheckoutNumber = numbered.SequenceNumber
                FROM tblCheckout AS checkoutRow
                INNER JOIN NumberedCheckouts AS numbered
                    ON numbered.CheckoutID = checkoutRow.CheckoutID;

                INSERT INTO DocumentSequences (sequence_name, last_value)
                SELECT 'checkout', ISNULL(MAX(CheckoutNumber), 0)
                FROM tblCheckout;

                INSERT INTO DocumentSequences (sequence_name, last_value)
                SELECT 'delivery:' + SUBSTRING(batch_ID, 7, 8),
                       MAX(TRY_CONVERT(int, SUBSTRING(batch_ID, 16, 100)))
                FROM Deliveries
                WHERE batch_ID LIKE 'BATCH-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]-%'
                  AND TRY_CONVERT(int, SUBSTRING(batch_ID, 16, 100)) IS NOT NULL
                GROUP BY SUBSTRING(batch_ID, 7, 8);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_tblCheckout_CheckoutNumber",
                table: "tblCheckout",
                column: "CheckoutNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentSequences");

            migrationBuilder.DropIndex(
                name: "IX_tblCheckout_CheckoutNumber",
                table: "tblCheckout");

            migrationBuilder.DropColumn(
                name: "CheckoutNumber",
                table: "tblCheckout");
        }
    }
}
