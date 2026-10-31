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
        var userId = int.Parse(User.FindFirst("sub")!.Value);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
            return NotFound();
        return Ok(new { user.Id, user.Email, user.FullName, Role = user.Role.ToString() });
    }
}

