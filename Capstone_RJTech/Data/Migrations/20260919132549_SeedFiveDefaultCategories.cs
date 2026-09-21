using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_RJTech.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedFiveDefaultCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seeds the five default product categories idempotently (by name) so that
            // fresh databases start with Monitors, Mouses, Keyboards, Headsets and
            // Computer Accessories, while existing databases simply gain any default
            // category they are still missing - without touching the user's catalog.
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [ProductCategories] WHERE [category_name] = N'Monitors')
                    INSERT INTO [ProductCategories] ([category_name]) VALUES (N'Monitors');
                IF NOT EXISTS (SELECT 1 FROM [ProductCategories] WHERE [category_name] = N'Mouses')
                    INSERT INTO [ProductCategories] ([category_name]) VALUES (N'Mouses');
                IF NOT EXISTS (SELECT 1 FROM [ProductCategories] WHERE [category_name] = N'Keyboards')
                    INSERT INTO [ProductCategories] ([category_name]) VALUES (N'Keyboards');
                IF NOT EXISTS (SELECT 1 FROM [ProductCategories] WHERE [category_name] = N'Headsets')
                    INSERT INTO [ProductCategories] ([category_name]) VALUES (N'Headsets');
                IF NOT EXISTS (SELECT 1 FROM [ProductCategories] WHERE [category_name] = N'Computer Accessories')
                    INSERT INTO [ProductCategories] ([category_name]) VALUES (N'Computer Accessories');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM [ProductCategories] WHERE [category_name] IN (N'Monitors', N'Mouses', N'Keyboards', N'Headsets', N'Computer Accessories');
                """);
        }
    }
}