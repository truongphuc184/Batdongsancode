namespace Batdongsancode.DTOs.Auth
{
    public class RegisterRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Buyer"; // default role đơn lẻ
        public List<string>? Roles { get; set; } // Hỗ trợ chọn cùng lúc nhiều vai trò (Mua + Bán + Thuê...)
    }

    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class UpgradeRoleRequest
    {
        public string? TargetRole { get; set; } = "Seller";
        public List<string>? TargetRoles { get; set; } // Hỗ trợ chọn cùng lúc nhiều vai trò
    }

    public class AuthResponse
    {
        public string AccessToken { get; set; } = null!;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
    }
}


