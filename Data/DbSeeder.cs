using Batdongsancode.Data;
using Batdongsancode.Models;
using Microsoft.EntityFrameworkCore;

namespace Batdongsancode.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Seed Users nếu thiếu
        var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");

        var usersToSeed = new List<User>
        {
            new User { Email = "admin@batdongsan.vn", PasswordHash = defaultPasswordHash, FullName = "Quản trị viên Hệ thống", Role = Role.Admin, IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { Email = "seller@batdongsan.vn", PasswordHash = defaultPasswordHash, FullName = "Nguyễn Văn Bán (Chính chủ)", Role = Role.Seller, IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { Email = "landlord@batdongsan.vn", PasswordHash = defaultPasswordHash, FullName = "Trần Thị Cho Thuê", Role = Role.Landlord, IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { Email = "agent@batdongsan.vn", PasswordHash = defaultPasswordHash, FullName = "Lê Hoàng Môi Giới (Chuyên BĐS)", Role = Role.Agent, IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { Email = "buyer@batdongsan.vn", PasswordHash = defaultPasswordHash, FullName = "Phạm Khách Mua", Role = Role.Buyer, IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { Email = "tenant@batdongsan.vn", PasswordHash = defaultPasswordHash, FullName = "Vũ Khách Thuê Nhà", Role = Role.Tenant, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        foreach (var u in usersToSeed)
        {
            var existing = await context.Users.FirstOrDefaultAsync(x => x.Email == u.Email);
            if (existing == null)
            {
                context.Users.Add(u);
            }
        }
        await context.SaveChangesAsync();

        // 2. Seed Tin đăng mẫu nếu chưa có
        if (!await context.Listings.AnyAsync())
        {
            var seller = await context.Users.FirstOrDefaultAsync(u => u.Role == Role.Seller) 
                         ?? await context.Users.FirstAsync();
            var landlord = await context.Users.FirstOrDefaultAsync(u => u.Role == Role.Landlord) 
                           ?? seller;
            var agent = await context.Users.FirstOrDefaultAsync(u => u.Role == Role.Agent) 
                        ?? seller;

            var sampleListings = new List<Listing>
            {
                new Listing
                {
                    Title = "Bán Biệt thự Đơn lập Vinhomes Riverside Long Biên",
                    Description = "Biệt thự view hồ điều hòa, hoàn thiện full nội thất Ý cao cấp, sân vườn rộng thoáng, an ninh 24/7. Sổ đỏ chính chủ.",
                    Type = PropertyType.Sale,
                    Price = 38500000000m,
                    Address = "Đường Hoa Lan, KĐT Vinhomes Riverside",
                    City = "Hà Nội",
                    District = "Long Biên",
                    Direction = "Đông Nam",
                    Area = 320,
                    Bedrooms = 5,
                    Bathrooms = 5,
                    ImageUrls = "https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?auto=format&fit=crop&w=1200&q=80",
                    Status = ListingStatus.Approved,
                    IsVip = true,
                    IsVerified = true,
                    ViewCount = 128,
                    OwnerId = seller.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow
                },
                new Listing
                {
                    Title = "Căn hộ cao cấp 2PN The Landmark 81 view toàn cảnh",
                    Description = "Căn hộ tầng cao, ban công thoáng mát ngắm sông và trung tâm thành phố. Đầy đủ tiện ích hồ bơi vô cực, TTTM Vincom.",
                    Type = PropertyType.Rent,
                    Price = 28000000m,
                    Address = "720A Điện Biên Phủ, Phường 22",
                    City = "TP. Hồ Chí Minh",
                    District = "Bình Thạnh",
                    Direction = "Nam",
                    Area = 78.5,
                    Bedrooms = 2,
                    Bathrooms = 2,
                    ImageUrls = "https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?auto=format&fit=crop&w=1200&q=80",
                    Status = ListingStatus.Approved,
                    IsVip = false,
                    IsVerified = true,
                    ViewCount = 95,
                    OwnerId = landlord.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow
                },
                new Listing
                {
                    Title = "Nhà phố thương mại Shophouse mặt tiền Quận 1 kinh doanh đắc địa",
                    Description = "Nhà 1 trệt 3 lầu sân thượng, vị trí đắc địa kinh doanh nhà hàng, spa hoặc cho thuê văn phòng lợi nhuận cao.",
                    Type = PropertyType.Sale,
                    Price = 18500000000m,
                    Address = "88 Hai Bà Trưng, Phường Bến Nghé",
                    City = "TP. Hồ Chí Minh",
                    District = "Quận 1",
                    Direction = "Đông",
                    Area = 110,
                    Bedrooms = 4,
                    Bathrooms = 4,
                    ImageUrls = "https://images.unsplash.com/photo-1512917774080-9991f1c4c750?auto=format&fit=crop&w=1200&q=80",
                    Status = ListingStatus.Approved,
                    IsVip = true,
                    IsVerified = true,
                    ViewCount = 210,
                    OwnerId = agent.Id,
                    CreatedAt = DateTime.UtcNow.AddHours(-12),
                    UpdatedAt = DateTime.UtcNow
                },
                new Listing
                {
                    Title = "Cho thuê căn hộ dịch vụ Studio đầy đủ tiện nghi Quận 3",
                    Description = "Căn hộ mini mới tinh, giờ giấc tự do, có bảo vệ, thang máy, wifi tốc độ cao, máy giặt riêng.",
                    Type = PropertyType.Rent,
                    Price = 8500000m,
                    Address = "150 Lê Văn Sỹ, Phường 10",
                    City = "TP. Hồ Chí Minh",
                    District = "Quận 3",
                    Direction = "Tây",
                    Area = 35,
                    Bedrooms = 1,
                    Bathrooms = 1,
                    ImageUrls = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=1200&q=80",
                    Status = ListingStatus.Pending, // Tin đang chờ admin kiểm duyệt
                    IsVip = false,
                    IsVerified = false,
                    ViewCount = 0,
                    OwnerId = landlord.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Listings.AddRange(sampleListings);
            await context.SaveChangesAsync();
        }
    }
}
