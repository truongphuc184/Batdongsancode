using Batdongsancode.Data;
using Batdongsancode.Models;
using Batdongsancode.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Batdongsancode.Services.Implementations;

public class ListingService : IListingService
{
    private readonly AppDbContext _context;

    public ListingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Listing>> GetAllAsync()
    {
        return await _context.Listings.Include(l => l.Owner).ToListAsync();
    }

    public async Task<Listing?> GetByIdAsync(int id)
    {
        return await _context.Listings.Include(l => l.Owner).FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<Listing> CreateAsync(Listing listing)
    {
        listing.CreatedAt = DateTime.UtcNow;
        listing.UpdatedAt = DateTime.UtcNow;
        _context.Listings.Add(listing);
        await _context.SaveChangesAsync();
        return listing;
    }

    public async Task<bool> UpdateAsync(Listing listing)
    {
        var existing = await _context.Listings.FindAsync(listing.Id);
        if (existing == null) return false;

        existing.Title = listing.Title;
        existing.Description = listing.Description;
        existing.Type = listing.Type;
        existing.Price = listing.Price;
        existing.Address = listing.Address;
        existing.City = listing.City;
        existing.District = listing.District;
        existing.Area = listing.Area;
        existing.Bedrooms = listing.Bedrooms;
        existing.Bathrooms = listing.Bathrooms;
        existing.ImageUrls = listing.ImageUrls;
        existing.Status = listing.Status;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var listing = await _context.Listings.FindAsync(id);
        if (listing == null) return false;

        _context.Listings.Remove(listing);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Listing>> SearchAsync(string? keyword, string? city, PropertyType? type, decimal? minPrice, decimal? maxPrice)
    {
        var query = _context.Listings.Include(l => l.Owner).AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(l => l.Title.Contains(keyword) || l.Description.Contains(keyword));

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(l => l.City == city);

        if (type.HasValue)
            query = query.Where(l => l.Type == type.Value);

        if (minPrice.HasValue)
            query = query.Where(l => l.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(l => l.Price <= maxPrice.Value);

        return await query.ToListAsync();
    }
}

