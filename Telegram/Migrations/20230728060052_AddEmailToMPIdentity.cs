using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telegram.Migrations.TelegramBotUsersDb
{
    /// <inheritdoc />
    public partial class AddEmailToMPIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MPlusIdentityEntity",
                table: "MPlusIdentityEntity");

            migrationBuilder.DropForeignKey(
                name: "PK_MPlusIdentityEntity",
                table: "MPlusIdentityEntity");

            migrationBuilder.RenameColumn(
                name: "TelegramBotUserChatId",
                table: "MPlusIdentityEntity",
                newName: "UserChatId");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "MPlusIdentityEntity",
                type: "TEXT",
                nullable: false);

            migrationBuilder.RenameTable(
                name: "MPlusIdentityEntity",
                newName: "MPIdentity");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MPIdentity",
                table: "MPIdentity",
                column: "UserChatId");

            migrationBuilder.AddForeignKey(
                name: "FK_MPIdentity_Users_UserChatId",
                table: "MPIdentity",
                column: "UserChatId",
                principalTable: "Users",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MPIdentity",
                table: "MPIdentity");

            migrationBuilder.DropForeignKey(
                name: "FK_MPIdentity_Users_UserChatId",
                table: "MPIdentity");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "MPIdentity");

            migrationBuilder.RenameTable(
                name: "MPIdentity",
                newName: "MPlusIdentityEntity");

            migrationBuilder.RenameColumn(
                name: "UserChatId",
                table: "MPlusIdentityEntity",
                newName: "TelegramBotUserChatId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MPlusIdentityEntity",
                table: "MPlusIdentityEntity",
                column: "TelegramBotUserChatId");

            migrationBuilder.AddForeignKey(
                name: "FK_MPlusIdentityEntity_Users_TelegramBotUserChatId",
                table: "MPlusIdentityEntity",
                column: "TelegramBotUserChatId",
                principalTable: "Users",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
