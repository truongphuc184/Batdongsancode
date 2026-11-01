-- ====================================================================
-- SCRIPT CẬP NHẬT DATABASE BẤT ĐỘNG SẢN (DÙNG ĐỂ UPDATE DATABASE ĐANG CÓ)
-- Chạy an toàn: Tự thêm các cột còn thiếu, tự tạo bảng mới nếu chưa có,
-- và nạp đầy đủ dữ liệu mẫu phong phú để Tìm kiếm & Lọc hoạt động hoàn hảo.
-- ====================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'batdongsan')
BEGIN
    CREATE DATABASE batdongsan;
    PRINT N'>>> Đã tạo mới database batdongsan';
END
GO

USE batdongsan;
GO

-- 1. BẢNG USERS
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Email NVARCHAR(100) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(MAX) NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        Role NVARCHAR(50) NOT NULL DEFAULT 'Buyer',
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        RemainingPushes INT NOT NULL DEFAULT 5,
        CurrentPackage NVARCHAR(100) NULL DEFAULT N'Thành viên thường'
    );
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'RemainingPushes')
        ALTER TABLE Users ADD RemainingPushes INT NOT NULL DEFAULT 5;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'CurrentPackage')
        ALTER TABLE Users ADD CurrentPackage NVARCHAR(100) NULL DEFAULT N'Thành viên thường';
END
GO

-- 2. BẢNG LISTINGS
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Listings')
BEGIN
    CREATE TABLE Listings (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NOT NULL,
        Price DECIMAL(18,2) NOT NULL,
        Area FLOAT NOT NULL DEFAULT 0,
        Bedrooms INT NOT NULL DEFAULT 0,
        Bathrooms INT NOT NULL DEFAULT 0,
        Address NVARCHAR(250) NOT NULL,
        City NVARCHAR(100) NOT NULL,
        District NVARCHAR(100) NOT NULL,
        Direction NVARCHAR(50) NULL,
        Type NVARCHAR(50) NOT NULL DEFAULT 'Sale',
        Status NVARCHAR(50) NOT NULL DEFAULT 'Approved',
        OwnerId INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NULL,
        ImageUrls NVARCHAR(MAX) NULL,
        ViewCount INT NOT NULL DEFAULT 0,
        IsVip BIT NOT NULL DEFAULT 0,
        IsVerified BIT NOT NULL DEFAULT 1,
        PriorityTime DATETIME2 NULL,
        RejectReason NVARCHAR(500) NULL,
        CONSTRAINT FK_Listings_Users FOREIGN KEY (OwnerId) REFERENCES Users(Id) ON DELETE CASCADE
    );
END
ELSE
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Listings') AND name = 'UpdatedAt' AND is_nullable = 0)
        ALTER TABLE Listings ALTER COLUMN UpdatedAt DATETIME2 NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Listings') AND name = 'Direction')
        ALTER TABLE Listings ADD Direction NVARCHAR(50) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Listings') AND name = 'ViewCount')
        ALTER TABLE Listings ADD ViewCount INT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Listings') AND name = 'IsVip')
        ALTER TABLE Listings ADD IsVip BIT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Listings') AND name = 'IsVerified')
        ALTER TABLE Listings ADD IsVerified BIT NOT NULL DEFAULT 1;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Listings') AND name = 'PriorityTime')
        ALTER TABLE Listings ADD PriorityTime DATETIME2 NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Listings') AND name = 'RejectReason')
        ALTER TABLE Listings ADD RejectReason NVARCHAR(500) NULL;

    -- Chuẩn hóa dữ liệu cũ không để NULL hoặc sai Status
    UPDATE Listings SET Status = 'Approved' WHERE Status = 'Active';
    UPDATE Listings SET Direction = N'Đông Nam' WHERE Direction IS NULL OR Direction = '';
    UPDATE Listings SET ViewCount = 0 WHERE ViewCount IS NULL;
    UPDATE Listings SET IsVip = 0 WHERE IsVip IS NULL;
    UPDATE Listings SET IsVerified = 1 WHERE IsVerified IS NULL;
    UPDATE Listings SET Area = 0 WHERE Area IS NULL;
    UPDATE Listings SET Bedrooms = 0 WHERE Bedrooms IS NULL;
    UPDATE Listings SET Bathrooms = 0 WHERE Bathrooms IS NULL;
    UPDATE Listings SET CreatedAt = GETDATE() WHERE CreatedAt IS NULL;
    UPDATE Listings SET UpdatedAt = GETDATE() WHERE UpdatedAt IS NULL;
    DELETE FROM Listings WHERE Title IN ('NTP', 'NTTT') OR Price < 1000000;
