using Batdongsancode.Data;
using Batdongsancode.DTOs.Auth;
using Batdongsancode.Helpers;
using Batdongsancode.Models;
using Batdongsancode.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Batdongsancode.Services.Implementations;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public UserService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request)
    {
        // Check if email already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
            return (false, "Email đã được sử dụng.");

        // Xác định vai trò: Hỗ trợ chọn cùng lúc nhiều vai trò (ví dụ vừa Mua vừa Bán)
        Role role = Role.None;
        if (request.Roles != null && request.Roles.Count > 0)
        {
            foreach (var r in request.Roles)
            {
                if (Enum.TryParse<Role>(r.Trim(), true, out var parsedRole))
                {
                    role |= parsedRole;
                }
            }
        }
        
        if (role == Role.None)
        {
            if (!Enum.TryParse<Role>(request.Role, true, out role))
                return (false, "Vai trò không hợp lệ.");
        }

        bool canPost = role.HasFlag(Role.Seller) || role.HasFlag(Role.Landlord) || role.HasFlag(Role.Agent) || role.HasFlag(Role.Admin);

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = role,
            IsActive = true,
            RemainingPushes = canPost ? 5 : 0,
            CurrentPackage = "Thành viên thường",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return (true, "Đăng ký thành công.");
    }

    public async Task<(bool Success, string Message, AuthResponse? Response)> AuthenticateAsync(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
            return (false, "Email hoặc mật khẩu không đúng.", null);

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return (false, "Email hoặc mật khẩu không đúng.", null);

        if (!user.IsActive)
            return (false, "Tài khoản đã bị vô hiệu hóa.", null);

        var token = JwtHelper.GenerateToken(user, _configuration);

        var response = new AuthResponse
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = int.Parse(_configuration.GetSection("JwtSettings")["ExpirationMinutes"] ?? "60") * 60
        };

        return (true, "Đăng nhập thành công.", response);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }
}

