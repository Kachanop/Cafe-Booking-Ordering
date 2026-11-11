using CafeSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace CafeSystem.Data;

public sealed class CafeDbContext(DbContextOptions<CafeDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>(); public DbSet<CafeUser> Users => Set<CafeUser>(); public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>(); public DbSet<Service> Services => Set<Service>(); public DbSet<Booking> Bookings => Set<Booking>(); public DbSet<TableCafe> Tables => Set<TableCafe>(); public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>(); public DbSet<MenuItem> MenuItems => Set<MenuItem>(); public DbSet<CafeOrder> Orders => Set<CafeOrder>(); public DbSet<OrderItem> OrderItems => Set<OrderItem>(); public DbSet<Payment> Payments => Set<Payment>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Role>().ToTable("Role").HasKey(x => x.Id); b.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        b.Entity<CafeUser>().ToTable("User").HasKey(x => x.Id); b.Entity<CafeUser>().Property(x => x.AuthUserId).HasColumnName("auth_user_id"); b.Entity<CafeUser>().Property(x => x.CreatedAt).HasColumnName("createdAt"); b.Entity<CafeUser>().HasIndex(x => x.AuthUserId).IsUnique(); b.Entity<CafeUser>().HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId);
        b.Entity<StaffProfile>().ToTable("StaffProfile").HasKey(x => x.Id); b.Entity<StaffProfile>().Property(x => x.AvatarUrl).HasColumnName("avatar_url"); b.Entity<StaffProfile>().Property(x => x.Specialties).HasColumnType("text[]");
        b.Entity<Service>().ToTable("Service").HasKey(x => x.Id);
        b.Entity<Booking>().ToTable("Booking").HasKey(x => x.Id); b.Entity<Booking>().Property(x => x.CustomerNote).HasColumnName("customer_note"); b.Entity<Booking>().Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Entity<TableCafe>().ToTable("TableCafe").HasKey(x => x.Id); b.Entity<TableCafe>().Property(x => x.BillRequestedAt).HasColumnName("bill_requested_at"); b.Entity<TableCafe>().Property(x => x.OpenedAt).HasColumnName("opened_at");
        b.Entity<MenuCategory>().ToTable("MenuCategory").HasKey(x => x.Id); b.Entity<MenuItem>().ToTable("MenuItem").HasKey(x => x.Id);
        b.Entity<CafeOrder>().ToTable("Order").HasKey(x => x.Id); b.Entity<CafeOrder>().Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Entity<OrderItem>().ToTable("OrderItem").HasKey(x => x.Id); b.Entity<OrderItem>().Property(x => x.KdsStatus).HasColumnName("kdsStatus");
        b.Entity<Payment>().ToTable("Payment").HasKey(x => x.Id); b.Entity<Payment>().Property(x => x.ReferenceId).HasColumnName("referenceId"); b.Entity<Payment>().Property(x => x.ReferenceType).HasColumnName("referenceType"); b.Entity<Payment>().Property(x => x.PaidAt).HasColumnName("paidAt");
    }
}
