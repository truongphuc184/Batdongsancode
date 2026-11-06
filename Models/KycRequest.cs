using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Batdongsancode.Models;

public class KycRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string IdCardNumber { get; set; } = string.Empty;

    [Required]
    public string IdCardFront { get; set; } = string.Empty;

    [Required]
    public string IdCardBack { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? TaxCode { get; set; }

    [MaxLength(50)]
    public string? TaxType { get; set; } // "Cá nhân", "Doanh nghiệp"

    [Required]
    [MaxLength(100)]
    public string RequestedRole { get; set; } = "Seller"; // "Seller", "Landlord", "Agent"

    [MaxLength(100)]
    public string? BrokerLicenseNumber { get; set; }

    public string? BrokerLicenseImage { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "PENDING"; // "PENDING", "APPROVED", "REJECTED"

    [MaxLength(500)]
    public string? RejectReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    public int? ReviewedBy { get; set; }
}

