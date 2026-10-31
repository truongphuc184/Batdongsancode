using Batdongsancode.Models;

namespace Batdongsancode.Services.Interfaces;

public interface IListingService
{
    Task<List<Listing>> GetAllAsync();
    Task<Listing?> GetByIdAsync(int id);
    Task<Listing> CreateAsync(Listing listing);
    Task<bool> UpdateAsync(Listing listing);
    Task<bool> DeleteAsync(int id);
    Task<List<Listing>> SearchAsync(string? keyword, string? city, PropertyType? type, decimal? minPrice, decimal? maxPrice);
}

