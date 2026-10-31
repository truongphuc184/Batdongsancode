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
            .ThenByDescending(l => l.IsVip)
            .ThenByDescending(l => l.CreatedAt)
            .ToListAsync();

        return Ok(listings.Select(l => MapToListingDto(l)));
    }

    // 2. Tìm kiếm BĐS nâng cao (Sequence Diagram 2)
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? keyword,
        [FromQuery] string? city,
        [FromQuery] PropertyType? type,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? direction)
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

        if (minPrice.HasValue)
            query = query.Where(l => l.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(l => l.Price <= maxPrice.Value);

        if (!string.IsNullOrWhiteSpace(direction))
            query = query.Where(l => l.Direction == direction);

        var results = await query
            .OrderByDescending(l => l.PriorityTime ?? DateTime.MinValue)
            .ThenByDescending(l => l.CreatedAt)
            .ToListAsync();

        return Ok(results.Select(l => MapToListingDto(l)));
    }

    // 3. Xem chi tiết BĐS + Tăng lượt xem (Sequence Diagram 2: view_count = view_count + 1)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var listing = await _context.Listings
            .Include(l => l.Owner)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing == null) return NotFound(new { message = "Không tìm thấy bất động sản" });

        // Tăng viewCount
        listing.ViewCount += 1;
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
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return Ok(listings.Select(l => MapToListingDto(l)));
    }

    // 5. [ADMIN] Lấy danh sách tin chờ duyệt (Sequence Diagram 1)
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
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return Ok(listings.Select(l => MapToListingDto(l)));
    }

    // 6. [ADMIN] Duyệt tin đăng (Sequence Diagram 1: status = 'Approved')
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

    // 7. [ADMIN] Từ chối tin đăng kèm lý do (Sequence Diagram 1: status = 'Rejected')
    [HttpPut("{id}/reject")]
    [Authorize]
    public async Task<IActionResult> RejectListing(int id, [FromBody] RejectRequest req)
    {
        var role = User.FindFirst("role")?.Value;
        if (role != "Admin")
            return Forbid();

        var listing = await _context.Listings.FindAsync(id);
        if (listing == null) return NotFound();

        listing.Status = ListingStatus.Rejected;
        listing.RejectReason = req.Reason ?? "Không đạt tiêu chuẩn kiểm duyệt";
        listing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã từ chối tin đăng." });
    }

    // 8. Đẩy tin lên đầu danh sách (Sequence Diagram 3 & Ad6)
    [HttpPut("{id}/push")]
    [Authorize]
    public async Task<IActionResult> PushListing(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var listing = await _context.Listings.FindAsync(id);
        if (listing == null) return NotFound();

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

    // 9. Cập nhật trạng thái BĐS (Đã bán, Đã cho thuê, Ẩn tin theo Ad1 & Ad2)
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

    // 10. Tạo tin đăng BĐS mới (Sequence Diagram 1)
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateListingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Tiêu đề không được để trống" });

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ" });

        if (!Enum.TryParse<PropertyType>(request.Type, true, out var pType))
            pType = PropertyType.Sale;

        var role = User.FindFirst("role")?.Value;

        // Theo Sequence Diagram 1: Tin đăng của Seller/Landlord/Agent tạo ra ở trạng thái Chờ duyệt (Pending)
        // Nếu là Admin thì duyệt luôn (Approved)
        var initialStatus = (role == "Admin") ? ListingStatus.Approved : ListingStatus.Pending;

        var listing = new Listing
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? "",
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
            : "Đăng tin thành công! Tin của bạn đang được chuyển đến Ban quản trị kiểm duyệt trước khi hiển thị.";

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

    // 12. Upload hình ảnh
    [HttpPost("upload")]
    [Authorize]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Không tìm thấy file" });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { message = "Định dạng ảnh không hỗ trợ (chỉ nhận JPG, PNG, WEBP)" });

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { url = $"/uploads/{uniqueFileName}", message = "Tải ảnh thành công" });
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
            l.Description,
            l.Address,
            l.City,
            l.District,
            l.Direction,
            l.Price,
            l.Area,
            l.Bedrooms,
            l.Bathrooms,
            l.ImageUrls,
            l.IsVip,
            l.IsVerified,
            l.ViewCount,
            l.PriorityTime,
            l.RejectReason,
            Type = l.Type.ToString(),
            Status = l.Status.ToString(),
            l.OwnerId,
            l.CreatedAt,
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
}

public class RejectRequest
{
    public string Reason { get; set; } = "";
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = "Approved";
}
