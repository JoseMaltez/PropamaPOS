using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropamaPOS.Migrations
{
    /// <inheritdoc />
    public partial class ModuloVentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ventas",
                columns: table => new
                {
                    Id_Venta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroVenta = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Id_Cliente = table.Column<int>(type: "int", nullable: true),
                    NombreConsumidor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descuentos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MetodoPago = table.Column<int>(type: "int", nullable: false),
                    MontoRecibido = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Cambio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Id_Empleado = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas", x => x.Id_Venta);
                    table.ForeignKey(
                        name: "FK_Ventas_Clientes_Id_Cliente",
                        column: x => x.Id_Cliente,
                        principalTable: "Clientes",
                        principalColumn: "Id_Cliente",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Ventas_Empleados_Id_Empleado",
                        column: x => x.Id_Empleado,
                        principalTable: "Empleados",
                        principalColumn: "Id_Empleado",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PagoVentas",
                columns: table => new
                {
                    Id_PagoVenta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Venta = table.Column<int>(type: "int", nullable: false),
                    Metodo = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Nota = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagoVentas", x => x.Id_PagoVenta);
                    table.ForeignKey(
                        name: "FK_PagoVentas_Ventas_Id_Venta",
                        column: x => x.Id_Venta,
                        principalTable: "Ventas",
                        principalColumn: "Id_Venta",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VentaDetalles",
                columns: table => new
                {
                    Id_VentaDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Venta = table.Column<int>(type: "int", nullable: false),
                    Id_Item = table.Column<int>(type: "int", nullable: false),
                    Id_ItemPresentacion = table.Column<int>(type: "int", nullable: true),
                    CantidadPresentaciones = table.Column<int>(type: "int", nullable: false),
                    CantidadUnidades = table.Column<int>(type: "int", nullable: false),
                    PrecioVentaPorPresentacion = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EsServicio = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentaDetalles", x => x.Id_VentaDetalle);
                    table.ForeignKey(
                        name: "FK_VentaDetalles_ItemPresentaciones_Id_ItemPresentacion",
                        column: x => x.Id_ItemPresentacion,
                        principalTable: "ItemPresentaciones",
                        principalColumn: "Id_ItemPresentacion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaDetalles_Items_Id_Item",
                        column: x => x.Id_Item,
                        principalTable: "Items",
                        principalColumn: "Id_Item",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaDetalles_Ventas_Id_Venta",
                        column: x => x.Id_Venta,
                        principalTable: "Ventas",
                        principalColumn: "Id_Venta",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagoVentas_Id_Venta",
                table: "PagoVentas",
                column: "Id_Venta");

            migrationBuilder.CreateIndex(
                name: "IX_VentaDetalles_Id_Item",
                table: "VentaDetalles",
                column: "Id_Item");

            migrationBuilder.CreateIndex(
                name: "IX_VentaDetalles_Id_ItemPresentacion",
                table: "VentaDetalles",
                column: "Id_ItemPresentacion");

            migrationBuilder.CreateIndex(
                name: "IX_VentaDetalles_Id_Venta",
                table: "VentaDetalles",
                column: "Id_Venta");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_Id_Cliente",
                table: "Ventas",
                column: "Id_Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_Id_Empleado",
                table: "Ventas",
                column: "Id_Empleado");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_NumeroVenta",
                table: "Ventas",
                column: "NumeroVenta",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagoVentas");

            migrationBuilder.DropTable(
                name: "VentaDetalles");

            migrationBuilder.DropTable(
                name: "Ventas");
        }
    }
}
