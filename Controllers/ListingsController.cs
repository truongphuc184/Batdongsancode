using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Batdongsancode.Models;
using Batdongsancode.Services.Interfaces;
using Batdongsancode.Data;
using Microsoft.EntityFrameworkCore;

namespace Batdongsancode.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ListingsController(AppDbContext context)
    {
        _context = context;
    }

    // 1. Lấy danh sách tin đã được duyệt (Hiển thị trang chủ)
    // Sắp xếp: Tin VIP/Đẩy tin lên đầu (PriorityTime), sau đó đến ngày tạo
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PropertyType? type, [FromQuery] string? city)
    {
        var query = _context.Listings
            .Include(l => l.Owner)
            .Where(l => l.Status == ListingStatus.Approved);

        if (type.HasValue)
            query = query.Where(l => l.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(l => l.City.Contains(city));

        var listings = await query
            .OrderByDescending(l => l.PriorityTime ?? DateTime.MinValue)
            .ThenByDescending(l => l.IsVip ?? false)
            .ThenByDescending(l => l.CreatedAt ?? DateTime.MinValue)
            .ToListAsync();

        return Ok(listings.Select(l => MapToListingDto(l)));
    }

    // 1.1 Thống kê xu hướng thị trường & Phân loại loại hình BĐS (Market Analytics & Trend Dashboard)
    [HttpGet("market-trends")]
    public async Task<IActionResult> GetMarketTrends()
    {
        var allListings = await _context.Listings
            .AsNoTracking()
            .ToListAsync();

        string ClassifyListing(string title, string desc)
        {
            var text = ((title ?? "") + " " + (desc ?? "")).ToLower();
            if (text.Contains("căn hộ") || text.Contains("chung cư") || text.Contains("duplex") || text.Contains("penthouse") || text.Contains("studio") || text.Contains("officetel"))
                return "Căn hộ / Chung cư";
            if (text.Contains("biệt thự") || text.Contains("villa") || text.Contains("dinh thự"))
                return "Biệt thự";
            if (text.Contains("nhà phố") || text.Contains("liền kề") || text.Contains("nhà riêng") || text.Contains("nguyên căn") || text.Contains("nhà ngõ"))
                return "Nhà phố / Nhà riêng";
            if (text.Contains("đất nền") || text.Contains("lô đất") || text.Contains("thổ cư") || text.Contains("đất dự án"))
                return "Đất nền";
            if (text.Contains("mặt bằng") || text.Contains("shophouse") || text.Contains("cửa hàng") || text.Contains("kiot"))
                return "Mặt bằng / Shophouse";
            if (text.Contains("nhà xưởng") || text.Contains("kho bãi") || text.Contains("kho") || text.Contains("xưởng"))
                return "Kho xưởng / Bãi";
            return "Bất động sản khác";
        }

        var total = allListings.Count;
        var approvedTotal = allListings.Count(l => l.Status == ListingStatus.Approved);
        var pendingTotal = allListings.Count(l => l.Status == ListingStatus.Pending);
        var rejectedTotal = allListings.Count(l => l.Status == ListingStatus.Rejected);
        var totalSale = allListings.Count(l => l.Type == PropertyType.Sale);
        var totalRent = allListings.Count(l => l.Type == PropertyType.Rent);

        var groups = allListings
            .GroupBy(l => ClassifyListing(l.Title, l.Description))
            .Select(g =>
            {
                var count = g.Count();
                var pct = total > 0 ? Math.Round((double)count / total * 100, 1) : 0;
                var saleCount = g.Count(l => l.Type == PropertyType.Sale);
                var rentCount = g.Count(l => l.Type == PropertyType.Rent);
                var approvedCount = g.Count(l => l.Status == ListingStatus.Approved);
                var pendingCount = g.Count(l => l.Status == ListingStatus.Pending);
                var avgPriceSale = g.Where(l => l.Type == PropertyType.Sale && l.Price > 0).Select(l => l.Price).DefaultIfEmpty(0).Average();
                var avgPriceRent = g.Where(l => l.Type == PropertyType.Rent && l.Price > 0).Select(l => l.Price).DefaultIfEmpty(0).Average();
                var avgArea = g.Where(l => (l.Area ?? 0) > 0).Select(l => l.Area!.Value).DefaultIfEmpty(0).Average();

                string icon = g.Key switch
                {
                    "Căn hộ / Chung cư" => "🏢",
                    "Biệt thự" => "🏰",
                    "Nhà phố / Nhà riêng" => "🏡",
                    "Đất nền" => "🏞️",
                    "Mặt bằng / Shophouse" => "🏬",
                    "Kho xưởng / Bãi" => "🏭",
                    _ => "🏠"
                };

                return new
                {
                    Category = g.Key,
                    Icon = icon,
                    Count = count,
                    Percentage = pct,
                    SaleCount = saleCount,
                    RentCount = rentCount,
                    ApprovedCount = approvedCount,
                    PendingCount = pendingCount,
                    AvgPriceSale = Math.Round(avgPriceSale, 0),
                    AvgPriceRent = Math.Round(avgPriceRent, 0),
                    AvgArea = Math.Round(avgArea, 1)
                };
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var top = groups.FirstOrDefault();

        return Ok(new
        {
            TotalListings = total,
            ApprovedListings = approvedTotal,
            PendingListings = pendingTotal,
            RejectedListings = rejectedTotal,
            TotalSale = totalSale,
            TotalRent = totalRent,
            TopCategory = top?.Category ?? "Chưa có",
            TopCategoryIcon = top?.Icon ?? "🏢",
            TopCategoryCount = top?.Count ?? 0,
            TopCategoryPercentage = top?.Percentage ?? 0,
            Categories = groups
        });
    }

    // 2. Tìm kiếm BĐS nâng cao & Sắp xếp
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? keyword,
        [FromQuery] string? city,
        [FromQuery] PropertyType? type,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] double? minArea,
        [FromQuery] double? maxArea,
        [FromQuery] int? bedrooms,
        [FromQuery] int? bathrooms,
        [FromQuery] string? direction,
        [FromQuery] string? category,
        [FromQuery] string? legal,
        [FromQuery] string? furniture,
        [FromQuery] string? project,
        [FromQuery] string? sortBy)
    {
        var query = _context.Listings
            .Include(l => l.Owner)
            .Where(l => l.Status == ListingStatus.Approved);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(l => l.Title.ToLower().Contains(kw) 
                                  || l.Description.ToLower().Contains(kw) 
                                  || l.Address.ToLower().Contains(kw) 
                                  || l.District.ToLower().Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(l => l.City.ToLower().Contains(city.ToLower()));

        if (type.HasValue)
            query = query.Where(l => l.Type == type.Value);

        if (minPrice.HasValue && minPrice.Value > 0)
            query = query.Where(l => l.Price >= minPrice.Value);

        if (maxPrice.HasValue && maxPrice.Value > 0)
            query = query.Where(l => l.Price <= maxPrice.Value);

        if (minArea.HasValue && minArea.Value > 0)
            query = query.Where(l => (l.Area ?? 0) >= minArea.Value);

        if (maxArea.HasValue && maxArea.Value > 0)
            query = query.Where(l => (l.Area ?? 0) <= maxArea.Value);

        if (bedrooms.HasValue && bedrooms.Value > 0)
            query = query.Where(l => (l.Bedrooms ?? 0) >= bedrooms.Value);

        if (bathrooms.HasValue && bathrooms.Value > 0)
            query = query.Where(l => (l.Bathrooms ?? 0) >= bathrooms.Value);

        if (!string.IsNullOrWhiteSpace(direction))
            query = query.Where(l => l.Direction == direction);

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLower();
            query = query.Where(l => l.Title.ToLower().Contains(cat) || l.Description.ToLower().Contains(cat));
        }

        if (!string.IsNullOrWhiteSpace(legal))
        {
            var leg = legal.Trim().ToLower();
            query = query.Where(l => l.Title.ToLower().Contains(leg) || l.Description.ToLower().Contains(leg));
        }

        if (!string.IsNullOrWhiteSpace(furniture))
        {
            var furn = furniture.Trim().ToLower();
            query = query.Where(l => l.Title.ToLower().Contains(furn) || l.Description.ToLower().Contains(furn));
        }

        if (!string.IsNullOrWhiteSpace(project))
        {
            var proj = project.Trim().ToLower();
            query = query.Where(l => l.Title.ToLower().Contains(proj) || l.Description.ToLower().Contains(proj) || l.Address.ToLower().Contains(proj));
        }

        // Sắp xếp kết quả tìm kiếm
        query = (sortBy?.ToLower()) switch
        {
            "price_asc" => query.OrderBy(l => l.Price),
            "price_desc" => query.OrderByDescending(l => l.Price),
            "area_desc" => query.OrderByDescending(l => l.Area ?? 0),
            "newest" => query.OrderByDescending(l => l.CreatedAt ?? DateTime.MinValue),
            _ => query.OrderByDescending(l => l.PriorityTime ?? DateTime.MinValue)
                      .ThenByDescending(l => l.IsVip ?? false)
                      .ThenByDescending(l => l.CreatedAt ?? DateTime.MinValue)
        };

        var results = await query.ToListAsync();
        return Ok(results.Select(l => MapToListingDto(l)));
    }

    // 3. Xem chi tiết BĐS + Tăng lượt xem (view_count = view_count + 1)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var listing = await _context.Listings
            .Include(l => l.Owner)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing == null) return NotFound(new { message = "Không tìm thấy bất động sản" });

        // Tăng viewCount
        listing.ViewCount = (listing.ViewCount ?? 0) + 1;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            listing.Id,
            listing.Title,
            listing.Description,
            listing.Address,
            listing.City,
            listing.District,
            listing.Direction,
            listing.Price,
            listing.Area,
            listing.Bedrooms,
            listing.Bathrooms,
            listing.ImageUrls,
            listing.IsVip,
            listing.IsVerified,
            listing.ViewCount,
            Type = listing.Type.ToString(),
            Status = listing.Status.ToString(),
            listing.RejectReason,
            listing.OwnerId,
            listing.CreatedAt,
            listing.UpdatedAt,
            OwnerName = listing.Owner?.FullName,
            OwnerEmail = listing.Owner?.Email,
            OwnerRole = listing.Owner?.Role.ToString()
        });
    }

    // 4. Lấy danh sách tin của chính người đăng (Người bán/Cho thuê/Môi giới)
    [HttpGet("my-listings")]
    [Authorize]
    public async Task<IActionResult> GetMyListings()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var listings = await _context.Listings
            .Where(l => l.OwnerId == userId.Value)
            .OrderByDescending(l => l.CreatedAt ?? DateTime.MinValue)
            .ToListAsync();

        return Ok(listings.Select(l => MapToListingDto(l)));
    }

    // 5. [ADMIN] Lấy danh sách tin chờ duyệt
    [HttpGet("pending")]
    [Authorize]
    public async Task<IActionResult> GetPendingListings()
    {
        var role = User.FindFirst("role")?.Value;
        if (role != "Admin")
            return Forbid();

        var listings = await _context.Listings
            .Include(l => l.Owner)
            .Where(l => l.Status == ListingStatus.Pending)
            .OrderByDescending(l => l.CreatedAt ?? DateTime.MinValue)
            .ToListAsync();

        return Ok(listings.Select(l => MapToListingDto(l)));
    }

    // 6. [ADMIN] Duyệt tin đăng (status = 'Approved')
    [HttpPut("{id}/approve")]
    [Authorize]
    public async Task<IActionResult> ApproveListing(int id)
    {
        var role = User.FindFirst("role")?.Value;
        if (role != "Admin")
            return Forbid();

        var listing = await _context.Listings.FindAsync(id);
        if (listing == null) return NotFound();

        listing.Status = ListingStatus.Approved;
        listing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã duyệt tin đăng thành công. Tin hiện đã được công khai!" });
    }

    // 7. [ADMIN] Từ chối tin đăng kèm lý do (status = 'Rejected')
    [HttpPut("{id}/reject")]
    [Authorize]
    public async Task<IActionResult> RejectListing(int id, [FromBody] RejectRequest? req)
    {
        var role = User.FindFirst("role")?.Value;
        if (role != "Admin")
            return Forbid();

        var listing = await _context.Listings.FindAsync(id);
        if (listing == null) return NotFound();

        var reason = !string.IsNullOrWhiteSpace(req?.Reason) 
            ? req.Reason.Trim() 
            : "Không đạt tiêu chuẩn kiểm duyệt nội dung của sàn";

        listing.Status = ListingStatus.Rejected;
        listing.RejectReason = reason;
        listing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Đã từ chối tin đăng thành công. Lý do: '{reason}'" });
    }

    // 8. Đẩy tin lên đầu danh sách
    [HttpPut("{id}/push")]
    [Authorize]
    public async Task<IActionResult> PushListing(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var listing = await _context.Listings.FindAsync(id);
        if (listing == null) return NotFound();

        // CHẶN ĐẨY TIN ĐỐI VỚI TIN BỊ TỪ CHỐI HOẶC CHƯA ĐƯỢC DUYỆT
        if (listing.Status == ListingStatus.Rejected)
        {
            return BadRequest(new 
            { 
                code = "LISTING_REJECTED", 
                message = $"Không thể đẩy tin! Tin đăng này đã bị Ban quản trị từ chối kiểm duyệt (Lý do: {listing.RejectReason ?? "Nội dung chưa đạt tiêu chuẩn"}). Bạn không thể sử dụng gói đẩy tin cho tin đã bị từ chối." 
            });
        }

        if (listing.Status == ListingStatus.Pending)
        {
            return BadRequest(new 
            { 
                code = "LISTING_PENDING", 
                message = "Tin đăng đang trong trạng thái chờ Ban quản trị duyệt. Bạn chỉ có thể sử dụng tính năng đẩy tin sau khi tin đã được phê duyệt công khai!" 
            });
        }

        // Chỉ chủ tin hoặc Admin mới được đẩy tin
        var role = User.FindFirst("role")?.Value;
        if (listing.OwnerId != userId.Value && role != "Admin")
            return Forbid();

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null) return NotFound();

        if (role != "Admin")
        {
            if (user.RemainingPushes <= 0)
            {
                return BadRequest(new 
                { 
                    code = "OUT_OF_PUSHES", 
                    message = "Bạn đã sử dụng hết lượt đẩy tin. Vui lòng nâng cấp Gói hội viên để nhận thêm lượt đẩy!" 
                });
            }

            user.RemainingPushes -= 1;
        }

        listing.PriorityTime = DateTime.UtcNow;
        listing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var remainingText = (role == "Admin") ? "vô hạn (Quyền Admin)" : $"{user.RemainingPushes} lượt";
        return Ok(new 
        { 
            message = $"🚀 Đã đẩy tin lên đầu trang thành công! Số lượt đẩy còn lại: {remainingText}",
            remainingPushes = user.RemainingPushes
        });
    }

    // 9. Cập nhật trạng thái BĐS (Đã bán, Đã cho thuê, Ẩn tin)
    [HttpPut("{id}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest req)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var listing = await _context.Listings.FindAsync(id);
        if (listing == null) return NotFound();

        var role = User.FindFirst("role")?.Value;
        if (listing.OwnerId != userId.Value && role != "Admin")
            return Forbid();

        if (Enum.TryParse<ListingStatus>(req.Status, true, out var newStatus))
        {
            listing.Status = newStatus;
            listing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã cập nhật trạng thái tin thành: {newStatus}" });
        }

        return BadRequest(new { message = "Trạng thái không hợp lệ" });
    }

    // 10. Tạo tin đăng BĐS mới
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateListingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Tiêu đề không được để trống" });

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ" });

        // Kiểm tra quyền từ Database: Người mua (Buyer) và Người thuê (Tenant) không được phép đăng tin
        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
            return Unauthorized(new { message = "Không tìm thấy thông tin tài khoản người dùng" });

        if (user.Role == Role.Admin)
        {
            return StatusCode(403, new { 
                message = "Tài khoản Quản trị viên (Admin) chỉ thực hiện nhiệm vụ quản trị hệ thống và kiểm duyệt tin, không tham gia đăng tin bất động sản." 
            });
        }

        bool canPost = user.Role.HasFlag(Role.Seller) || user.Role.HasFlag(Role.Landlord) || user.Role.HasFlag(Role.Agent);
        if (!canPost)
        {
            return StatusCode(403, new { 
                message = "Tài khoản của bạn chưa có quyền đăng tin. Vui lòng vào Hồ sơ cá nhân chọn vai trò Người bán, Cho thuê hoặc Môi giới để đăng tin bất động sản!" 
            });
        }

        if (!Enum.TryParse<PropertyType>(request.Type, true, out var pType))
            pType = PropertyType.Sale;

        var role = user.Role.ToString();

        // Tin đăng của Seller/Landlord/Agent tạo ra ở trạng thái Chờ duyệt (Pending)
        // Nếu là Admin thì duyệt luôn (Approved)
        var initialStatus = (role == "Admin") ? ListingStatus.Approved : ListingStatus.Pending;

        var desc = request.Description?.Trim() ?? "";
        var extraDetails = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Category)) extraDetails.Add($"Loại BĐS: {request.Category.Trim()}");
        if (!string.IsNullOrWhiteSpace(request.Legal)) extraDetails.Add($"Pháp lý: {request.Legal.Trim()}");
        if (!string.IsNullOrWhiteSpace(request.Furniture)) extraDetails.Add($"Nội thất: {request.Furniture.Trim()}");
        if (!string.IsNullOrWhiteSpace(request.Project)) extraDetails.Add($"Dự án: {request.Project.Trim()}");

        if (extraDetails.Count > 0 && !desc.Contains("Loại BĐS:"))
        {
            desc = desc + (string.IsNullOrWhiteSpace(desc) ? "" : "\n\n") + "[Thông tin chi tiết]: " + string.Join(" • ", extraDetails);
        }

        var listing = new Listing
        {
            Title = request.Title.Trim(),
            Description = desc,
            Type = pType,
            Price = request.Price > 0 ? request.Price : 0,
            Address = request.Address?.Trim() ?? "",
            City = request.City?.Trim() ?? "",
            District = request.District?.Trim() ?? "",
            Direction = request.Direction?.Trim() ?? "",
            Area = request.Area > 0 ? request.Area : 0,
            Bedrooms = request.Bedrooms > 0 ? request.Bedrooms : 0,
            Bathrooms = request.Bathrooms > 0 ? request.Bathrooms : 0,
            ImageUrls = request.ImageUrls ?? "",
            Status = initialStatus,
            ViewCount = 0,
            IsVip = false,
            IsVerified = false,
            OwnerId = userId.Value,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Listings.Add(listing);
        await _context.SaveChangesAsync();

        var msg = (initialStatus == ListingStatus.Approved) 
            ? "Đăng tin thành công và đã hiển thị công khai!" 
            : "Tin đăng đã gửi và đang chờ duyệt! Ban quản trị sẽ kiểm duyệt tin trước khi hiển thị.";

        return Ok(new { id = listing.Id, status = initialStatus.ToString(), message = msg });
    }

    // 11. Xóa tin đăng
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var listing = await _context.Listings.FindAsync(id);
        if (listing == null) return NotFound();

        var role = User.FindFirst("role")?.Value;
        if (listing.OwnerId != userId.Value && role != "Admin")
            return Forbid();

        _context.Listings.Remove(listing);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa tin đăng thành công" });
    }

    // 12. Upload hình ảnh (Hỗ trợ upload 1 hoặc nhiều ảnh cùng lúc)
    [HttpPost("upload")]
    [Authorize]
    public async Task<IActionResult> UploadImage([FromForm] List<IFormFile>? files, IFormFile? file)
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            var user = await _context.Users.FindAsync(userId.Value);
            if (user != null)
            {
                bool canUpload = user.Role.HasFlag(Role.Seller) || user.Role.HasFlag(Role.Landlord) || user.Role.HasFlag(Role.Agent) || user.Role.HasFlag(Role.Admin);
                if (!canUpload)
                {
                    return StatusCode(403, new { message = "Tài khoản Người mua không có quyền tải ảnh đăng tin." });
                }
            }
        }

        var allFiles = new List<IFormFile>();
        if (files != null && files.Count > 0) allFiles.AddRange(files);
        if (file != null && !allFiles.Contains(file)) allFiles.Add(file);

        if (allFiles.Count == 0)
            return BadRequest(new { message = "Không tìm thấy file tải lên" });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var urls = new List<string>();
        foreach (var f in allFiles)
        {
            if (f.Length == 0) continue;
            var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext)) continue;

            var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await f.CopyToAsync(stream);
            }
            urls.Add($"/uploads/{uniqueFileName}");
        }

        if (urls.Count == 0)
            return BadRequest(new { message = "Không có ảnh hợp lệ nào được tải lên (chỉ nhận JPG, PNG, WEBP)" });

        return Ok(new 
        { 
            url = urls[0], 
            urls = urls, 
            joinedUrls = string.Join(",", urls),
            count = urls.Count,
            message = $"Đã tải lên {urls.Count} hình ảnh thành công" 
        });
    }

    private int? GetCurrentUserId()
    {
        var subClaim = User.FindFirst("sub")?.Value 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(subClaim, out var id) ? id : null;
    }

    private static object MapToListingDto(Listing l)
    {
        return new
        {
            l.Id,
            l.Title,
            Description = l.Description ?? "",
            Address = l.Address ?? "",
            City = l.City ?? "",
            District = l.District ?? "",
            Direction = l.Direction ?? "",
            l.Price,
            Area = l.Area ?? 0,
            Bedrooms = l.Bedrooms ?? 0,
            Bathrooms = l.Bathrooms ?? 0,
            ImageUrls = l.ImageUrls ?? "",
            IsVip = l.IsVip ?? false,
            IsVerified = l.IsVerified ?? true,
            ViewCount = l.ViewCount ?? 0,
            l.PriorityTime,
            RejectReason = l.RejectReason ?? "",
            Type = l.Type.ToString(),
            Status = l.Status.ToString(),
            l.OwnerId,
            CreatedAt = l.CreatedAt ?? DateTime.UtcNow,
            l.UpdatedAt,
            OwnerName = l.Owner?.FullName
        };
    }
}

public class CreateListingRequest
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = "";
    public string Type { get; set; } = "Sale";
    public decimal Price { get; set; }
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string District { get; set; } = "";
    public string Direction { get; set; } = "";
    public double Area { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public string? ImageUrls { get; set; }
    public string? Category { get; set; }
    public string? Legal { get; set; }
    public string? Furniture { get; set; }
    public string? Project { get; set; }
}

public class RejectRequest
{
    public string Reason { get; set; } = "";
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = "Approved";
}
