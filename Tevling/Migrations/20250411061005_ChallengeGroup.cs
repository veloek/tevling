using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tevling.Migrations
{
    /// <inheritdoc />
    public partial class ChallengeGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChallengeGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedById = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeGroups_Athletes_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AthleteChallengeGroup",
                columns: table => new
                {
                    ChallengeGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    MembersId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AthleteChallengeGroup", x => new { x.ChallengeGroupId, x.MembersId });
                    table.ForeignKey(
                        name: "FK_AthleteChallengeGroup_Athletes_MembersId",
                        column: x => x.MembersId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AthleteChallengeGroup_ChallengeGroups_ChallengeGroupId",
                        column: x => x.ChallengeGroupId,
                        principalTable: "ChallengeGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AthleteChallengeGroup_MembersId",
                table: "AthleteChallengeGroup",
                column: "MembersId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeGroups_CreatedById",
                table: "ChallengeGroups",
                column: "CreatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AthleteChallengeGroup");

            migrationBuilder.DropTable(
                name: "ChallengeGroups");
        }
    }
}
