using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spur.Migrations
{
    public partial class ActivityDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_ActivityDetails_DetailsId",
                table: "Activities");

            migrationBuilder.DropTable(
                name: "ActivityDetails");

            migrationBuilder.DropIndex(
                name: "IX_Activities_DetailsId",
                table: "Activities");

            migrationBuilder.RenameColumn(
                name: "DetailsId",
                table: "Activities",
                newName: "Details_Type");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Activities",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<float>(
                name: "Details_Calories",
                table: "Activities",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Details_Description",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Details_DistanceInMeters",
                table: "Activities",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Details_ElapsedTimeInSeconds",
                table: "Activities",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Details_Manual",
                table: "Activities",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Details_MovingTimeInSeconds",
                table: "Activities",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Details_Name",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Details_StartDate",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Details_TotalElevationGain",
                table: "Activities",
                type: "REAL",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Details_Calories",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Details_Description",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Details_DistanceInMeters",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Details_ElapsedTimeInSeconds",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Details_Manual",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Details_MovingTimeInSeconds",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Details_Name",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Details_StartDate",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Details_TotalElevationGain",
                table: "Activities");

            migrationBuilder.RenameColumn(
                name: "Details_Type",
                table: "Activities",
                newName: "DetailsId");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Activities",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.CreateTable(
                name: "ActivityDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityDetails", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_DetailsId",
                table: "Activities",
                column: "DetailsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_ActivityDetails_DetailsId",
                table: "Activities",
                column: "DetailsId",
                principalTable: "ActivityDetails",
                principalColumn: "Id");
        }
    }
}
