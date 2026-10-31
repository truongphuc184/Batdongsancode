using System.ComponentModel.DataAnnotations;
using Batdongsancode.Models;

namespace Batdongsancode.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = null!;

    [Required]
    public string PasswordHash { get; set; } = null!;

    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public Role Role { get; set; }

    public bool IsActive { get; set; } = true;

    public int RemainingPushes { get; set; } = 5; // Số lượt đẩy tin còn lại

    [MaxLength(100)]
    public string CurrentPackage { get; set; } = "Thành viên thường";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
