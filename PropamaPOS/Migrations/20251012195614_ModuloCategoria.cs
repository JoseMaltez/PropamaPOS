using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropamaPOS.Migrations
{
    /// <inheritdoc />
    public partial class ModuloCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoriaId_Categoria",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id_Categoria",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id_Categoria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id_Categoria);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Categorias_CategoriaId_Categoria",
                table: "Items");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropIndex(
                name: "IX_Items_CategoriaId_Categoria",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "CategoriaId_Categoria",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Id_Categoria",
                table: "Items");
        }
    }
}
