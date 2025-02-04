using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoteriaProject.Migrations
{
    /// <inheritdoc />
    public partial class Addfieldsinginticket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "sing",
                table: "Tickets",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sing",
                table: "Tickets");
        }
    }
}
