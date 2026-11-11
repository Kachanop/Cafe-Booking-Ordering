using CafeSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace CafeSystem.Data;

public sealed class CafeDbContext(DbContextOptions<CafeDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<CafeUser> Users => Set<CafeUser>();
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<TableCafe> Tables => Set<TableCafe>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<CafeOrder> Orders => Set<CafeOrder>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<LoginSession> LoginSessions => Set<LoginSession>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Role>().ToTable("Role"); b.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        b.Entity<CafeUser>().ToTable("User"); b.Entity<CafeUser>().HasIndex(x => x.Email).IsUnique(); b.Entity<CafeUser>().HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId);
        b.Entity<StaffProfile>().ToTable("StaffProfile").Property(x => x.Specialties).HasConversion(v => string.Join("|", v), v => v.Split('|', StringSplitOptions.RemoveEmptyEntries));
        b.Entity<Service>().ToTable("Service");
        b.Entity<Booking>().ToTable("Booking").HasIndex(x => new { x.StaffId, x.BookingDate, x.TimeSlot }).IsUnique().HasFilter("Status <> 'Cancelled'");
        b.Entity<TableCafe>().ToTable("TableCafe"); b.Entity<MenuCategory>().ToTable("MenuCategory"); b.Entity<MenuItem>().ToTable("MenuItem");
        b.Entity<CafeOrder>().ToTable("Order"); b.Entity<OrderItem>().ToTable("OrderItem"); b.Entity<Payment>().ToTable("Payment");
        b.Entity<LoginSession>().ToTable("LoginSession").HasKey(x => x.TokenHash); b.Entity<LoginSession>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
