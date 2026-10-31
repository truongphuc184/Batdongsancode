namespace Batdongsancode.Models
{
    public enum Role
    {
        Buyer,
        Seller,
        Broker,
        Agency,
        Admin
    }

    public enum PropertyType
    {
        Sale,
        Rent
    }

    public enum ListingStatus
    {
        Pending,
        Active,
        Rejected,
        Expired,
        Sold,
        Rented,
        Hidden
    }
}

