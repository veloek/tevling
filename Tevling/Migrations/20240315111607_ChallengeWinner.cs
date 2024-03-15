using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tevling.Migrations
{
    /// <inheritdoc />
    public partial class ChallengeWinner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WinnerId",
                table: "Challenges",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_WinnerId",
                table: "Challenges",
                column: "WinnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_Athletes_WinnerId",
                table: "Challenges",
                column: "WinnerId",
                principalTable: "Athletes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_Athletes_WinnerId",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_WinnerId",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "WinnerId",
                table: "Challenges");
        }
    }
}
