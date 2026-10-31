using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Batdongsancode.Data;
using Batdongsancode.Models;
using Microsoft.EntityFrameworkCore;

namespace Batdongsancode.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SubscriptionsController(AppDbContext context)
    {
        _context = context;
    }

    // 1. Xem danh sách các gói dịch vụ hội viên (Use Case 1 & Ad5)
    [HttpGet("packages")]
    public IActionResult GetPackages()
    {
        var packages = new[]
        {
            new
            {
                Id = "basic",
                Name = "Hội viên Cơ bản",
                Subtitle = "Phù hợp với môi giới mới hoặc giỏ hàng nhỏ",
                Price = 467000,
                OriginalPrice = 675000,
                Discount = "31%",
                Savings = "208.000 đ/tháng",
                Pushes = 15,
                VipPosts = 0,
                Features = new[]
                {
                    "15 voucher Đăng tin thường (Giảm 35.000 đ/lần)",
                    "15 lượt Đẩy tin thường lên đầu trang",
                    "Hỗ trợ đăng tin nhanh chóng",
                    "Thống kê lượt xem tin đăng"
                },
                IsHot = false
            },
            new
            {
                Id = "standard",
                Name = "Hội viên Tiêu chuẩn",
                Subtitle = "Phù hợp với môi giới chuyên nghiệp có giỏ hàng từ 10 BĐS",
                Price = 1267000,
                OriginalPrice = 1890000,
                Discount = "33%",
                Savings = "623.000 đ/tháng",
                Pushes = 30,
                VipPosts = 1,
                Features = new[]
                {
                    "30 voucher Đăng tin thường",
                    "30 lượt Đẩy tin thường lên đầu trang",
                    "1 voucher Đăng tin VIP nổi bật (Giảm 400.000 đ)",
                    "Bản quyền ảnh & Đóng dấu watermark",
                    "Hẹn giờ đăng tin tự động",
                    "Báo cáo hiệu suất & Thị trường BĐS"
                },
                IsHot = true
            },
            new
            {
                Id = "premium",
                Name = "Hội viên Cao cấp",
                Subtitle = "Phù hợp với môi giới có giỏ hàng và ngân sách quảng cáo lớn",
                Price = 2200000,
                OriginalPrice = 3590000,
                Discount = "39%",
                Savings = "1.390.000 đ/tháng",
                Pushes = 50,
                VipPosts = 3,
                Features = new[]
                {
                    "50 voucher Đăng tin thường",
                    "50 lượt Đẩy tin thường ưu tiên cao nhất",
                    "3 voucher Đăng tin VIP đặc biệt",
                    "Huy hiệu Môi giới Chuyên nghiệp / Uy tín",
                    "Bản quyền ảnh & Hẹn giờ đăng tin",
                    "Báo cáo hiệu suất chuyên sâu"
                },
                IsHot = false
            }
        };

        return Ok(packages);
    }

    // 2. Lấy trạng thái gói và số lượt đẩy tin của user hiện tại
    [HttpGet("my-status")]
    [Authorize]
    public async Task<IActionResult> GetMyStatus()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.FullName,
            user.Email,
            Role = user.Role.ToString(),
            user.CurrentPackage,
            user.RemainingPushes
        });
    }

    // 3. Mua gói dịch vụ & Thanh toán (Sequence Diagram 4 & Ad5)
    [HttpPost("buy")]
    [Authorize]
    public async Task<IActionResult> BuyPackage([FromBody] BuyPackageRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null) return NotFound();

        int addedPushes = 0;
        string packageName = "";

        switch (request.PackageId?.ToLower())
        {
            case "basic":
                addedPushes = 15;
                packageName = "Hội viên Cơ bản";
                break;
            case "standard":
                addedPushes = 30;
                packageName = "Hội viên Tiêu chuẩn";
                break;
            case "premium":
                addedPushes = 50;
                packageName = "Hội viên Cao cấp";
                break;
            default:
                return BadRequest(new { message = "Gói dịch vụ không hợp lệ" });
        }

        // Mô phỏng quy trình thanh toán thành công (Sequence Diagram 4: Webhook / Callback -> Success)
        user.CurrentPackage = packageName;
        user.RemainingPushes += addedPushes;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = $"Thanh toán thành công qua {request.PaymentMethod ?? "Cổng thanh toán"}! Bạn đã được nâng cấp lên gói '{packageName}' và cộng thêm {addedPushes} lượt đẩy tin.",
            currentPackage = user.CurrentPackage,
            remainingPushes = user.RemainingPushes
        });
    }

    private int? GetCurrentUserId()
    {
        var subClaim = User.FindFirst("sub")?.Value 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(subClaim, out var id) ? id : null;
    }
}

public class BuyPackageRequest
{
    public string PackageId { get; set; } = "standard";
    public string PaymentMethod { get; set; } = "VNPay QR";
}

