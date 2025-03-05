using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tevling.Migrations
{
    /// <inheritdoc />
    public partial class FollowRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FollowRequests",
                columns: table => new
                {
                    FolloweeId = table.Column<int>(type: "INTEGER", nullable: false),
                    FollowerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowRequests", x => new { x.FolloweeId, x.FollowerId });
                    table.ForeignKey(
                        name: "FK_FollowRequests_Athletes_FolloweeId",
                        column: x => x.FolloweeId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FollowRequests_Athletes_FollowerId",
                        column: x => x.FollowerId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FollowRequests_FollowerId",
                table: "FollowRequests",
                column: "FollowerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FollowRequests");
        }
    }
}
