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
public class LeadsController : ControllerBase
{
    private readonly AppDbContext _context;

    public LeadsController(AppDbContext context)
    {
        _context = context;
    }

    public record CreateLeadDto(int ListingId, string CustomerName, string CustomerPhone, string? CustomerEmail, string? Notes);
    public record UpdateLeadStatusDto(LeadStatus Status);

    // 1. Khách hàng gửi yêu cầu tư vấn / Đặt lịch xem BĐS (Use Case: Liên hệ người đăng -> Tiếp nhận thông tin khách quan tâm)
    [HttpPost]
    public async Task<IActionResult> CreateLead([FromBody] CreateLeadDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CustomerName) || string.IsNullOrWhiteSpace(dto.CustomerPhone))
            return BadRequest(new { message = "Vui lòng nhập họ tên và số điện thoại liên hệ" });

        var listing = await _context.Listings.FindAsync(dto.ListingId);
        if (listing == null)
            return NotFound(new { message = "Không tìm thấy bất động sản" });

        var lead = new Lead
        {
            ListingId = dto.ListingId,
            OwnerId = listing.OwnerId,
            CustomerName = dto.CustomerName.Trim(),
            CustomerPhone = dto.CustomerPhone.Trim(),
            CustomerEmail = dto.CustomerEmail?.Trim(),
            Notes = dto.Notes?.Trim(),
            Status = LeadStatus.NEW,
            CreatedAt = DateTime.UtcNow
        };

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Gửi yêu cầu liên hệ thành công! Người đăng sẽ liên hệ lại bạn sớm nhất.", leadId = lead.Id });
    }

    // 2. Môi giới / Người bán xem danh sách khách hàng quan tâm (Use Case: Quản lý khách hàng CRM)
    [Authorize]
    [HttpGet("my-leads")]
    public async Task<IActionResult> GetMyLeads()
    {
        var userIdStr = User.FindFirst("sub")?.Value 
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Không xác định được danh tính người dùng" });

        var leads = await _context.Leads
            .Include(ld => ld.Listing)
            .Where(ld => ld.OwnerId == userId)
            .OrderByDescending(ld => ld.CreatedAt)
            .Select(ld => new
            {
                ld.Id,
                ld.ListingId,
                ListingTitle = ld.Listing != null ? ld.Listing.Title : "BĐS #" + ld.ListingId,
                ListingPrice = ld.Listing != null ? ld.Listing.Price : 0,
                ld.CustomerName,
                ld.CustomerPhone,
                ld.CustomerEmail,
                ld.Notes,
                Status = ld.Status.ToString(),
                ld.CreatedAt
            })
            .ToListAsync();

        return Ok(leads);
    }

    // 3. Cập nhật trạng thái khách hàng (Phân loại: NEW, CONSULTING, VIEWING, NEGOTIATING, DEALT, CANCELED)
    [Authorize]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateLeadStatusDto dto)
    {
        var userIdStr = User.FindFirst("sub")?.Value 
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Không xác định được danh tính" });

        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound(new { message = "Không tìm thấy khách hàng" });

        if (lead.OwnerId != userId)
            return Forbid();

        lead.Status = dto.Status;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã cập nhật trạng thái khách hàng thành công!", newStatus = lead.Status.ToString() });
    }
}

