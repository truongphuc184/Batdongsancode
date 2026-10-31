// Seed script - run once to create admin account
using Batdongsancode.Data;
using Batdongsancode.Models;
using Microsoft.EntityFrameworkCore;

namespace Batdongsancode.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Users.AnyAsync())
            return; // Already seeded

        var admin = new User
        {
            Email = "admin@batdongsan.vn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            FullName = "Quản trị viên",
            Role = Role.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var buyer = new User
        {
            Email = "buyer@batdongsan.vn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            FullName = "Người mua",
            Role = Role.Buyer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(admin, buyer);
        await context.SaveChangesAsync();
    }
}
