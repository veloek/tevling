using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spur.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "Athletes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StravaId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ImgUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Athletes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Start = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    End = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StravaId = table.Column<long>(type: "INTEGER", nullable: false),
                    AthleteId = table.Column<int>(type: "INTEGER", nullable: false),
                    DetailsId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_ActivityDetails_DetailsId",
                        column: x => x.DetailsId,
                        principalTable: "ActivityDetails",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Activities_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AthleteChallenge",
                columns: table => new
                {
                    AthletesId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChallengesId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AthleteChallenge", x => new { x.AthletesId, x.ChallengesId });
                    table.ForeignKey(
                        name: "FK_AthleteChallenge_Athletes_AthletesId",
                        column: x => x.AthletesId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AthleteChallenge_Challenges_ChallengesId",
                        column: x => x.ChallengesId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_AthleteId",
                table: "Activities",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_DetailsId",
                table: "Activities",
                column: "DetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_AthleteChallenge_ChallengesId",
                table: "AthleteChallenge",
                column: "ChallengesId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "AthleteChallenge");

            migrationBuilder.DropTable(
                name: "ActivityDetails");

            migrationBuilder.DropTable(
                name: "Athletes");

            migrationBuilder.DropTable(
                name: "Challenges");
        }
    }
}
