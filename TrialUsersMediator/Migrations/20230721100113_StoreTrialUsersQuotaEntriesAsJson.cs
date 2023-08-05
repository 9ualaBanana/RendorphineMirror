using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrialUsersMediator.Migrations
{
    /// <inheritdoc />
    public partial class StoreTrialUsersQuotaEntriesAsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrialUsers_QuotaEntries");

            migrationBuilder.AddColumn<string>(
                name: "Entries",
                table: "TrialUsers_Quota",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Entries",
                table: "TrialUsers_Quota");

            migrationBuilder.CreateTable(
                name: "TrialUsers_QuotaEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QuotaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false)
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
                name: "IX_TrialUsers_QuotaEntries_QuotaId",
                table: "TrialUsers_QuotaEntries",
                column: "QuotaId");
        }
    }
}
