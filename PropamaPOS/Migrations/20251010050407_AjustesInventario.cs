using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropamaPOS.Migrations
{
    /// <inheritdoc />
    public partial class AjustesInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AjustesInventario",
                columns: table => new
                {
                    Id_Ajuste = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Id_Empleado = table.Column<int>(type: "int", nullable: true),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreadoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AjustesInventario", x => x.Id_Ajuste);
                    table.ForeignKey(
                        name: "FK_AjustesInventario_Empleados_Id_Empleado",
                        column: x => x.Id_Empleado,
                        principalTable: "Empleados",
                        principalColumn: "Id_Empleado",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AjusteInventarioDetalles",
                columns: table => new
                {
                    Id_AjusteDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Ajuste = table.Column<int>(type: "int", nullable: false),
                    Id_Item = table.Column<int>(type: "int", nullable: false),
                    Id_ItemPresentacion = table.Column<int>(type: "int", nullable: true),
                    CantidadPresentaciones = table.Column<int>(type: "int", nullable: false),
                    CantidadUnidades = table.Column<int>(type: "int", nullable: false),
                    StockAntes = table.Column<int>(type: "int", nullable: false),
                    StockDespues = table.Column<int>(type: "int", nullable: false),
                    Nota = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AjusteInventarioDetalles", x => x.Id_AjusteDetalle);
                    table.ForeignKey(
                        name: "FK_AjusteInventarioDetalles_AjustesInventario_Id_Ajuste",
                        column: x => x.Id_Ajuste,
                        principalTable: "AjustesInventario",
                        principalColumn: "Id_Ajuste",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AjusteInventarioDetalles_ItemPresentaciones_Id_ItemPresentacion",
                        column: x => x.Id_ItemPresentacion,
                        principalTable: "ItemPresentaciones",
                        principalColumn: "Id_ItemPresentacion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AjusteInventarioDetalles_Items_Id_Item",
                        column: x => x.Id_Item,
                        principalTable: "Items",
                        principalColumn: "Id_Item",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AjusteInventarioDetalles_Id_Ajuste",
                table: "AjusteInventarioDetalles",
                column: "Id_Ajuste");

            migrationBuilder.CreateIndex(
                name: "IX_AjusteInventarioDetalles_Id_Item",
                table: "AjusteInventarioDetalles",
                column: "Id_Item");

            migrationBuilder.CreateIndex(
                name: "IX_AjusteInventarioDetalles_Id_ItemPresentacion",
                table: "AjusteInventarioDetalles",
                column: "Id_ItemPresentacion");

            migrationBuilder.CreateIndex(
                name: "IX_AjustesInventario_Id_Empleado",
                table: "AjustesInventario",
                column: "Id_Empleado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AjusteInventarioDetalles");

            migrationBuilder.DropTable(
                name: "AjustesInventario");
        }
    }
}
