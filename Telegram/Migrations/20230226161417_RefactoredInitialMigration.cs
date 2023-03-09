using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telegram.Migrations.TelegramBotUsersDb
{
    public partial class RefactoredInitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.ChatId);
                });

            migrationBuilder.CreateTable(
                name: "MPlusIdentityEntity",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    TelegramBotUserChatId = table.Column<long>(type: "INTEGER", nullable: true),
                    SessionId = table.Column<string>(type: "TEXT", nullable: false),
                    AccessLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MPlusIdentityEntity", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_MPlusIdentityEntity_Users_TelegramBotUserChatId",
                        column: x => x.TelegramBotUserChatId,
                        principalTable: "Users",
                        principalColumn: "ChatId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MPlusIdentityEntity_TelegramBotUserChatId",
                table: "MPlusIdentityEntity",
                column: "TelegramBotUserChatId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MPlusIdentityEntity");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
