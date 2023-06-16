using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrialUsersMediator.Migrations
{
    /// <inheritdoc />
    public partial class AddTrialUserQuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityIdentifier = table.Column<long>(type: "INTEGER", nullable: false),
                    EntityPlatform = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotas_TrialUsers_EntityIdentifier_EntityPlatform",
                        columns: x => new { x.EntityIdentifier, x.EntityPlatform },
                        principalTable: "TrialUsers",
                        principalColumns: new[] { "Identifier", "Platform" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuotaEntries",
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
                    table.PrimaryKey("PK_QuotaEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotaEntries_Quotas_QuotaId",
                        column: x => x.QuotaId,
                        principalTable: "Quotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuotaEntries_QuotaId",
                table: "QuotaEntries",
                column: "QuotaId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotas_EntityIdentifier_EntityPlatform",
                table: "Quotas",
                columns: new[] { "EntityIdentifier", "EntityPlatform" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuotaEntries");

            migrationBuilder.DropTable(
                name: "Quotas");
        }
    }
}
