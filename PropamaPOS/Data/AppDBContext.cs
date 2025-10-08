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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Relación entre Usuario y Rol
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Rol)
                .WithMany()
                .HasForeignKey(u => u.Id_Rol);

            // Relación entre Usuario y Empleado
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Empleado)
                .WithOne(e => e.Usuario)
                .HasForeignKey<Empleado>(e => e.Id_Usuario)
                .OnDelete(DeleteBehavior.Cascade);

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

            // Relaciones Item - Presentaciones
            modelBuilder.Entity<ItemPresentacion>()
                .HasOne(p => p.Item)
                .WithMany(i => i.Presentaciones)
                .HasForeignKey(p => p.Id_Item)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacion con UnidadMedida
            modelBuilder.Entity<ItemPresentacion>()
                .HasOne(p => p.UnidadMedida)
                .WithMany()
                .HasForeignKey(p => p.Id_UnidadMedida);

            // Evitar duplicados (una unidad por item)
            modelBuilder.Entity<ItemPresentacion>()
                .HasIndex(p => new { p.Id_Item, p.Id_UnidadMedida })
                .IsUnique();

            // Precisión decimal (si usas Fluent API adicional)
            modelBuilder.Entity<ItemPresentacion>()
                .Property(p => p.PrecioVenta)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<ItemPresentacion>()
                .Property(p => p.PrecioCosto)
                .HasColumnType("decimal(18,2)");

        }
    }
}
