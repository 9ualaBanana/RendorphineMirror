using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3DProductsPublish.Migrations
{
    /// <inheritdoc />
    public partial class AddScanPeriod_IsAnalyzed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAnalyzed",
                table: "TS_ScanPeriod",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAnalyzed",
                table: "TS_ScanPeriod");
        }
    }
}
