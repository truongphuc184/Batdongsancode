namespace Batdongsancode.Models
{
    [Flags]
    public enum Role
    {
        None = 0,
        Buyer = 1,      // Người mua
        Tenant = 2,     // Người thuê
        Seller = 4,     // Người bán
        Landlord = 8,   // Người cho thuê
        Agent = 16,     // Môi giới
        Broker = 16,    // Tương thích ngược
        Agency = 32,
        Admin = 64      // Quản trị viên
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

    // Trạng thái khách hàng trong phân hệ CRM (Class Diagram)
    public enum LeadStatus
    {
        NEW,          // Mới tiếp nhận
        CONSULTING,   // Đang tư vấn
        VIEWING,      // Hẹn xem nhà
        NEGOTIATING,  // Đang đàm phán
        DEALT,        // Đã chốt giao dịch
        CANCELED      // Đã hủy / không phù hợp
    }
}

