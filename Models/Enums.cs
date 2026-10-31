namespace Batdongsancode.Models
{
    public enum Role
    {
        Buyer,      // Người mua
        Tenant,     // Người thuê
        Seller,     // Người bán
        Landlord,   // Người cho thuê
        Agent,      // Môi giới
        Broker = Agent, // Tương thích ngược
        Agency,
        Admin       // Quản trị viên
    }

    public enum PropertyType
    {
        Sale,       // Mua bán
        Rent        // Cho thuê
    }

    public enum ListingStatus
    {
        Pending,    // Chờ duyệt
        Approved,   // Đã duyệt (đang hiển thị)
        Active = Approved, // Tương thích ngược
        Rejected,   // Bị từ chối
        Expired,    // Hết hạn
        Sold,       // Đã bán
        Rented,     // Đã cho thuê
        Hidden      // Ẩn tin
    }
}

