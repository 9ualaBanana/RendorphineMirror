using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telegram.Migrations.TelegramBotUsersDb
{
    public partial class MakeMPlusIdentityEntityFKAlsoBeItsPK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MPlusIdentityEntity",
                table: "MPlusIdentityEntity");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MPlusIdentityEntity",
                table: "MPlusIdentityEntity",
                column: "TelegramBotUserChatId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MPlusIdentityEntity",
                table: "MPlusIdentityEntity");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MPlusIdentityEntity",
                table: "MPlusIdentityEntity",
                column: "UserId");
        }
    }
}
