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

    // === HỆ THỐNG XÁC THỰC DANH TÍNH (KYC) & ĐĂNG TIN ===
    public bool IsVerified { get; set; } = false; // Đã duyệt xác minh CCCD chưa

    [MaxLength(50)]
    public string KycStatus { get; set; } = "None"; // "None", "Pending", "Approved", "Rejected"

    [MaxLength(50)]
    public string? Phone { get; set; }

    public bool PhoneVerified { get; set; } = false; // Đã xác thực OTP qua SĐT chưa

    [MaxLength(50)]
    public string? IdCardNumber { get; set; } // Số CCCD

    public string? IdCardFront { get; set; } // Ảnh mặt trước CCCD

    public string? IdCardBack { get; set; } // Ảnh mặt sau CCCD

    [MaxLength(50)]
    public string? TaxCode { get; set; } // Mã số thuế cá nhân hoặc doanh nghiệp

    [MaxLength(50)]
    public string? TaxType { get; set; } // "Cá nhân", "Doanh nghiệp"

    [MaxLength(100)]
    public string? BrokerLicenseNumber { get; set; } // Số chứng chỉ hành nghề môi giới BĐS

    public string? BrokerLicenseImage { get; set; } // Ảnh chụp chứng chỉ hành nghề môi giới

    [MaxLength(100)]
    public string? PendingRoles { get; set; } // Vai trò đang chờ duyệt KYC

    [MaxLength(500)]
    public string? KycRejectReason { get; set; } // Lý do từ chối KYC (nếu có)
}
