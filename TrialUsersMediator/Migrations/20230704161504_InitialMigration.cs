using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrialUsersMediator.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
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
                });

            migrationBuilder.CreateTable(
                name: "TrialUsers_Quota",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrialUserIdentifier = table.Column<long>(type: "INTEGER", nullable: false),
                    TrialUserPlatform = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrialUsers_Quota", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrialUsers_Quota_TrialUsers_TrialUserIdentifier_TrialUserPlatform",
                        columns: x => new { x.TrialUserIdentifier, x.TrialUserPlatform },
                        principalTable: "TrialUsers",
                        principalColumns: new[] { "Identifier", "Platform" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrialUsers_Info",
                columns: table => new
                {
                    TrialUserIdentifier = table.Column<long>(type: "INTEGER", nullable: false),
                    TrialUserPlatform = table.Column<int>(type: "INTEGER", nullable: false),
                    TelegramInfoId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrialUsers_Info", x => new { x.TrialUserIdentifier, x.TrialUserPlatform });
                    table.ForeignKey(
                        name: "FK_TrialUsers_Info_TrialUsers_Info_Telegram_TelegramInfoId",
                        column: x => x.TelegramInfoId,
                        principalTable: "TrialUsers_Info_Telegram",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrialUsers_Info_TrialUsers_TrialUserIdentifier_TrialUserPlatform",
                        columns: x => new { x.TrialUserIdentifier, x.TrialUserPlatform },
                        principalTable: "TrialUsers",
                        principalColumns: new[] { "Identifier", "Platform" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrialUsers_QuotaEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false),
                    QuotaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrialUsers_QuotaEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrialUsers_QuotaEntries_TrialUsers_Quota_QuotaId",
                        column: x => x.QuotaId,
                        principalTable: "TrialUsers_Quota",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrialUsers_Info_TelegramInfoId",
                table: "TrialUsers_Info",
                column: "TelegramInfoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrialUsers_Quota_TrialUserIdentifier_TrialUserPlatform",
                table: "TrialUsers_Quota",
                columns: new[] { "TrialUserIdentifier", "TrialUserPlatform" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrialUsers_QuotaEntries_QuotaId",
                table: "TrialUsers_QuotaEntries",
                column: "QuotaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrialUsers_Info");

            migrationBuilder.DropTable(
                name: "TrialUsers_QuotaEntries");

            migrationBuilder.DropTable(
                name: "TrialUsers_Info_Telegram");

            migrationBuilder.DropTable(
                name: "TrialUsers_Quota");

            migrationBuilder.DropTable(
                name: "TrialUsers");
        }
    }
}
