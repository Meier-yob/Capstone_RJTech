using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_RJTech.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveScheduleEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove obsolete alerts so notification links cannot point to the retired page.
            migrationBuilder.Sql("""
                DELETE FROM [Notifications]
                WHERE [notification_type] = N'calendar'
                   OR [action_url] = N'/Notification/Calendar'
                   OR [action_url] LIKE N'/Notification/Calendar?%'
                   OR [action_url] LIKE N'/Notification/Calendar#%';
                """);

            migrationBuilder.DropTable(
                name: "ScheduleEvents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleEvents",
                columns: table => new
                {
                    event_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    end_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    event_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    start_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleEvents", x => x.event_ID);
                });
        }
    }
}
