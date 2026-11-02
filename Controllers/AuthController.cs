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
    public async Task<IActionResult> Me()
    {
        var subClaim = User.FindFirst("sub")?.Value 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(subClaim) || !int.TryParse(subClaim, out var userId))
            return Unauthorized();

        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
            return NotFound();
        return Ok(new { user.Id, user.Email, user.FullName, Role = user.Role.ToString() });
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

        Role targetRole = Role.None;
        List<string> selectedRoles = new List<string>();

        if (request.TargetRoles != null && request.TargetRoles.Count > 0)
        {
            selectedRoles = request.TargetRoles.Select(r => r.Trim()).Distinct().ToList();
            foreach (var r in selectedRoles)
            {
                if (Enum.TryParse<Role>(r, true, out var parsedRole))
                {
                    targetRole |= parsedRole;
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

        user.Role = targetRole;
        bool canPost = targetRole.HasFlag(Role.Seller) || targetRole.HasFlag(Role.Landlord) || targetRole.HasFlag(Role.Agent) || targetRole.HasFlag(Role.Admin);
        if (canPost && user.RemainingPushes <= 0)
        {
            user.RemainingPushes = 5; // Cấp mặc định 5 lượt đẩy tin khi có vai trò đăng bán
        }
        await context.SaveChangesAsync();

        // Tạo JWT Token mới mang vai trò đã được nâng cấp
        var newToken = Batdongsancode.Helpers.JwtHelper.GenerateToken(user, config);

        var labelParts = new List<string>();
        var roleSetLower = new HashSet<string>(selectedRoles, StringComparer.OrdinalIgnoreCase);
        if (roleSetLower.Contains("Buyer")) labelParts.Add("Người mua");
        if (roleSetLower.Contains("Seller")) labelParts.Add("Người bán");
        if (roleSetLower.Contains("Tenant")) labelParts.Add("Người thuê");
        if (roleSetLower.Contains("Landlord")) labelParts.Add("Cho thuê");
        if (roleSetLower.Contains("Agent")) labelParts.Add("Môi giới");

        string friendlyRoleName = labelParts.Count > 0 
            ? string.Join(" & ", labelParts) 
            : targetRole.ToString();

        var friendlyMessage = $"Đã cập nhật vai trò thành '{friendlyRoleName}' thành công!";

        return Ok(new
        {
            message = friendlyMessage,
            user = new { user.Id, user.Email, user.FullName, Role = user.Role.ToString() },
            roles = selectedRoles,
            roleLabel = friendlyRoleName,
            accessToken = newToken,
            token = newToken
        });
    }
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? NewPassword { get; set; }
}

