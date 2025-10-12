using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropamaPOS.Migrations
{
    /// <inheritdoc />
    public partial class ModificacionServicios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsServicio",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsServicio",
                table: "Items");
        }
    }
}
