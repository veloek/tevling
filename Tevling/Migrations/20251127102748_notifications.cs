using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tevling.Migrations
{
    /// <inheritdoc />
    public partial class notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnreadNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedById = table.Column<int>(type: "INTEGER", nullable: false),
                    Recipient = table.Column<int>(type: "INTEGER", nullable: false),
                    NotificationReadId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ChallengeId = table.Column<int>(type: "INTEGER", nullable: true),
                    Read = table.Column<long>(type: "INTEGER", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnreadNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnreadNotifications_Athletes_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnreadNotifications_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnreadNotifications_ChallengeId",
                table: "UnreadNotifications",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_UnreadNotifications_CreatedById",
                table: "UnreadNotifications",
                column: "CreatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnreadNotifications");
        }
    }
}
