using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrialUsersMediator.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramTrialUsersSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrialUsers_Info",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrialUserIdentifier = table.Column<long>(type: "INTEGER", nullable: false),
                    TrialUserPlatform = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrialUsers_Info", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrialUsers_Info_TrialUsers_TrialUserIdentifier_TrialUserPlatform",
                        columns: x => new { x.TrialUserIdentifier, x.TrialUserPlatform },
                        principalTable: "TrialUsers",
                        principalColumns: new[] { "Identifier", "Platform" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrialUsers_Info_Telegram",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: true),
                    ProfilePicture = table.Column<string>(type: "TEXT", nullable: true),
                    AuthenticationDate = table.Column<long>(type: "INTEGER", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrialUsers_Info_Telegram", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrialUsers_Info_Telegram_TrialUsers_Info_Id",
                        column: x => x.Id,
                        principalTable: "TrialUsers_Info",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrialUsers_Info_TrialUserIdentifier_TrialUserPlatform",
                table: "TrialUsers_Info",
                columns: new[] { "TrialUserIdentifier", "TrialUserPlatform" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrialUsers_Info_Telegram");

            migrationBuilder.DropTable(
                name: "TrialUsers_Info");
        }
    }
}
