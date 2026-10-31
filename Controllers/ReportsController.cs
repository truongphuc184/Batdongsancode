using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Batdongsancode.Data;
using Batdongsancode.Models;

namespace Batdongsancode.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    public record CreateReportDto(int ListingId, string Reason, string? Details, string? ReporterName, string? ReporterPhone);
    public record ResolveReportDto(string Action); // "HideListing" hoặc "Dismiss"

    // 1. Gửi báo cáo tin đăng vi phạm (Use Case: Báo cáo tin đăng vi phạm)
    [HttpPost]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest(new { message = "Vui lòng chọn lý do báo cáo vi phạm" });

        var listing = await _context.Listings.FindAsync(dto.ListingId);
        if (listing == null)
            return NotFound(new { message = "Không tìm thấy bất động sản cần báo cáo" });

        var report = new Report
        {
            ListingId = dto.ListingId,
            Reason = dto.Reason.Trim(),
            Details = dto.Details?.Trim(),
            ReporterName = dto.ReporterName?.Trim(),
            ReporterPhone = dto.ReporterPhone?.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã gửi báo cáo vi phạm tới Ban quản trị. Cảm ơn sự đóng góp của bạn!" });
    }

    // 2. Admin xem danh sách báo cáo vi phạm (Use Case Quản trị: Xử lý báo cáo vi phạm)
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAllReports()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value 
                ?? User.FindFirst("role")?.Value;
        if (role != "Admin")
            return StatusCode(403, new { message = "Chỉ Admin mới có quyền xem báo cáo vi phạm" });

        var reports = await _context.Reports
            .Include(r => r.Listing)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.ListingId,
                ListingTitle = r.Listing != null ? r.Listing.Title : "BĐS #" + r.ListingId,
                r.Reason,
                r.Details,
                r.ReporterName,
                r.ReporterPhone,
                r.Status,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(reports);
    }

    // 3. Admin xử lý báo cáo (Gỡ tin hoặc Bác bỏ báo cáo)
    [Authorize]
    [HttpPut("{id}/resolve")]
    public async Task<IActionResult> ResolveReport(int id, [FromBody] ResolveReportDto dto)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value 
                ?? User.FindFirst("role")?.Value;
        if (role != "Admin")
            return StatusCode(403, new { message = "Chỉ Admin mới có quyền xử lý báo cáo" });

        var report = await _context.Reports.Include(r => r.Listing).FirstOrDefaultAsync(r => r.Id == id);
        if (report == null)
            return NotFound(new { message = "Không tìm thấy báo cáo vi phạm" });

        if (dto.Action == "HideListing")
        {
            report.Status = "Resolved";
            if (report.Listing != null)
            {
                report.Listing.Status = ListingStatus.Hidden; // Ẩn tin vi phạm khỏi trang chủ
                report.Listing.RejectReason = "Tin bị gỡ do vi phạm: " + report.Reason;
            }
        }
        else
        {
            report.Status = "Dismissed"; // Bác bỏ / Báo cáo không đúng
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xử lý báo cáo vi phạm thành công!", status = report.Status });
    }
}

