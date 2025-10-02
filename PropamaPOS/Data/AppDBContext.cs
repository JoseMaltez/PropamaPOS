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

            //propiedad unica para el nombre de usuario y correo

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.NombreUsuario)
                .IsUnique();

            modelBuilder.Entity<Empleado>()
                .HasIndex(e => e.Correo)
                .IsUnique();

            modelBuilder.Entity<Proveedor>()
                .HasIndex(p => p.Correo)
                .IsUnique();
        }
    }
}
