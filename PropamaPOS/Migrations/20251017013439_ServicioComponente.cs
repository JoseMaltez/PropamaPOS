using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropamaPOS.Migrations
{
    /// <inheritdoc />
    public partial class ServicioComponente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServicioComponentes",
                columns: table => new
                {
                    Id_ServicioComponente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Servicio = table.Column<int>(type: "int", nullable: false),
                    Id_Item = table.Column<int>(type: "int", nullable: false),
                    CantidadPorServicio = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicioComponentes", x => x.Id_ServicioComponente);
                    table.ForeignKey(
                        name: "FK_ServicioComponentes_Items_Id_Item",
                        column: x => x.Id_Item,
                        principalTable: "Items",
                        principalColumn: "Id_Item",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServicioComponentes_Items_Id_Servicio",
                        column: x => x.Id_Servicio,
                        principalTable: "Items",
                        principalColumn: "Id_Item",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServicioComponentes_Id_Item",
                table: "ServicioComponentes",
                column: "Id_Item");

            migrationBuilder.CreateIndex(
                name: "IX_ServicioComponentes_Id_Servicio",
                table: "ServicioComponentes",
                column: "Id_Servicio");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServicioComponentes");
        }
    }
}
