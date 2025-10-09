using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropamaPOS.Migrations
{
    /// <inheritdoc />
    public partial class ModuloCompras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoPromedioUnidad",
                table: "Items",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "Items",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Compras",
                columns: table => new
                {
                    Id_Compra = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Id_Proveedor = table.Column<int>(type: "int", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Nota = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compras", x => x.Id_Compra);
                    table.ForeignKey(
                        name: "FK_Compras_Proveedores_Id_Proveedor",
                        column: x => x.Id_Proveedor,
                        principalTable: "Proveedores",
                        principalColumn: "Id_Proveedor",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemProveedores",
                columns: table => new
                {
                    Id_ItemProveedor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Item = table.Column<int>(type: "int", nullable: false),
                    Id_Proveedor = table.Column<int>(type: "int", nullable: false),
                    CodigoProveedor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemProveedores", x => x.Id_ItemProveedor);
                    table.ForeignKey(
                        name: "FK_ItemProveedores_Items_Id_Item",
                        column: x => x.Id_Item,
                        principalTable: "Items",
                        principalColumn: "Id_Item",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemProveedores_Proveedores_Id_Proveedor",
                        column: x => x.Id_Proveedor,
                        principalTable: "Proveedores",
                        principalColumn: "Id_Proveedor",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompraDetalles",
                columns: table => new
                {
                    Id_CompraDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Compra = table.Column<int>(type: "int", nullable: false),
                    Id_Item = table.Column<int>(type: "int", nullable: false),
                    Id_ItemPresentacion = table.Column<int>(type: "int", nullable: false),
                    CantidadPresentaciones = table.Column<int>(type: "int", nullable: false),
                    PrecioCostoPorPresentacion = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioCostoPorUnidad = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompraDetalles", x => x.Id_CompraDetalle);
                    table.ForeignKey(
                        name: "FK_CompraDetalles_Compras_Id_Compra",
                        column: x => x.Id_Compra,
                        principalTable: "Compras",
                        principalColumn: "Id_Compra",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompraDetalles_ItemPresentaciones_Id_ItemPresentacion",
                        column: x => x.Id_ItemPresentacion,
                        principalTable: "ItemPresentaciones",
                        principalColumn: "Id_ItemPresentacion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompraDetalles_Items_Id_Item",
                        column: x => x.Id_Item,
                        principalTable: "Items",
                        principalColumn: "Id_Item",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalles_Id_Compra",
                table: "CompraDetalles",
                column: "Id_Compra");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalles_Id_Item",
                table: "CompraDetalles",
                column: "Id_Item");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalles_Id_ItemPresentacion",
                table: "CompraDetalles",
                column: "Id_ItemPresentacion");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_Id_Proveedor",
                table: "Compras",
                column: "Id_Proveedor");

            migrationBuilder.CreateIndex(
                name: "IX_ItemProveedores_Id_Item",
                table: "ItemProveedores",
                column: "Id_Item");

            migrationBuilder.CreateIndex(
                name: "IX_ItemProveedores_Id_Proveedor",
                table: "ItemProveedores",
                column: "Id_Proveedor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompraDetalles");

            migrationBuilder.DropTable(
                name: "ItemProveedores");

            migrationBuilder.DropTable(
                name: "Compras");

            migrationBuilder.DropColumn(
                name: "CostoPromedioUnidad",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Stock",
                table: "Items");
        }
    }
}