END
GO

-- 3. BẢNG LEADS (CRM Khách hàng)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Leads')
BEGIN
    CREATE TABLE Leads (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ListingId INT NOT NULL,
        OwnerId INT NOT NULL,
        CustomerName NVARCHAR(100) NOT NULL,
        CustomerPhone NVARCHAR(50) NOT NULL,
        CustomerEmail NVARCHAR(100) NULL,
        Notes NVARCHAR(500) NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'NEW',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Leads_Listings FOREIGN KEY (ListingId) REFERENCES Listings(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Leads_Users FOREIGN KEY (OwnerId) REFERENCES Users(Id) ON DELETE NO ACTION
    );
END
GO

-- 4. BẢNG REPORTS (Báo cáo vi phạm)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Reports')
BEGIN
    CREATE TABLE Reports (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ListingId INT NOT NULL,
        Reason NVARCHAR(200) NOT NULL,
        Details NVARCHAR(500) NULL,
        ReporterName NVARCHAR(100) NULL,
        ReporterPhone NVARCHAR(50) NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Reports_Listings FOREIGN KEY (ListingId) REFERENCES Listings(Id) ON DELETE NO ACTION
    );
END
GO

-- 5. ĐỒNG BỘ MẬT KHẨU '123456' VÀ 6 TÀI KHOẢN MẪU
DECLARE @DefaultHash NVARCHAR(MAX) = '$2a$11$qNA0JOrSYqjFIXS/Z.kpjOSt./3QxrNL/BZHqEjK70cHOU/CDARY.';

UPDATE Users SET PasswordHash = @DefaultHash;

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@batdongsan.vn')
    INSERT INTO Users (Email, PasswordHash, FullName, Role, IsActive, CreatedAt, RemainingPushes, CurrentPackage)
    VALUES ('admin@batdongsan.vn', @DefaultHash, N'Quản trị viên Hệ thống', 'Admin', 1, GETDATE(), 99, N'Hội viên Cao cấp');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'seller@batdongsan.vn')
    INSERT INTO Users (Email, PasswordHash, FullName, Role, IsActive, CreatedAt, RemainingPushes, CurrentPackage)
    VALUES ('seller@batdongsan.vn', @DefaultHash, N'Nguyễn Văn Bán (Chính chủ)', 'Seller', 1, GETDATE(), 10, N'Hội viên Tiêu chuẩn');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'landlord@batdongsan.vn')
    INSERT INTO Users (Email, PasswordHash, FullName, Role, IsActive, CreatedAt, RemainingPushes, CurrentPackage)
    VALUES ('landlord@batdongsan.vn', @DefaultHash, N'Trần Thị Cho Thuê', 'Landlord', 1, GETDATE(), 8, N'Hội viên Cơ bản');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'agent@batdongsan.vn')
    INSERT INTO Users (Email, PasswordHash, FullName, Role, IsActive, CreatedAt, RemainingPushes, CurrentPackage)
    VALUES ('agent@batdongsan.vn', @DefaultHash, N'Lê Hoàng Môi Giới BĐS', 'Agent', 1, GETDATE(), 25, N'Hội viên Cao cấp');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'buyer@batdongsan.vn')
    INSERT INTO Users (Email, PasswordHash, FullName, Role, IsActive, CreatedAt, RemainingPushes, CurrentPackage)
    VALUES ('buyer@batdongsan.vn', @DefaultHash, N'Phạm Khách Mua', 'Buyer', 1, GETDATE(), 0, N'Thành viên thường');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'tenant@batdongsan.vn')
    INSERT INTO Users (Email, PasswordHash, FullName, Role, IsActive, CreatedAt, RemainingPushes, CurrentPackage)
    VALUES ('tenant@batdongsan.vn', @DefaultHash, N'Vũ Khách Thuê Nhà', 'Tenant', 1, GETDATE(), 0, N'Thành viên thường');
