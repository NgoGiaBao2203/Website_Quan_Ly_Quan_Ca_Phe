using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebCoffeeApplication.Models;

namespace WebCoffeeApplication.DataContext
{
  public class AppDataContext : IdentityDbContext<TaiKhoan, IdentityRole<int>, int>
  {
    public AppDataContext() { }

    public AppDataContext(DbContextOptions<AppDataContext> options) : base(options) { }

    public DbSet<TaiKhoan> TaiKhoan { get; set; }
    public DbSet<HoaDon> HoaDon { get; set; }
    public DbSet<ChiTietHoaDon> ChiTietHoaDon { get; set; }
    public DbSet<DanhMucDoUong> DanhMucDoUong { get; set; }
    public DbSet<DoUong> DoUong { get; set; }
    public DbSet<ChiTietDoUong> ChiTietDoUong { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      if (!optionsBuilder.IsConfigured)
      {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        optionsBuilder.UseSqlServer(connectionString);
      }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      // Configure TaiKhoan entity
      modelBuilder.Entity<TaiKhoan>(entity =>
      {
        entity.ToTable("TAIKHOAN");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("MaTaiKhoan").ValueGeneratedOnAdd();
        entity.Property(e => e.HoTen).HasMaxLength(100);
        entity.Property(e => e.PasswordHash)
            .HasColumnName("MatKhau")
            .HasMaxLength(256);
        entity.Property(e => e.SDT)
            .HasMaxLength(11)
            .IsRequired(false);

        // Ignore Identity default properties we don't need
        entity.Ignore(e => e.PhoneNumber);
        entity.Ignore(e => e.PhoneNumberConfirmed);
        entity.Ignore(e => e.TwoFactorEnabled);
        entity.Ignore(e => e.LockoutEnd);
        entity.Ignore(e => e.LockoutEnabled);
        entity.Ignore(e => e.AccessFailedCount);
      });

    }
  }
}
