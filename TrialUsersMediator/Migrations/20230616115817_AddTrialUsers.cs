using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrialUsersMediator.Migrations
{
    /// <inheritdoc />
    public partial class AddTrialUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrialUsers",
                columns: table => new
                {
                    Identifier = table.Column<long>(type: "INTEGER", nullable: false),
                    Platform = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrialUsers", x => new { x.Identifier, x.Platform });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrialUsers");
        }
    }
}
