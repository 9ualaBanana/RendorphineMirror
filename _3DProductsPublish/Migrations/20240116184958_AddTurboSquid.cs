using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3DProductsPublish.Migrations
{
    /// <inheritdoc />
    public partial class AddTurboSquid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TS_User",
                columns: table => new
                {
                    Email = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Email", x => x.Email);
                });

            migrationBuilder.CreateTable(
                name: "TS_ScanPeriod",
                columns: table => new
                {
                    Start = table.Column<long>(type: "INTEGER", nullable: false),
                    End = table.Column<long>(type: "INTEGER", nullable: false),
                    UserEmail = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TS_ScanPeriod", x => new { x.Start, x.End });
                    table.ForeignKey(
                        name: "FK_TS_ScanPeriod_TS_User_UserEmail",
                        column: x => x.UserEmail,
                        principalTable: "TS_User",
                        principalColumn: "Email",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TS_ScanPeriod_UserEmail",
                table: "TS_ScanPeriod",
                column: "UserEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TS_ScanPeriod");

            migrationBuilder.DropTable(
                name: "TS_User");
        }
    }
}
