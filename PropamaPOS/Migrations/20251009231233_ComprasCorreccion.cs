using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropamaPOS.Migrations
{
    /// <inheritdoc />
    public partial class ComprasCorreccion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Proveedores_Id_Proveedor",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_Id_Proveedor",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Id_Proveedor",
                table: "Items");

            migrationBuilder.AddColumn<int>(
                name: "Id_Empleado",
                table: "Compras",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compras_Id_Empleado",
                table: "Compras",
                column: "Id_Empleado");

            migrationBuilder.AddForeignKey(
                name: "FK_Compras_Empleados_Id_Empleado",
                table: "Compras",
                column: "Id_Empleado",
                principalTable: "Empleados",
                principalColumn: "Id_Empleado",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Compras_Empleados_Id_Empleado",
                table: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_Compras_Id_Empleado",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "Id_Empleado",
                table: "Compras");

            migrationBuilder.AddColumn<int>(
                name: "Id_Proveedor",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_Id_Proveedor",
                table: "Items",
                column: "Id_Proveedor");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Proveedores_Id_Proveedor",
                table: "Items",
                column: "Id_Proveedor",
                principalTable: "Proveedores",
                principalColumn: "Id_Proveedor");
        }
    }
}
