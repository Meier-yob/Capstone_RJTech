using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_RJTech.Data.Migrations
{
    /// <inheritdoc />
    public partial class UseDeliveryIdForIdentifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [Deliveries]
                SET [batch_ID] = N'BATCH-' +
                    RIGHT(
                        N'000' + CONVERT(nvarchar(20), [delivery_ID]),
                        CASE
                            WHEN LEN(CONVERT(nvarchar(20), [delivery_ID])) > 3
                                THEN LEN(CONVERT(nvarchar(20), [delivery_ID]))
                            ELSE 3
                        END
                    );
                """);

            migrationBuilder.DropTable(
                name: "DocumentSequences");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
