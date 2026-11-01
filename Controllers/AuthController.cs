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
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? NewPassword { get; set; }
}

