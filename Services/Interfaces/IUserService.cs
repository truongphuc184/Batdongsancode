using Batdongsancode.DTOs.Auth;
using Batdongsancode.Models;

namespace Batdongsancode.Services.Interfaces;

public interface IUserService
{
    Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string Message, AuthResponse? Response)> AuthenticateAsync(LoginRequest request);
    Task<User?> GetByIdAsync(int id);
}

