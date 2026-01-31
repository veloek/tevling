using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tevling.Migrations
{
    /// <inheritdoc />
    public partial class IndividualGoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "IndividualGoal",
                table: "ChallengeTemplates",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "IndividualGoal",
                table: "Challenges",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IndividualGoal",
                table: "ChallengeTemplates");

            migrationBuilder.DropColumn(
                name: "IndividualGoal",
                table: "Challenges");
        }
    }
}
