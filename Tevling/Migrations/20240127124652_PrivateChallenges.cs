using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tevling.Migrations
{
    /// <inheritdoc />
    public partial class PrivateChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "Challenges",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AthleteChallenge1",
                columns: table => new
                {
                    ChallengeId = table.Column<int>(type: "INTEGER", nullable: false),
                    InvitedAthletesId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AthleteChallenge1", x => new { x.ChallengeId, x.InvitedAthletesId });
                    table.ForeignKey(
                        name: "FK_AthleteChallenge1_Athletes_InvitedAthletesId",
                        column: x => x.InvitedAthletesId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AthleteChallenge1_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AthleteChallenge1_InvitedAthletesId",
                table: "AthleteChallenge1",
                column: "InvitedAthletesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AthleteChallenge1");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "Challenges");
        }
    }
}