GO

-- 6. DỮ LIỆU BẤT ĐỘNG SẢN MẪU ĐA DẠNG TIÊU CHÍ TÌM KIẾM & LỌC
DECLARE @SellerId INT = (SELECT TOP 1 Id FROM Users WHERE Email = 'seller@batdongsan.vn');
DECLARE @LandlordId INT = (SELECT TOP 1 Id FROM Users WHERE Email = 'landlord@batdongsan.vn');
DECLARE @AgentId INT = (SELECT TOP 1 Id FROM Users WHERE Email = 'agent@batdongsan.vn');

IF @SellerId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Listings WHERE Title LIKE N'%Landmark 81%')
BEGIN
    INSERT INTO Listings (Title, Description, Price, Area, Bedrooms, Bathrooms, Address, City, District, Direction, Type, Status, OwnerId, CreatedAt, UpdatedAt, ImageUrls, ViewCount, IsVip, IsVerified, PriorityTime)
    VALUES 
    (N'Bán Biệt thự Đơn lập Vinhomes Riverside Long Biên view hồ', 
     N'Biệt thự view hồ điều hòa, hoàn thiện full nội thất Ý cao cấp, sân vườn rộng thoáng, an ninh 24/7. Sổ đỏ chính chủ trao tay.',
     38500000000, 320, 5, 5, N'Đường Hoa Lan, KĐT Vinhomes Riverside', N'Hà Nội', N'Long Biên', N'Đông Nam', 'Sale', 'Approved', @SellerId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?auto=format&fit=crop&w=1200&q=80', 125, 1, 1, DATEADD(DAY, 1, GETDATE())),

    (N'Bán Căn hộ 2PN Landmark 81 Vinhomes Central Park view sông', 
     N'Căn hộ tầng cao thoáng mát, view trực diện sông Sài Gòn và công viên 14ha. Đã có sổ hồng, tặng toàn bộ nội thất cao cấp.',
     6800000000, 78.5, 2, 2, N'208 Nguyễn Hữu Cảnh, Phường 22', N'Hồ Chí Minh', N'Bình Thạnh', N'Đông', 'Sale', 'Approved', @AgentId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?auto=format&fit=crop&w=1200&q=80', 145, 1, 1, DATEADD(DAY, 2, GETDATE())),

    (N'Cho thuê Căn hộ Penthouse Duplex Thảo Điền Quận 2 view trọn thành phố', 
     N'Căn hộ cao cấp 3 phòng ngủ master, hồ bơi riêng trên tầng thượng, nội thất phong cách Châu Âu hiện đại.',
     45000000, 210, 3, 3, N'Đường Xuân Thủy, Phường Thảo Điền', N'Hồ Chí Minh', N'Quận 2', N'Nam', 'Rent', 'Approved', @LandlordId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1512917774080-9991f1c4c750?auto=format&fit=crop&w=1200&q=80', 88, 1, 1, DATEADD(DAY, 1, GETDATE())),

    (N'Bán Nhà phố Mặt tiền Nguyễn Văn Linh Đà Nẵng tiện kinh doanh', 
     N'Vị trí đắc địa trung tâm thành phố Đà Nẵng, gần cầu Rồng. Diện tích đất 110m2, xây dựng 4 tầng kiên cố, đang cho ngân hàng thuê 50 tr/tháng.',
     16500000000, 110, 4, 4, N'Đường Nguyễn Văn Linh, Phường Nam Dương', N'Đà Nẵng', N'Hải Châu', N'Bắc', 'Sale', 'Approved', @AgentId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1568605117036-5fe5e7bab0b7?auto=format&fit=crop&w=1200&q=80', 73, 0, 1, NULL),

    (N'Cho thuê Nhà nguyên căn 3 tầng ngõ ô tô Cầu Giấy Hà Nội', 
     N'Nhà mới sửa đẹp, ngõ rộng ô tô đỗ cửa, thích hợp làm văn phòng công ty hoặc gia đình ở kết hợp kinh doanh online.',
     18000000, 65, 3, 3, N'Ngõ 165 Cầu Giấy, Phường Dịch Vọng', N'Hà Nội', N'Cầu Giấy', N'Tây Nam', 'Rent', 'Approved', @LandlordId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1580587771525-78b9dba3b914?auto=format&fit=crop&w=1200&q=80', 56, 0, 1, NULL),

    (N'Bán Căn hộ Studio Vinhomes Smart City Tây Mỗ giá tốt', 
     N'Căn hộ Studio thiết kế thông minh, ban công thoáng mát, đầy đủ tiện ích biển hồ, vườn Nhật, trung tâm thương mại Vincom.',
     1350000000, 32, 1, 1, N'KĐT Vinhomes Smart City, Tây Mỗ', N'Hà Nội', N'Nam Từ Liêm', N'Tây', 'Sale', 'Approved', @SellerId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80', 92, 0, 1, NULL),

    (N'Cho thuê Shophouse chân đế KĐT Ecopark Văn Giang', 
     N'Vị trí đắc địa ngay mặt đường trục chính Ecopark, dân cư đông đúc, vỉa hè rộng 8m thuận tiện mở cafe, nhà hàng, spa.',
     35000000, 120, 2, 2, N'Khu phố Thủy Nguyên, KĐT Ecopark', N'Hà Nội', N'Gia Lâm', N'Đông Bắc', 'Rent', 'Approved', @AgentId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=1200&q=80', 67, 0, 1, NULL);
END

-- 7. DỮ LIỆU MẪU CHO LEADS (CRM KHÁCH HÀNG)
DECLARE @FirstListingId INT = (SELECT TOP 1 Id FROM Listings WHERE Status = 'Approved');
IF @FirstListingId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Leads)
BEGIN
    INSERT INTO Leads (ListingId, OwnerId, CustomerName, CustomerPhone, CustomerEmail, Notes, Status, CreatedAt)
    VALUES 
    (@FirstListingId, @SellerId, N'Trần Minh Quân', '0988123456', 'quan.tran@gmail.com', N'Muốn hẹn xem nhà vào 9h sáng thứ 7 tuần này', 'VIEWING', GETDATE()),
    (@FirstListingId, @SellerId, N'Hoàng Thu Thảo', '0912888999', 'thao.hoang@yahoo.com', N'Cần hỗ trợ tư vấn thủ tục vay ngân hàng 70%', 'CONSULTING', DATEADD(HOUR, -5, GETDATE())),
    (@FirstListingId, @AgentId, N'Nguyễn Đức Anh', '0903555777', 'ducanh88@gmail.com', N'Đang đàm phán bớt lộc và hẹn chốt cọc', 'NEGOTIATING', DATEADD(DAY, -1, GETDATE()));
END

-- 8. DỮ LIỆU MẪU CHO REPORTS (BÁO CÁO VI PHẠM)
IF @FirstListingId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reports)
BEGIN
    INSERT INTO Reports (ListingId, Reason, Details, ReporterName, ReporterPhone, Status, CreatedAt)
    VALUES 
    (@FirstListingId, N'Sai giá niêm yết', N'Chủ nhà thông báo mức giá khác khi tôi liên hệ qua điện thoại', N'Phạm Văn Hùng', '0977666555', 'Pending', GETDATE()),
    (@FirstListingId, N'Tin ảo / Đã bán / Không có thật', N'Căn này môi giới báo đã chốt cọc từ tuần trước', N'Lê Minh Trang', '0933222111', 'Pending', DATEADD(HOUR, -8, GETDATE()));
END
GO

PRINT N'====================================================================';
PRINT N'>>> CẬP NHẬT DATABASE THÀNH CÔNG VÀ NẠP ĐẦY ĐỦ DATA 100%!';
PRINT N'====================================================================';
GO
