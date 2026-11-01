using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Batdongsancode.Models;

public class Listing
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public PropertyType Type { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(100)]
    public string District { get; set; } = string.Empty;

    public double? Area { get; set; } = 0; // m2

    public int? Bedrooms { get; set; } = 0;

    public int? Bathrooms { get; set; } = 0;

    [MaxLength(50)]
    public string? Direction { get; set; } = string.Empty; // Đông, Tây, Nam, Bắc...

    [MaxLength(1000)]
    public string? ImageUrls { get; set; } = string.Empty; // comma-separated

    [Required]
    public ListingStatus Status { get; set; } = ListingStatus.Pending;

    public int? ViewCount { get; set; } = 0;

    public bool? IsVip { get; set; } = false;

    public bool? IsVerified { get; set; } = true;

    public DateTime? PriorityTime { get; set; }

    [MaxLength(500)]
    public string? RejectReason { get; set; } = string.Empty;

    public int OwnerId { get; set; }

    [ForeignKey(nameof(OwnerId))]
    public User? Owner { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}

