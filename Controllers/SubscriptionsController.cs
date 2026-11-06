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

    public class PackageTransaction
    {
        public string TransactionCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string PackageId { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string PaymentMethod { get; set; } = "VNPAY QR";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Thành công";
        public bool IsRetail { get; set; }
    }

    private static readonly List<PackageTransaction> _transactions = new()
    {
        new PackageTransaction { TransactionCode = "PAY-2099", UserName = "Nguyễn Văn Bán (Chính chủ)", UserEmail = "seller@batdongsan.vn", PackageId = "push_owner_10", PackageName = "Gói Chính chủ 10 lượt (Vĩnh viễn)", Amount = 99000, PaymentMethod = "VNPAY QR", CreatedAt = DateTime.UtcNow.AddMinutes(-35), IsRetail = true },
        new PackageTransaction { TransactionCode = "PAY-2098", UserName = "Vũ Đình Cường (Chủ biệt thự)", UserEmail = "cuong.vudinh@gmail.com", PackageId = "push_owner_10", PackageName = "Gói Chính chủ 10 lượt (Vĩnh viễn)", Amount = 99000, PaymentMethod = "Ví MoMo", CreatedAt = DateTime.UtcNow.AddHours(-2), IsRetail = true },
        new PackageTransaction { TransactionCode = "PAY-2088", UserName = "Lê Hoàng Môi Giới BĐS", UserEmail = "agent@batdongsan.vn", PackageId = "premium", PackageName = "Hội viên Cao cấp", Amount = 2200000, PaymentMethod = "VNPAY QR", CreatedAt = DateTime.UtcNow.AddHours(-5), IsRetail = false },
        new PackageTransaction { TransactionCode = "PAY-2075", UserName = "Trần Thanh Phong (Sàn CenLand)", UserEmail = "phong.tran@cenland.vn", PackageId = "standard", PackageName = "Hội viên Tiêu chuẩn", Amount = 1267000, PaymentMethod = "Chuyển khoản VietQR", CreatedAt = DateTime.UtcNow.AddHours(-11), IsRetail = false },
        new PackageTransaction { TransactionCode = "PAY-2065", UserName = "Nguyễn Văn Bán (Chính chủ)", UserEmail = "seller@batdongsan.vn", PackageId = "standard", PackageName = "Hội viên Tiêu chuẩn", Amount = 1267000, PaymentMethod = "Ví MoMo", CreatedAt = DateTime.UtcNow.AddHours(-18), IsRetail = false },
        new PackageTransaction { TransactionCode = "PAY-2055", UserName = "Hoàng Thị Kim Liên", UserEmail = "lien.kimhoang@gmail.com", PackageId = "push_weekend", PackageName = "Gói Cuối tuần vàng (4 lượt)", Amount = 49000, PaymentMethod = "VNPAY QR", CreatedAt = DateTime.UtcNow.AddHours(-23), IsRetail = true },
        new PackageTransaction { TransactionCode = "PAY-2049", UserName = "Phan Văn Nam (Chủ nhà)", UserEmail = "chuduan.nam@gmail.com", PackageId = "push_weekend", PackageName = "Gói Cuối tuần vàng (4 lượt)", Amount = 49000, PaymentMethod = "Ví MoMo", CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-4), IsRetail = true },
        new PackageTransaction { TransactionCode = "PAY-2042", UserName = "Trần Thị Cho Thuê", UserEmail = "landlord@batdongsan.vn", PackageId = "basic", PackageName = "Hội viên Cơ bản", Amount = 467000, PaymentMethod = "Chuyển khoản VietQR", CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-9), IsRetail = false },
        new PackageTransaction { TransactionCode = "PAY-2039", UserName = "Hoàng Thị Mai (Chủ căn hộ)", UserEmail = "maihoang.bds@gmail.com", PackageId = "push_quick_3", PackageName = "Combo Bán nhanh 3 lượt", Amount = 39000, PaymentMethod = "VNPAY QR", CreatedAt = DateTime.UtcNow.AddDays(-2), IsRetail = true },
        new PackageTransaction { TransactionCode = "PAY-2038", UserName = "Phạm Quang Dũng", UserEmail = "dung.pham@yahoo.com", PackageId = "push_quick_3", PackageName = "Combo Bán nhanh 3 lượt", Amount = 39000, PaymentMethod = "Chuyển khoản VietQR", CreatedAt = DateTime.UtcNow.AddDays(-2).AddHours(-7), IsRetail = true },
        new PackageTransaction { TransactionCode = "PAY-2035", UserName = "Ngô Bảo Long (Môi giới Thủ Đức)", UserEmail = "long.ngo@thuduc-land.com", PackageId = "standard", PackageName = "Hội viên Tiêu chuẩn", Amount = 1267000, PaymentMethod = "VNPAY QR", CreatedAt = DateTime.UtcNow.AddDays(-3), IsRetail = false },
        new PackageTransaction { TransactionCode = "PAY-2025", UserName = "Đinh Gia Huy (Chủ liền kề)", UserEmail = "huy.dinhgia@gmail.com", PackageId = "push_owner_10", PackageName = "Gói Chính chủ 10 lượt (Vĩnh viễn)", Amount = 99000, PaymentMethod = "Ví MoMo", CreatedAt = DateTime.UtcNow.AddDays(-3).AddHours(-10), IsRetail = true },
        new PackageTransaction { TransactionCode = "PAY-2020", UserName = "Nguyễn Minh Châu", UserEmail = "chau.nguyen@vinhomes.vn", PackageId = "premium", PackageName = "Hội viên Cao cấp", Amount = 2200000, PaymentMethod = "Chuyển khoản VietQR", CreatedAt = DateTime.UtcNow.AddDays(-4), IsRetail = false },
        new PackageTransaction { TransactionCode = "PAY-2015", UserName = "Đỗ Đức Toàn", UserEmail = "toan.doduc@gmail.com", PackageId = "push_single", PackageName = "Đẩy tin lẻ 1 lượt", Amount = 15000, PaymentMethod = "Chuyển khoản VietQR", CreatedAt = DateTime.UtcNow.AddDays(-4).AddHours(-5), IsRetail = true },
        new PackageTransaction { TransactionCode = "PAY-2010", UserName = "Bùi Thu Trang", UserEmail = "trang.buithu@gmail.com", PackageId = "push_single", PackageName = "Đẩy tin lẻ 1 lượt", Amount = 15000, PaymentMethod = "Ví MoMo", CreatedAt = DateTime.UtcNow.AddDays(-5), IsRetail = true },
        new PackageTransaction { TransactionCode = "PAY-2005", UserName = "Võ Thành Đạt", UserEmail = "dat.vothanh@gmail.com", PackageId = "basic", PackageName = "Hội viên Cơ bản", Amount = 467000, PaymentMethod = "VNPAY QR", CreatedAt = DateTime.UtcNow.AddDays(-6), IsRetail = false }
    };

    public SubscriptionsController(AppDbContext context)
    {
        _context = context;
    }

    // 1. Xem danh sách các gói dịch vụ hội viên
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

        bool canPost = user.Role.HasFlag(Role.Seller) || user.Role.HasFlag(Role.Landlord) || user.Role.HasFlag(Role.Agent);
        if (!canPost)
        {
            return StatusCode(403, new 
            { 
                message = "Gói dịch vụ hội viên chỉ dành cho tài khoản có quyền đăng tin (Người bán, Cho thuê, Môi giới)." 
            });
        }

        return Ok(new
        {
            user.FullName,
            user.Email,
            Role = user.Role.ToString(),
            user.CurrentPackage,
            user.RemainingPushes
        });
    }

    // 3. Mua gói dịch vụ & Thanh toán
    [HttpPost("buy")]
    [Authorize]
    public async Task<IActionResult> BuyPackage([FromBody] BuyPackageRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null) return NotFound();

        // Chỉ tài khoản có quyền đăng tin mới được mua gói dịch vụ
        bool canPost = user.Role.HasFlag(Role.Seller) || user.Role.HasFlag(Role.Landlord) || user.Role.HasFlag(Role.Agent);
        if (!canPost)
        {
            return StatusCode(403, new 
            { 
                message = "Gói dịch vụ hội viên chỉ dành riêng cho tài khoản có quyền đăng tin (Người bán, Cho thuê, Môi giới). Tài khoản Người mua, Người thuê hoặc Quản trị viên không thể mua các gói dịch vụ này." 
            });
        }

        int addedPushes = 0;
        string packageName = "";
        long packagePrice = 0;
        bool isRetail = false;

        switch (request.PackageId?.ToLower())
        {
            // Các gói lẻ & gói combo chính chủ (1-2 lần/năm, không giới hạn thời gian)
            case "push_single":
                addedPushes = 1;
                packagePrice = 15000;
                packageName = "Đẩy tin lẻ 1 lượt";
                isRetail = true;
                break;
            case "push_quick_3":
                addedPushes = 3;
                packagePrice = 39000;
                packageName = "Combo Bán nhanh 3 lượt";
                isRetail = true;
                break;
            case "push_weekend":
                addedPushes = 4;
                packagePrice = 49000;
                packageName = "Gói Cuối tuần vàng (4 lượt)";
                isRetail = true;
                break;
            case "push_owner_10":
                addedPushes = 10;
                packagePrice = 99000;
                packageName = "Gói Chính chủ 10 lượt (Vĩnh viễn)";
                isRetail = true;
                break;

            // Các gói hội viên theo tháng (Dành cho môi giới có nhiều căn)
            case "basic":
                addedPushes = 15;
                packagePrice = 467000;
                packageName = "Hội viên Cơ bản";
                break;
            case "standard":
                addedPushes = 30;
                packagePrice = 1267000;
                packageName = "Hội viên Tiêu chuẩn";
                break;
            case "premium":
                addedPushes = 50;
                packagePrice = 2200000;
                packageName = "Hội viên Cao cấp";
                break;
            default:
                return BadRequest(new { message = "Gói dịch vụ không hợp lệ" });
        }

        // Cập nhật lượt đẩy tin và gói dịch vụ người dùng
        if (!isRetail || user.CurrentPackage == "Thành viên thường" || string.IsNullOrEmpty(user.CurrentPackage))
        {
            user.CurrentPackage = packageName;
        }
        user.RemainingPushes += addedPushes;
        await _context.SaveChangesAsync();

        // Ghi nhận giao dịch vào hệ thống doanh thu Admin
        var paymentMethod = request.PaymentMethod ?? "VNPAY QR";
        var txnCode = $"PAY-{DateTime.UtcNow:MMddHHmm}{new Random().Next(10, 99)}";
        lock (_transactions)
        {
            _transactions.Insert(0, new PackageTransaction
            {
                TransactionCode = txnCode,
                UserName = user.FullName,
                UserEmail = user.Email,
                PackageId = request.PackageId?.ToLower() ?? "",
                PackageName = packageName,
                Amount = packagePrice,
                PaymentMethod = paymentMethod,
                CreatedAt = DateTime.UtcNow,
                IsRetail = isRetail,
                Status = "Thành công"
            });
        }

        var successMsg = isRetail
            ? $"Thanh toán thành công qua {paymentMethod}! Bạn đã được cộng thêm {addedPushes} lượt đẩy tin ({packageName}). Các lượt đẩy tin này không giới hạn thời gian sử dụng."
            : $"Thanh toán thành công qua {paymentMethod}! Bạn đã được nâng cấp lên gói '{packageName}' và cộng thêm {addedPushes} lượt đẩy tin.";

        return Ok(new
        {
            success = true,
            message = successMsg,
            currentPackage = user.CurrentPackage,
            remainingPushes = user.RemainingPushes
        });
    }

    // 4. [ADMIN] Thống kê doanh thu và lịch sử thanh toán gói dịch vụ hội viên & gói đẩy tin lẻ
    [HttpGet("admin-revenue")]
    [Authorize]
    public IActionResult GetAdminRevenue()
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value 
                ?? User.FindFirst("role")?.Value;
        if (role != "Admin")
            return StatusCode(403, new { message = "Chỉ Quản trị viên mới có quyền xem doanh thu hệ thống" });

        long basicRevenue = 0, standardRevenue = 0, premiumRevenue = 0;
        int basicCount = 0, standardCount = 0, premiumCount = 0;

        long pushSingleRevenue = 0, pushQuick3Revenue = 0, pushWeekendRevenue = 0, pushOwner10Revenue = 0;
        int pushSingleCount = 0, pushQuick3Count = 0, pushWeekendCount = 0, pushOwner10Count = 0;

        List<PackageTransaction> txnsCopy;
        lock (_transactions)
        {
            txnsCopy = _transactions.ToList();
        }

        foreach (var t in txnsCopy)
        {
            switch (t.PackageId.ToLower())
            {
                case "basic":
                    basicRevenue += t.Amount;
                    basicCount++;
                    break;
                case "standard":
                    standardRevenue += t.Amount;
                    standardCount++;
                    break;
                case "premium":
                    premiumRevenue += t.Amount;
                    premiumCount++;
                    break;
                case "push_single":
                    pushSingleRevenue += t.Amount;
                    pushSingleCount++;
                    break;
                case "push_quick_3":
                    pushQuick3Revenue += t.Amount;
                    pushQuick3Count++;
                    break;
                case "push_weekend":
                    pushWeekendRevenue += t.Amount;
                    pushWeekendCount++;
                    break;
                case "push_owner_10":
                    pushOwner10Revenue += t.Amount;
                    pushOwner10Count++;
                    break;
            }
        }

        long monthlyRevenue = basicRevenue + standardRevenue + premiumRevenue;
        int monthlyCount = basicCount + standardCount + premiumCount;

        long retailRevenue = pushSingleRevenue + pushQuick3Revenue + pushWeekendRevenue + pushOwner10Revenue;
        int retailCount = pushSingleCount + pushQuick3Count + pushWeekendCount + pushOwner10Count;

        long totalRevenue = monthlyRevenue + retailRevenue;

        return Ok(new
        {
            totalRevenue,
            totalTransactions = txnsCopy.Count,
            monthlyRevenue,
            monthlyCount,
            retailRevenue,
            retailCount,
            packages = new
            {
                basic = new { name = "Hội viên Cơ bản", count = basicCount, revenue = basicRevenue, price = 467000 },
                standard = new { name = "Hội viên Tiêu chuẩn", count = standardCount, revenue = standardRevenue, price = 1267000 },
                premium = new { name = "Hội viên Cao cấp", count = premiumCount, revenue = premiumRevenue, price = 2200000 },
                pushSingle = new { name = "Đẩy lẻ 1 lượt", count = pushSingleCount, revenue = pushSingleRevenue, price = 15000 },
                pushQuick3 = new { name = "Bán nhanh 3 lượt", count = pushQuick3Count, revenue = pushQuick3Revenue, price = 39000 },
                pushWeekend = new { name = "Cuối tuần vàng", count = pushWeekendCount, revenue = pushWeekendRevenue, price = 49000 },
                pushOwner10 = new { name = "Chính chủ vĩnh viễn", count = pushOwner10Count, revenue = pushOwner10Revenue, price = 99000 },
            },
            transactions = txnsCopy
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

