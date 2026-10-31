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
    }
}

