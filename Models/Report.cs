using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Batdongsancode.Models;

public class Report
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ListingId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Details { get; set; }

    [MaxLength(100)]
    public string? ReporterName { get; set; }

    [MaxLength(50)]
    public string? ReporterPhone { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Resolved, Dismissed

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("ListingId")]
    public Listing? Listing { get; set; }
}

