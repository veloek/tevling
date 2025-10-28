using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tevling.Migrations
{
    /// <inheritdoc />
    public partial class DeviceName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Details_DeviceName",
                table: "Activities",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Details_DeviceName",
                table: "Activities");
        }
    }
}
