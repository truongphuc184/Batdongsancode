using Microsoft.AspNetCore.Mvc;
using Batdongsancode.DTOs.Auth;
using Batdongsancode.Models;
using Batdongsancode.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Batdongsancode.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _userService.RegisterAsync(request);
        if (!result.Success)
            return BadRequest(new { result.Message });
        return Ok(new { result.Message });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var authResult = await _userService.AuthenticateAsync(request);
        if (!authResult.Success)
            return Unauthorized(new { authResult.Message });
        return Ok(authResult.Response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me([FromServices] Batdongsancode.Data.AppDbContext context)
    {
        var subClaim = User.FindFirst("sub")?.Value 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(subClaim) || !int.TryParse(subClaim, out var userId))
            return Unauthorized();

        var user = await context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();
        return Ok(new 
        { 
            user.Id, 
            user.Email, 
            user.FullName, 
            Role = user.Role.ToString(),
            user.IsVerified,
            user.KycStatus,
            user.Phone,
            user.PhoneVerified,
            user.TaxCode,
            user.TaxType,
            user.PendingRoles,
            user.KycRejectReason,
            user.RemainingPushes,
            user.CurrentPackage
        });
    }

    // Quản lý người dùng dành riêng cho Admin
    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> GetAllUsers([FromServices] Batdongsancode.Data.AppDbContext context)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value 
                ?? User.FindFirst("role")?.Value;
        if (role != "Admin")
            return StatusCode(403, new { message = "Chỉ Quản trị viên mới có quyền xem danh sách người dùng" });

        var users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            context.Users.OrderByDescending(u => u.CreatedAt)
        );

        return Ok(users.Select(u => new
        {
            u.Id,
            u.Email,
            u.FullName,
            Role = u.Role.ToString(),
            u.CurrentPackage,
            u.RemainingPushes,
            u.IsActive,
            u.IsVerified,
            u.KycStatus,
            u.Phone,
            u.TaxCode,
            u.CreatedAt
        }));
    }

    // Chỉnh sửa thông tin cá nhân (Họ tên, Đổi mật khẩu)
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, [FromServices] Batdongsancode.Data.AppDbContext context)
    {
        var subClaim = User.FindFirst("sub")?.Value 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(subClaim) || !int.TryParse(subClaim, out var userId))
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ" });

        var user = await context.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "Không tìm thấy tài khoản người dùng" });

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName.Trim();

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            if (request.NewPassword.Trim().Length < 6)
                return BadRequest(new { message = "Mật khẩu mới phải có tối thiểu 6 ký tự" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword.Trim());
        }

        await context.SaveChangesAsync();
        return Ok(new 
        { 
            message = "Cập nhật thông tin cá nhân thành công!", 
            user = new { user.Id, user.Email, user.FullName, Role = user.Role.ToString() } 
        });
    }

    // Nâng cấp / Kích hoạt vai trò (Ví dụ Người mua muốn mở quyền Đăng bán mà không cần tạo email mới)
    [HttpPost("upgrade-role")]
    [Authorize]
    public async Task<IActionResult> UpgradeRole([FromBody] UpgradeRoleRequest request, [FromServices] Batdongsancode.Data.AppDbContext context, [FromServices] IConfiguration config)
    {
        var subClaim = User.FindFirst("sub")?.Value 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(subClaim) || !int.TryParse(subClaim, out var userId))
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ" });

        var user = await context.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "Không tìm thấy tài khoản người dùng" });

        if (user.Role == Role.Admin)
        {
            return BadRequest(new { message = "Tài khoản Quản trị viên hệ thống có vai trò cố định, không thể chuyển đổi sang vai trò người dùng thường." });
        }

        Role targetRole = Role.None;
        List<string> selectedRoles = new List<string>();

        if (request.TargetRoles != null && request.TargetRoles.Count > 0)
        {
            selectedRoles = request.TargetRoles.Select(r => r.Trim()).Distinct().ToList();
            foreach (var r in selectedRoles)
            {
                if (Enum.TryParse<Role>(r, true, out var parsedRole))
                {
                    if (parsedRole != Role.Admin)
                    {
                        targetRole |= parsedRole;
                    }
                }
            }
        }
        else
        {
            var targetRoleStr = string.IsNullOrWhiteSpace(request.TargetRole) ? "Seller" : request.TargetRole.Trim();
            if (!Enum.TryParse<Role>(targetRoleStr, true, out targetRole) || targetRole == Role.Admin)
            {
                return BadRequest(new { message = "Vai trò nâng cấp không hợp lệ." });
            }
            selectedRoles.Add(targetRole.ToString());
        }

        if (targetRole == Role.None)
            targetRole = Role.Buyer;

        bool wantsPostingRole = selectedRoles.Any(r => 
            r.Equals("Seller", StringComparison.OrdinalIgnoreCase) || 
            r.Equals("Landlord", StringComparison.OrdinalIgnoreCase) || 
            r.Equals("Agent", StringComparison.OrdinalIgnoreCase));

        // 1. Phân quyền: Chuyển tự do giữa Người mua (Buyer) và Người thuê (Tenant) - Trải nghiệm 1-click
        if (!wantsPostingRole)
        {
            user.Role = targetRole;
            await context.SaveChangesAsync();

            var newToken = Batdongsancode.Helpers.JwtHelper.GenerateToken(user, config);
            var labelParts = new List<string>();
            var roleSetLower = new HashSet<string>(selectedRoles, StringComparer.OrdinalIgnoreCase);
            if (roleSetLower.Contains("Buyer")) labelParts.Add("Người mua");
            if (roleSetLower.Contains("Tenant")) labelParts.Add("Người thuê");

            string friendlyName = labelParts.Count > 0 ? string.Join(" & ", labelParts) : targetRole.ToString();

            return Ok(new
            {
                success = true,
                requiresKyc = false,
                message = $"Đã cập nhật vai trò thành '{friendlyName}' thành công!",
                user = new { user.Id, user.Email, user.FullName, Role = user.Role.ToString(), user.IsVerified, user.KycStatus },
                roles = selectedRoles,
                roleLabel = friendlyName,
                accessToken = newToken,
                token = newToken
            });
        }

        // 2. Vai trò Người bán, Cho thuê, Môi giới: Bắt buộc xác thực danh tính (KYC) trước khi cấp quyền Đăng tin
        if (!user.IsVerified && user.KycStatus != "Approved")
        {
            var postingRoles = selectedRoles.Where(r => 
                r.Equals("Seller", StringComparison.OrdinalIgnoreCase) || 
                r.Equals("Landlord", StringComparison.OrdinalIgnoreCase) || 
                r.Equals("Agent", StringComparison.OrdinalIgnoreCase)).ToList();

            user.PendingRoles = string.Join(",", postingRoles);
            await context.SaveChangesAsync();

            var postingRoleNames = new List<string>();
            if (postingRoles.Any(r => r.Equals("Seller", StringComparison.OrdinalIgnoreCase))) postingRoleNames.Add("Người bán (Chủ nhà)");
            if (postingRoles.Any(r => r.Equals("Landlord", StringComparison.OrdinalIgnoreCase))) postingRoleNames.Add("Người cho thuê");
            if (postingRoles.Any(r => r.Equals("Agent", StringComparison.OrdinalIgnoreCase))) postingRoleNames.Add("Môi giới chuyên nghiệp");

            string roleNamesStr = string.Join(", ", postingRoleNames);

            return Ok(new
            {
                success = false,
                requiresKyc = true,
                kycStatus = user.KycStatus,
                pendingRoles = postingRoles,
                phone = user.Phone,
                phoneVerified = user.PhoneVerified,
                rejectReason = user.KycRejectReason,
                message = $"Vai trò [{roleNamesStr}] có quyền Đăng tin BĐS nên bắt buộc phải Xác minh danh tính (KYC) CCCD & SĐT trước khi mở khóa tính năng Đăng tin."
            });
        }

        // 3. Nếu tài khoản ĐÃ ĐƯỢC XÁC MINH KYC từ trước: Kích hoạt vai trò đăng tin
        user.Role = targetRole;
        bool canPost = targetRole.HasFlag(Role.Seller) || targetRole.HasFlag(Role.Landlord) || targetRole.HasFlag(Role.Agent) || targetRole.HasFlag(Role.Admin);
        if (canPost && user.RemainingPushes <= 0)
        {
            user.RemainingPushes = 5; // Cấp mặc định 5 lượt đẩy tin khi có vai trò đăng bán
        }
        await context.SaveChangesAsync();

        // Tạo JWT Token mới mang vai trò đã được nâng cấp
        var newGrantedToken = Batdongsancode.Helpers.JwtHelper.GenerateToken(user, config);

        var grantedLabelParts = new List<string>();
        var grantedRoleSetLower = new HashSet<string>(selectedRoles, StringComparer.OrdinalIgnoreCase);
        if (grantedRoleSetLower.Contains("Buyer")) grantedLabelParts.Add("Người mua");
        if (grantedRoleSetLower.Contains("Seller")) grantedLabelParts.Add("Người bán");
        if (grantedRoleSetLower.Contains("Tenant")) grantedLabelParts.Add("Người thuê");
        if (grantedRoleSetLower.Contains("Landlord")) grantedLabelParts.Add("Cho thuê");
        if (grantedRoleSetLower.Contains("Agent")) grantedLabelParts.Add("Môi giới");

        string friendlyRoleName = grantedLabelParts.Count > 0 
            ? string.Join(" & ", grantedLabelParts) 
            : targetRole.ToString();

        var friendlyMessage = $"Đã cập nhật vai trò thành '{friendlyRoleName}' thành công!";

        return Ok(new
        {
            success = true,
            requiresKyc = false,
            message = friendlyMessage,
            user = new { user.Id, user.Email, user.FullName, Role = user.Role.ToString(), user.IsVerified, user.KycStatus },
            roles = selectedRoles,
            roleLabel = friendlyRoleName,
            accessToken = newGrantedToken,
            token = newGrantedToken
        });
    }
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? NewPassword { get; set; }
}

