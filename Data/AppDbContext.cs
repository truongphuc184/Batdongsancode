using Microsoft.EntityFrameworkCore;
using Batdongsancode.Models;

namespace Batdongsancode.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configure enum storage as string
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        modelBuilder.Entity<Listing>()
            .Property(l => l.Type)
            .HasConversion<string>();
        modelBuilder.Entity<Listing>()
            .Property(l => l.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Lead>()
            .Property(ld => ld.Status)
            .HasConversion<string>();

        // Tránh lỗi Multiple Cascade Paths trong SQL Server
        modelBuilder.Entity<Lead>()
            .HasOne(ld => ld.Owner)
            .WithMany()
            .HasForeignKey(ld => ld.OwnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Lead>()
            .HasOne(ld => ld.Listing)
            .WithMany()
            .HasForeignKey(ld => ld.ListingId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Report>()
            .HasOne(r => r.Listing)
            .WithMany()
            .HasForeignKey(r => r.ListingId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

