using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropamaPOS.Migrations
{
    /// <inheritdoc />
    public partial class CorreccionCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Categorias_CategoriaId_Categoria",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_CategoriaId_Categoria",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "CategoriaId_Categoria",
                table: "Items");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Id_Categoria",
                table: "Items",
                column: "Id_Categoria");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Categorias_Id_Categoria",
                table: "Items",
                column: "Id_Categoria",
                principalTable: "Categorias",
                principalColumn: "Id_Categoria",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Categorias_Id_Categoria",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_Id_Categoria",
                table: "Items");

            migrationBuilder.AddColumn<int>(
                name: "CategoriaId_Categoria",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_CategoriaId_Categoria",
                table: "Items",
                column: "CategoriaId_Categoria");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Categorias_CategoriaId_Categoria",
                table: "Items",
                column: "CategoriaId_Categoria",
                principalTable: "Categorias",
                principalColumn: "Id_Categoria");
        }
    }
}
