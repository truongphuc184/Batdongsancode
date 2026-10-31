using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Batdongsancode.Models;
using Batdongsancode.Services.Interfaces;

namespace Batdongsancode.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListingsController : ControllerBase
{
    private readonly IListingService _listingService;

    public ListingsController(IListingService listingService)
    {
        _listingService = listingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var listings = await _listingService.GetAllAsync();
        return Ok(listings.Select(l => new
        {
            l.Id, l.Title, l.Description, l.Address, l.City, l.District,
            l.Price, l.Area, l.Bedrooms, l.Bathrooms, l.ImageUrls,
            Type = l.Type.ToString(),
            Status = l.Status.ToString(),
            l.OwnerId, l.CreatedAt, l.UpdatedAt
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var listing = await _listingService.GetByIdAsync(id);
        if (listing == null) return NotFound();
        return Ok(new
        {
            listing.Id, listing.Title, listing.Description, listing.Address,
            listing.City, listing.District, listing.Price, listing.Area,
            listing.Bedrooms, listing.Bathrooms, listing.ImageUrls,
            Type = listing.Type.ToString(),
            Status = listing.Status.ToString(),
            listing.OwnerId, listing.CreatedAt, listing.UpdatedAt,
            OwnerName = listing.Owner?.FullName
        });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? keyword,
        [FromQuery] string? city,
        [FromQuery] PropertyType? type,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice)
    {
        var results = await _listingService.SearchAsync(keyword, city, type, minPrice, maxPrice);
        return Ok(results.Select(l => new
        {
            l.Id, l.Title, l.Description, l.Address, l.City, l.District,
            l.Price, l.Area, l.Bedrooms, l.Bathrooms, l.ImageUrls,
            Type = l.Type.ToString(),
            Status = l.Status.ToString(),
            l.OwnerId, l.CreatedAt
        }));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateListingRequest request)
    {
        var userId = int.Parse(User.FindFirst("sub")!.Value);
        var listing = new Listing
        {
            Title = request.Title,
            Description = request.Description,
            Type = Enum.Parse<PropertyType>(request.Type, true),
            Price = request.Price,
            Address = request.Address,
            City = request.City,
            District = request.District,
            Area = request.Area,
            Bedrooms = request.Bedrooms,
            Bathrooms = request.Bathrooms,
            ImageUrls = request.ImageUrls ?? "",
            Status = ListingStatus.Active,
            OwnerId = userId
        };

        var created = await _listingService.CreateAsync(listing);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, new { created.Id, message = "Tạo tin đăng thành công" });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _listingService.DeleteAsync(id);
        if (!success) return NotFound();
        return Ok(new { message = "Đã xóa tin đăng" });
    }
}

public class CreateListingRequest
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = "";
    public string Type { get; set; } = "Sale";
    public decimal Price { get; set; }
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string District { get; set; } = "";
    public double Area { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public string? ImageUrls { get; set; }
}

