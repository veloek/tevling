using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spur.Migrations
{
    /// <inheritdoc />
    public partial class ChallengeDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActivityTypes",
                table: "Challenges",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Challenges",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Challenges",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Measurement",
                table: "Challenges",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Challenges",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_CreatedById",
                table: "Challenges",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_Athletes_CreatedById",
                table: "Challenges",
                column: "CreatedById",
                principalTable: "Athletes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_Athletes_CreatedById",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_CreatedById",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "ActivityTypes",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "Measurement",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Challenges");
        }
    }
}
