using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoteriaProject.Migrations
{
    /// <inheritdoc />
    public partial class AddAstroPatronsSign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "sing",
                table: "Tickets",
                newName: "sign");

            migrationBuilder.AddColumn<string>(
                name: "Sign",
                table: "AstroPatrons",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sign",
                table: "AstroPatrons");

            migrationBuilder.RenameColumn(
                name: "sign",
                table: "Tickets",
                newName: "sing");
        }
    }
}
