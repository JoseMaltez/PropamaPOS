using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropamaPOS.Migrations
{
    /// <inheritdoc />
    public partial class initialCrate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id_Cliente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NIT = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id_Cliente);
                });

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id_Proveedor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id_Proveedor);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id_Rol = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id_Rol);
                });

            migrationBuilder.CreateTable(
                name: "UnidadesMedida",
                columns: table => new
                {
                    Id_UnidadMedida = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Abreviatura = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadesMedida", x => x.Id_UnidadMedida);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id_Item = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsServicio = table.Column<bool>(type: "bit", nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    StockMinimo = table.Column<int>(type: "int", nullable: true),
                    CostoPromedioUnidad = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Id_Categoria = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id_Item);
                    table.ForeignKey(
                        name: "FK_Items_Categorias_Id_Categoria",
                        column: x => x.Id_Categoria,
                        principalTable: "Categorias",
                        principalColumn: "Id_Categoria",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id_Usuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreUsuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Id_Rol = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id_Usuario);
                    table.ForeignKey(
                        name: "FK_Usuarios_Roles_Id_Rol",
                        column: x => x.Id_Rol,
                        principalTable: "Roles",
                        principalColumn: "Id_Rol",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemPresentaciones",
                columns: table => new
                {
                    Id_ItemPresentacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Item = table.Column<int>(type: "int", nullable: false),
                    Id_UnidadMedida = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioCosto = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemPresentaciones", x => x.Id_ItemPresentacion);
                    table.ForeignKey(
                        name: "FK_ItemPresentaciones_Items_Id_Item",
                        column: x => x.Id_Item,
                        principalTable: "Items",
                        principalColumn: "Id_Item",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemPresentaciones_UnidadesMedida_Id_UnidadMedida",
                        column: x => x.Id_UnidadMedida,
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id_UnidadMedida",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    Id_Empleado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    FechaContratacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Id_Usuario = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.Id_Empleado);
                    table.ForeignKey(
                        name: "FK_Empleados_Usuarios_Id_Usuario",
                        column: x => x.Id_Usuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Expiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Id_Usuario = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Usuarios_Id_Usuario",
                        column: x => x.Id_Usuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "Compras",
                columns: table => new
                {
                    Id_Compra = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroCompra = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Id_Proveedor = table.Column<int>(type: "int", nullable: false),
                    Id_Empleado = table.Column<int>(type: "int", nullable: true),
                    CreadoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IVA = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Nota = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compras", x => x.Id_Compra);
                    table.ForeignKey(
                        name: "FK_Compras_Empleados_Id_Empleado",
                        column: x => x.Id_Empleado,
                        principalTable: "Empleados",
                        principalColumn: "Id_Empleado",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Compras_Proveedores_Id_Proveedor",
                        column: x => x.Id_Proveedor,
                        principalTable: "Proveedores",
                        principalColumn: "Id_Proveedor",
                        onDelete: ReferentialAction.Restrict);
                });

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
                    IVA = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AjusteRedondeo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_NIT",
                table: "Clientes",
                column: "NIT",
                unique: true);

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
                name: "IX_Compras_Id_Empleado",
                table: "Compras",
                column: "Id_Empleado");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_Id_Proveedor",
                table: "Compras",
                column: "Id_Proveedor");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_NumeroCompra",
                table: "Compras",
                column: "NumeroCompra",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_Correo",
                table: "Empleados",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_Id_Usuario",
                table: "Empleados",
                column: "Id_Usuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemPresentaciones_Id_Item_Id_UnidadMedida",
                table: "ItemPresentaciones",
                columns: new[] { "Id_Item", "Id_UnidadMedida" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemPresentaciones_Id_UnidadMedida",
                table: "ItemPresentaciones",
                column: "Id_UnidadMedida");

            migrationBuilder.CreateIndex(
                name: "IX_ItemProveedores_Id_Item",
                table: "ItemProveedores",
                column: "Id_Item");

            migrationBuilder.CreateIndex(
                name: "IX_ItemProveedores_Id_Proveedor",
                table: "ItemProveedores",
                column: "Id_Proveedor");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Codigo",
                table: "Items",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_Id_Categoria",
                table: "Items",
                column: "Id_Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_PagoVentas_Id_Venta",
                table: "PagoVentas",
                column: "Id_Venta");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_Id_Usuario",
                table: "PasswordResetTokens",
                column: "Id_Usuario");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Correo",
                table: "Proveedores",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicioComponentes_Id_Item",
                table: "ServicioComponentes",
                column: "Id_Item");

            migrationBuilder.CreateIndex(
                name: "IX_ServicioComponentes_Id_Servicio",
                table: "ServicioComponentes",
                column: "Id_Servicio");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Id_Rol",
                table: "Usuarios",
                column: "Id_Rol");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NombreUsuario",
                table: "Usuarios",
                column: "NombreUsuario",
                unique: true);

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
                name: "AjusteInventarioDetalles");

            migrationBuilder.DropTable(
                name: "CompraDetalles");

            migrationBuilder.DropTable(
                name: "ItemProveedores");

            migrationBuilder.DropTable(
                name: "PagoVentas");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "ServicioComponentes");

            migrationBuilder.DropTable(
                name: "VentaDetalles");

            migrationBuilder.DropTable(
                name: "AjustesInventario");

            migrationBuilder.DropTable(
                name: "Compras");

            migrationBuilder.DropTable(
                name: "ItemPresentaciones");

            migrationBuilder.DropTable(
                name: "Ventas");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "UnidadesMedida");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
