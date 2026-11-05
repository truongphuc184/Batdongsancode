using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Batdongsancode.Models;

public class Lead
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ListingId { get; set; }

    [Required]
    public int OwnerId { get; set; } // Người sở hữu tin đăng nhận khách này

    [Required]
    [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string CustomerPhone { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? CustomerEmail { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public LeadStatus Status { get; set; } = LeadStatus.NEW;

    [MaxLength(500)]
    public string? ResponseMessage { get; set; } // Phản hồi từ người bán / môi giới gửi người mua/thuê

    public DateTime? RespondedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("ListingId")]
    public Listing? Listing { get; set; }

    [ForeignKey("OwnerId")]
    public User? Owner { get; set; }
}

