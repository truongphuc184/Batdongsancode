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
        public string? Status { get; set; }
        public string? ResponseMessage { get; set; }
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

        bool isAdmin = user.Role.HasFlag(Role.Admin);

        var query = _context.Leads
            .Include(ld => ld.Listing)
            .AsQueryable();

        // Nghiệp vụ phân quyền tiếp nhận yêu cầu xem nhà:
        // - Admin (Quản trị viên hệ thống): Có quyền quản lý toàn bộ dữ liệu, xem và kiểm tra tất cả các yêu cầu tư vấn / đặt lịch xem nhà gửi lên.
        // - Chủ BĐS / Cho thuê / Người bán / Môi giới: Nhận thông tin đăng ký xem nhà của khách hàng đối với bài đăng BĐS do chính mình niêm yết/phụ trách (OwnerId == userId).
        if (!isAdmin)
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
                ld.ResponseMessage,
                ld.RespondedAt,
                ld.CreatedAt
            })
            .ToListAsync();

        return Ok(leads);
    }

    // 3. Khách hàng (Người mua / Người thuê) xem lịch sử yêu cầu tư vấn & phản hồi từ người bán
    [Authorize]
    [HttpGet("my-sent-requests")]
    public async Task<IActionResult> GetMySentRequests()
    {
        var userIdStr = User.FindFirst("sub")?.Value 
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Không xác định được danh tính người dùng" });

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return Unauthorized(new { message = "Không tìm thấy thông tin tài khoản người dùng" });

        var userEmail = (user.Email ?? string.Empty).Trim().ToLower();

        var requests = await _context.Leads
            .Include(ld => ld.Listing)
            .Include(ld => ld.Owner)
            .Where(ld => ld.CustomerEmail != null && ld.CustomerEmail.ToLower() == userEmail)
            .OrderByDescending(ld => ld.CreatedAt)
            .Select(ld => new
            {
                ld.Id,
                ld.ListingId,
                ListingTitle = ld.Listing != null ? ld.Listing.Title : "Bất động sản #" + ld.ListingId,
                ListingPrice = ld.Listing != null ? ld.Listing.Price : 0,
                ListingAddress = ld.Listing != null ? ld.Listing.Address : "",
                ListingCity = ld.Listing != null ? ld.Listing.City : "",
                ListingDistrict = ld.Listing != null ? ld.Listing.District : "",
                ListingType = ld.Listing != null ? ld.Listing.Type.ToString() : "Sale",
                ListingImage = ld.Listing != null && !string.IsNullOrEmpty(ld.Listing.ImageUrls)
                    ? ld.Listing.ImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() 
                    : null,
                OwnerName = ld.Owner != null ? ld.Owner.FullName : "Người đăng tin",
                OwnerEmail = ld.Owner != null ? ld.Owner.Email : "",
                ld.CustomerName,
                ld.CustomerPhone,
                ld.CustomerEmail,
                ld.Notes,
                Status = ld.Status.ToString(),
                ld.ResponseMessage,
                ld.RespondedAt,
                ld.CreatedAt
            })
            .ToListAsync();

        return Ok(requests);
    }

    // 4. Cập nhật trạng thái khách hàng & Gửi phản hồi lại cho khách
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

        bool isAdmin = user.Role.HasFlag(Role.Admin);
        if (lead.OwnerId != userId && !isAdmin)
            return Forbid();

        if (!string.IsNullOrEmpty(dto.Status) && Enum.TryParse<LeadStatus>(dto.Status, true, out var parsedStatus))
        {
            lead.Status = parsedStatus;
        }

        if (dto.ResponseMessage != null)
        {
            lead.ResponseMessage = dto.ResponseMessage.Trim();
            lead.RespondedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new { 
            message = "Đã cập nhật trạng thái và phản hồi khách hàng thành công!", 
            newStatus = lead.Status.ToString(),
            responseMessage = lead.ResponseMessage
        });
    }
}

