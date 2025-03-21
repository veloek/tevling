using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tevling.Migrations
{
    /// <inheritdoc />
    public partial class ChallengeTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChallengeTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Measurement = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityTypes = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    IsPrivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedById = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeTemplates_Athletes_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeTemplates_CreatedById",
                table: "ChallengeTemplates",
                column: "CreatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChallengeTemplates");
        }
    }
}
