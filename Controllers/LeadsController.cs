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

    public class CreateLeadDto
    {
        public int ListingId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateLeadStatusDto
    {
        public LeadStatus Status { get; set; }
    }

    // 1. Khách hàng gửi yêu cầu tư vấn / Đặt lịch xem BĐS
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

    // 2. Môi giới / Người bán xem danh sách khách hàng quan tâm (CRM)
    [Authorize]
    [HttpGet("my-leads")]
    public async Task<IActionResult> GetMyLeads()
    {
        var userIdStr = User.FindFirst("sub")?.Value 
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Không xác định được danh tính người dùng" });

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return Unauthorized(new { message = "Không tìm thấy thông tin tài khoản người dùng" });

        bool isAgentOrAdmin = user.Role.HasFlag(Role.Agent) || user.Role.HasFlag(Role.Admin);

        var query = _context.Leads
            .Include(ld => ld.Listing)
            .AsQueryable();

        // Môi giới (Agent) & Admin: Có quyền tiếp nhận và tư vấn toàn bộ khách hàng quan tâm trên sàn
        // Người bán (Seller) & Cho thuê (Landlord): Tiếp nhận khách hàng gửi yêu cầu cho chính tin đăng của mình
        if (!isAgentOrAdmin)
        {
            query = query.Where(ld => ld.OwnerId == userId);
        }

        var leads = await query
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

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound(new { message = "Không tìm thấy khách hàng" });

        bool isAgentOrAdmin = user.Role.HasFlag(Role.Agent) || user.Role.HasFlag(Role.Admin);
        if (lead.OwnerId != userId && !isAgentOrAdmin)
            return Forbid();

        lead.Status = dto.Status;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã cập nhật trạng thái khách hàng thành công!", newStatus = lead.Status.ToString() });
    }
}

