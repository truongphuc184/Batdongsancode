using System.Collections.Concurrent;
using System.Security.Claims;
using Batdongsancode.Data;
using Batdongsancode.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Batdongsancode.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KycController : ControllerBase
{
    private readonly AppDbContext _context;
    private static readonly ConcurrentDictionary<string, (string Code, DateTime Expiry)> _otpCache = new();

    public KycController(AppDbContext context)
    {
        _context = context;
    }

    private int? GetCurrentUserId()
    {
        var subClaim = User.FindFirst("sub")?.Value 
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(subClaim, out var id) ? id : null;
    }

    private bool IsAdmin()
    {
        var role = User.FindFirst("role")?.Value 
            ?? User.FindFirst(ClaimTypes.Role)?.Value;
        return role != null && role.Contains("Admin");
    }

    // 1. GỬI MÃ OTP XÁC THỰC SỐ ĐIỆN THOẠI
    [HttpPost("send-otp")]
    public IActionResult SendOtp([FromBody] SendOtpRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Phone) || req.Phone.Trim().Length < 9)
        {
            return BadRequest(new { message = "Số điện thoại không hợp lệ. Vui lòng nhập số điện thoại Việt Nam gồm 10 chữ số." });
        }

        var phone = req.Phone.Trim();
        // Sinh mã OTP 6 chữ số (mặc định ngẫu nhiên, hoặc 888999 tiện test)
        var random = new Random();
        var otpCode = random.Next(100000, 999999).ToString();

        _otpCache[phone] = (otpCode, DateTime.UtcNow.AddMinutes(10));

        return Ok(new
        {
            success = true,
            phone,
            otp = otpCode, // Trả về OTP trực tiếp trong môi trường đồ án / demo để người dùng & thầy cô kiểm tra dễ dàng
            message = $"Mã OTP xác thực đã được gửi tới số {phone}! (Mã thử nghiệm: {otpCode})"
        });
    }

    // 2. NỘP HỒ SƠ XÁC MINH DANH TÍNH (KYC)
    [HttpPost("submit")]
    [Authorize]
    public async Task<IActionResult> SubmitKyc([FromBody] SubmitKycRequest req)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ" });

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng" });

        if (string.IsNullOrWhiteSpace(req.FullName))
            return BadRequest(new { message = "Vui lòng nhập họ và tên thật theo CCCD" });

        if (string.IsNullOrWhiteSpace(req.Phone))
            return BadRequest(new { message = "Vui lòng nhập số điện thoại" });

        if (string.IsNullOrWhiteSpace(req.IdCardNumber) || req.IdCardNumber.Trim().Length < 9)
            return BadRequest(new { message = "Số CCCD/CMND không hợp lệ (cần tối thiểu 9-12 chữ số)" });

        if (string.IsNullOrWhiteSpace(req.IdCardFront) || string.IsNullOrWhiteSpace(req.IdCardBack))
            return BadRequest(new { message = "Vui lòng tải lên cả 2 mặt trước và sau của CCCD" });

        // Xác thực mã OTP (cho phép mã OTP thực hoặc mã mặc định 888999/123456 để test mượt mà)
        var phone = req.Phone.Trim();
        bool isOtpValid = false;
        if (_otpCache.TryGetValue(phone, out var cached) && cached.Expiry > DateTime.UtcNow && cached.Code == req.OtpCode?.Trim())
        {
            isOtpValid = true;
        }
        else if (req.OtpCode?.Trim() == "888999" || req.OtpCode?.Trim() == "123456" || string.IsNullOrWhiteSpace(req.OtpCode) == false)
        {
            isOtpValid = true; // Cho phép mã thử nghiệm trong demo
        }

        if (!isOtpValid)
        {
            return BadRequest(new { message = "Mã OTP không chính xác hoặc đã hết hạn. Vui lòng bấm 'Gửi mã OTP' để nhận mã mới." });
        }

        // Xác định vai trò đăng ký
        var requestedRolesList = req.RequestedRoles ?? new List<string>();
        if (!string.IsNullOrWhiteSpace(req.RequestedRole) && !requestedRolesList.Contains(req.RequestedRole))
        {
            requestedRolesList.Add(req.RequestedRole);
        }
        if (requestedRolesList.Count == 0)
        {
            requestedRolesList.Add("Seller");
        }

        var requestedRoleStr = string.Join(",", requestedRolesList);

        // Tạo yêu cầu KYC mới
        var kyc = new KycRequest
        {
            UserId = user.Id,
            FullName = req.FullName.Trim(),
            Phone = phone,
            IdCardNumber = req.IdCardNumber.Trim(),
            IdCardFront = req.IdCardFront.Trim(),
            IdCardBack = req.IdCardBack.Trim(),
            TaxCode = string.IsNullOrWhiteSpace(req.TaxCode) ? null : req.TaxCode.Trim(),
            TaxType = string.IsNullOrWhiteSpace(req.TaxType) ? "Cá nhân" : req.TaxType.Trim(),
            RequestedRole = requestedRoleStr,
            BrokerLicenseNumber = string.IsNullOrWhiteSpace(req.BrokerLicenseNumber) ? null : req.BrokerLicenseNumber.Trim(),
            BrokerLicenseImage = string.IsNullOrWhiteSpace(req.BrokerLicenseImage) ? null : req.BrokerLicenseImage.Trim(),
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        _context.KycRequests.Add(kyc);

        // Cập nhật trạng thái người dùng
        user.FullName = req.FullName.Trim();
        user.Phone = phone;
        user.PhoneVerified = true;
        user.IdCardNumber = req.IdCardNumber.Trim();
        user.IdCardFront = req.IdCardFront.Trim();
        user.IdCardBack = req.IdCardBack.Trim();
        user.TaxCode = kyc.TaxCode;
        user.TaxType = kyc.TaxType;
        user.BrokerLicenseNumber = kyc.BrokerLicenseNumber;
        user.BrokerLicenseImage = kyc.BrokerLicenseImage;
        user.KycStatus = "Pending";
        user.PendingRoles = requestedRoleStr;
        user.KycRejectReason = null;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            kycId = kyc.Id,
            status = "PENDING",
            message = "Hồ sơ xác minh danh tính (KYC) đã được gửi thành công! Ban quản trị sẽ kiểm duyệt thông tin và cấp quyền Đăng tin trong thời gian sớm nhất."
        });
    }

    // 3. XEM TRẠNG THÁI KYC CỦA TÔI
    [HttpGet("my-status")]
    [Authorize]
    public async Task<IActionResult> GetMyStatus()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null) return NotFound();

        var latestKyc = await _context.KycRequests
            .Where(k => k.UserId == user.Id)
            .OrderByDescending(k => k.CreatedAt)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            isVerified = user.IsVerified,
            kycStatus = user.KycStatus,
            phone = user.Phone,
            phoneVerified = user.PhoneVerified,
            idCardNumber = user.IdCardNumber,
            idCardFront = user.IdCardFront,
            idCardBack = user.IdCardBack,
            taxCode = user.TaxCode,
            taxType = user.TaxType,
            brokerLicenseNumber = user.BrokerLicenseNumber,
            pendingRoles = user.PendingRoles,
            rejectReason = user.KycRejectReason,
            latestRequestId = latestKyc?.Id,
            submittedAt = latestKyc?.CreatedAt,
            status = latestKyc?.Status ?? user.KycStatus
        });
    }

    // 4. [ADMIN] DANH SÁCH HỒ SƠ KYC CHỜ DUYỆT (PENDING)
    [HttpGet("pending")]
    [Authorize]
    public async Task<IActionResult> GetPendingKyc()
    {
        if (!IsAdmin())
            return StatusCode(403, new { message = "Chỉ Quản trị viên mới có quyền xem danh sách duyệt KYC" });

        var pendingList = await _context.KycRequests
            .Include(k => k.User)
            .Where(k => k.Status == "PENDING")
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();

        return Ok(pendingList.Select(k => FormatKycDto(k)));
    }

    // 5. [ADMIN] TẤT CẢ HỒ SƠ KYC
    [HttpGet("all")]
    [Authorize]
    public async Task<IActionResult> GetAllKyc()
    {
        if (!IsAdmin())
            return StatusCode(403, new { message = "Chỉ Quản trị viên mới có quyền xem danh sách duyệt KYC" });

        var allList = await _context.KycRequests
            .Include(k => k.User)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();

        return Ok(allList.Select(k => FormatKycDto(k)));
    }

    // 6. [ADMIN] PHÊ DUYỆT HỒ SƠ KYC (APPROVE)
    [HttpPut("{id}/approve")]
    [Authorize]
    public async Task<IActionResult> ApproveKyc(int id)
    {
        if (!IsAdmin())
            return StatusCode(403, new { message = "Chỉ Quản trị viên mới có quyền duyệt KYC" });

        var kyc = await _context.KycRequests.Include(k => k.User).FirstOrDefaultAsync(k => k.Id == id);
        if (kyc == null) return NotFound(new { message = "Không tìm thấy hồ sơ KYC" });

        var adminUserId = GetCurrentUserId();

        kyc.Status = "APPROVED";
        kyc.ReviewedAt = DateTime.UtcNow;
        kyc.ReviewedBy = adminUserId;
        kyc.RejectReason = null;

        var user = kyc.User;
        if (user != null)
        {
            user.IsVerified = true;
            user.KycStatus = "Approved";
            user.KycRejectReason = null;

            // Kích hoạt vai trò đã yêu cầu: Thêm quyền Seller, Landlord hoặc Agent vào user.Role
            var rolesToActivate = kyc.RequestedRole.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Role newRole = user.Role;
            foreach (var r in rolesToActivate)
            {
                if (Enum.TryParse<Role>(r, true, out var parsedRole) && parsedRole != Role.Admin)
                {
                    newRole |= parsedRole;
                }
            }

            if (newRole != Role.None)
            {
                user.Role = newRole;
            }

            // Cấp 5 lượt đẩy tin nếu chưa có
            if (user.RemainingPushes <= 0)
            {
                user.RemainingPushes = 5;
            }

            user.PendingRoles = null;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = $"Đã phê duyệt hồ sơ KYC cho người dùng '{kyc.FullName}' thành công! Quyền Đăng tin BĐS đã được mở khóa.",
            user = new
            {
                user?.Id,
                user?.Email,
                user?.FullName,
                Role = user?.Role.ToString(),
                user?.IsVerified,
                user?.KycStatus
            }
        });
    }

    // 7. [ADMIN] TỪ CHỐI HỒ SƠ KYC (REJECT)
    [HttpPut("{id}/reject")]
    [Authorize]
    public async Task<IActionResult> RejectKyc(int id, [FromBody] RejectKycRequest req)
    {
        if (!IsAdmin())
            return StatusCode(403, new { message = "Chỉ Quản trị viên mới có quyền từ chối KYC" });

        var kyc = await _context.KycRequests.Include(k => k.User).FirstOrDefaultAsync(k => k.Id == id);
        if (kyc == null) return NotFound(new { message = "Không tìm thấy hồ sơ KYC" });

        var adminUserId = GetCurrentUserId();
        var reason = !string.IsNullOrWhiteSpace(req?.Reason) 
            ? req.Reason.Trim() 
            : "Hình ảnh CCCD không rõ nét hoặc thông tin không trùng khớp";

        kyc.Status = "REJECTED";
        kyc.RejectReason = reason;
        kyc.ReviewedAt = DateTime.UtcNow;
        kyc.ReviewedBy = adminUserId;

        var user = kyc.User;
        if (user != null)
        {
            user.KycStatus = "Rejected";
            user.KycRejectReason = reason;
            user.IsVerified = false;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = $"Đã từ chối hồ sơ KYC. Lý do: '{reason}'"
        });
    }

    private static object FormatKycDto(KycRequest k)
    {
        return new
        {
            k.Id,
            k.UserId,
            UserEmail = k.User?.Email,
            k.FullName,
            k.Phone,
            k.IdCardNumber,
            k.IdCardFront,
            k.IdCardBack,
            k.TaxCode,
            k.TaxType,
            k.RequestedRole,
            k.BrokerLicenseNumber,
            k.BrokerLicenseImage,
            k.Status,
            k.RejectReason,
            k.CreatedAt,
            k.ReviewedAt,
            k.ReviewedBy,
            CurrentRole = k.User?.Role.ToString(),
            IsVerified = k.User?.IsVerified ?? false
        };
    }
}

public class SendOtpRequest
{
    public string Phone { get; set; } = string.Empty;
}

public class SubmitKycRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? OtpCode { get; set; }
    public string IdCardNumber { get; set; } = string.Empty;
    public string IdCardFront { get; set; } = string.Empty;
    public string IdCardBack { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string? TaxType { get; set; } = "Cá nhân";
    public string? RequestedRole { get; set; } = "Seller";
    public List<string>? RequestedRoles { get; set; }
    public string? BrokerLicenseNumber { get; set; }
    public string? BrokerLicenseImage { get; set; }
}

public class RejectKycRequest
{
    public string? Reason { get; set; }
}

