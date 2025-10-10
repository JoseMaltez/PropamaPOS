using Microsoft.EntityFrameworkCore;
using PropamaPOS.Models;

namespace PropamaPOS.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<UnidadMedida> UnidadesMedida { get; set; }
        public DbSet<ItemPresentacion> ItemPresentaciones { get; set; }
        public DbSet<ItemProveedor> ItemProveedores { get; set; }
        public DbSet<Compra> Compras { get; set; }
        public DbSet<CompraDetalle> CompraDetalles { get; set; }
        public DbSet<AjusteInventario> AjustesInventario { get; set; }
        public DbSet<AjusteInventarioDetalle> AjusteInventarioDetalles { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relación Usuario - Rol
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Rol)
                .WithMany()
                .HasForeignKey(u => u.Id_Rol);

            // Relación Usuario - Empleado (1:1)
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Empleado)
                .WithOne(e => e.Usuario)
                .HasForeignKey<Empleado>(e => e.Id_Usuario)
                .OnDelete(DeleteBehavior.Cascade);

            // Índices únicos
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.NombreUsuario)
                .IsUnique();

            modelBuilder.Entity<Empleado>()
                .HasIndex(e => e.Correo)
                .IsUnique();

            modelBuilder.Entity<Proveedor>()
                .HasIndex(p => p.Correo)
                .IsUnique();

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => new { c.Nombre, c.Apellido, c.Telefono })
                .IsUnique();

            // Relaciones Item - Presentaciones (1:N)
            modelBuilder.Entity<ItemPresentacion>()
                .HasOne(p => p.Item)
                .WithMany(i => i.Presentaciones)
                .HasForeignKey(p => p.Id_Item)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación Presentación - UnidadMedida
            modelBuilder.Entity<ItemPresentacion>()
                .HasOne(p => p.UnidadMedida)
                .WithMany()
                .HasForeignKey(p => p.Id_UnidadMedida);

            // Evitar duplicados (misma unidad por item)
            modelBuilder.Entity<ItemPresentacion>()
                .HasIndex(p => new { p.Id_Item, p.Id_UnidadMedida })
                .IsUnique();

            // Relación ItemProveedor (N:N manual)
            modelBuilder.Entity<ItemProveedor>()
                .HasOne(ip => ip.Item)
                .WithMany()
                .HasForeignKey(ip => ip.Id_Item)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ItemProveedor>()
                .HasOne(ip => ip.Proveedor)
                .WithMany()
                .HasForeignKey(ip => ip.Id_Proveedor)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Compra - Proveedor
            modelBuilder.Entity<Compra>()
                .HasOne(c => c.Proveedor)
                .WithMany()
                .HasForeignKey(c => c.Id_Proveedor)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Compra>()
                .HasOne(c => c.Empleado)
                .WithMany()
                .HasForeignKey(c => c.Id_Empleado)
                .OnDelete(DeleteBehavior.SetNull);

            // Relaciones Compra - Detalle
            modelBuilder.Entity<CompraDetalle>()
                .HasOne(d => d.Compra)
                .WithMany(c => c.Detalles)
                .HasForeignKey(d => d.Id_Compra)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CompraDetalle>()
                .HasOne(d => d.Item)
                .WithMany()
                .HasForeignKey(d => d.Id_Item)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CompraDetalle>()
                .HasOne(d => d.Presentacion)
                .WithMany()
                .HasForeignKey(d => d.Id_ItemPresentacion)
                .OnDelete(DeleteBehavior.Restrict);

            // Ajustes - Empleado (opcional)
            modelBuilder.Entity<AjusteInventario>()
                .HasOne(a => a.Empleado)
                .WithMany()
                .HasForeignKey(a => a.Id_Empleado)
                .OnDelete(DeleteBehavior.SetNull);

            // AjusteDetalle -> Item / Presentacion
            modelBuilder.Entity<AjusteInventarioDetalle>()
                .HasOne(d => d.Item)
                .WithMany()
                .HasForeignKey(d => d.Id_Item)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AjusteInventarioDetalle>()
                .HasOne(d => d.Presentacion)
                .WithMany()
                .HasForeignKey(d => d.Id_ItemPresentacion)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuración decimal general
            modelBuilder.Entity<Item>()
                .Property(i => i.CostoPromedioUnidad)
                .HasColumnType("decimal(18,4)");

            modelBuilder.Entity<ItemPresentacion>()
                .Property(p => p.PrecioVenta)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ItemPresentacion>()
                .Property(p => p.PrecioCosto)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CompraDetalle>()
                .Property(d => d.PrecioCostoPorPresentacion)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CompraDetalle>()
                .Property(d => d.PrecioCostoPorUnidad)
                .HasColumnType("decimal(18,4)");

            modelBuilder.Entity<CompraDetalle>()
                .Property(d => d.Subtotal)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Compra>()
                .Property(c => c.Total)
                .HasColumnType("decimal(18,2)");
        }
    }
}
