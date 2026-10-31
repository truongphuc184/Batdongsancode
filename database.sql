-- ====================================================================
-- HỆ THỐNG QUẢN LÝ BẤT ĐỘNG SẢN - SCRIPT TẠO DATABASE SQL SERVER CHUẨN
-- Chạy script này trên SQL Server Management Studio (SSMS)
-- ====================================================================

-- 1. TẠO CƠ SỞ DỮ LIỆU BATDONGSAN
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'batdongsan')
BEGIN
    CREATE DATABASE batdongsan;
    PRINT N'>>> Đã tạo database batdongsan thành công!';
END
GO

USE batdongsan;
GO

-- 2. TẠO BẢNG USERS (Tài khoản người dùng & Phân quyền 6 Actor)
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
    PRINT N'>>> Đã tạo bảng Users';
END
GO

-- 3. TẠO BẢNG LISTINGS (Tin đăng bất động sản)
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
    PRINT N'>>> Đã tạo bảng Listings';
END
GO

-- 4. TẠO BẢNG LEADS (Khách hàng tiềm năng & Phân hệ CRM)
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
    PRINT N'>>> Đã tạo bảng Leads (CRM)';
END
GO

-- 5. TẠO BẢNG REPORTS (Báo cáo vi phạm tin đăng)
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
        CONSTRAINT FK_Reports_Listings FOREIGN KEY (ListingId) REFERENCES Listings(Id) ON DELETE CASCADE
    );
    PRINT N'>>> Đã tạo bảng Reports';
END
GO

-- 6. DỮ LIỆU MẪU: 6 VAI TRÒ (MẬT KHẨU MẶC ĐỊNH: 123456)
-- Hash BCrypt chuẩn 100% của mật khẩu '123456'
DECLARE @DefaultHash NVARCHAR(MAX) = '$2a$11$qNA0JOrSYqjFIXS/Z.kpjOSt./3QxrNL/BZHqEjK70cHOU/CDARY.';

-- Cập nhật lại mật khẩu chuẩn nếu tài khoản đã tồn tại
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

-- 7. DỮ LIỆU MẪU: BẤT ĐỘNG SẢN CHẤT LƯỢNG CAO
DECLARE @SellerId INT = (SELECT TOP 1 Id FROM Users WHERE Email = 'seller@batdongsan.vn');
DECLARE @LandlordId INT = (SELECT TOP 1 Id FROM Users WHERE Email = 'landlord@batdongsan.vn');
DECLARE @AgentId INT = (SELECT TOP 1 Id FROM Users WHERE Email = 'agent@batdongsan.vn');

IF @SellerId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Listings WHERE Title LIKE N'%Vinhomes Riverside%')
BEGIN
    INSERT INTO Listings (Title, Description, Price, Area, Bedrooms, Bathrooms, Address, City, District, Direction, Type, Status, OwnerId, CreatedAt, ImageUrls, ViewCount, IsVip, IsVerified, PriorityTime)
    VALUES 
    (N'Bán Biệt thự Đơn lập Vinhomes Riverside Long Biên view hồ', 
     N'Biệt thự view hồ điều hòa, hoàn thiện full nội thất Ý cao cấp, sân vườn rộng thoáng, an ninh 24/7. Sổ đỏ chính chủ trao tay.',
     38500000000, 320, 5, 5, N'Đường Hoa Lan, KĐT Vinhomes Riverside', N'Hà Nội', N'Long Biên', N'Đông Nam', 'Sale', 'Approved', @SellerId, GETDATE(),
     'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?auto=format&fit=crop&w=1200&q=80', 125, 1, 1, DATEADD(DAY, 1, GETDATE())),

    (N'Bán Căn hộ 2PN Landmark 81 Vinhomes Central Park view sông Sài Gòn', 
     N'Căn hộ tầng cao thoáng mát, view trực diện sông và công viên 14ha. Đã có sổ hồng, tặng toàn bộ nội thất cao cấp.',
     6800000000, 78.5, 2, 2, N'208 Nguyễn Hữu Cảnh, Phường 22', N'Hồ Chí Minh', N'Bình Thạnh', N'Đông', 'Sale', 'Approved', @AgentId, GETDATE(),
     'https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?auto=format&fit=crop&w=1200&q=80', 89, 1, 1, DATEADD(HOUR, 12, GETDATE())),

    (N'Cho thuê Căn hộ Penthouse Duplex Thảo Điền Quận 2 view trọn thành phố', 
     N'Căn hộ cao cấp 3 phòng ngủ master, hồ bơi riêng trên tầng thượng, nội thất phong cách Châu Âu hiện đại.',
     45000000, 210, 3, 3, N'Đường Xuân Thủy, Phường Thảo Điền', N'Hồ Chí Minh', N'Quận 2', N'Nam', 'Rent', 'Approved', @LandlordId, GETDATE(),
     'https://images.unsplash.com/photo-1512917774080-9991f1c4c750?auto=format&fit=crop&w=1200&q=80', 64, 0, 1, NULL),

    (N'Bán Nhà phố Mặt tiền Nguyễn Văn Linh Đà Nẵng tiện kinh doanh', 
     N'Vị trí đắc địa trung tâm thành phố Đà Nẵng, gần cầu Rồng. Diện tích đất 110m2, xây dựng 4 tầng kiên cố, đang cho ngân hàng thuê.',
     16500000000, 110, 4, 4, N'Đường Nguyễn Văn Linh, Phường Nam Dương', N'Đà Nẵng', N'Hải Châu', N'Bắc', 'Sale', 'Approved', @AgentId, GETDATE(),
     'https://images.unsplash.com/photo-1568605117036-5fe5e7bab0b7?auto=format&fit=crop&w=1200&q=80', 42, 0, 1, NULL),

    (N'Cho thuê Nhà nguyên căn 3 tầng ngõ ô tô Cầu Giấy Hà Nội', 
     N'Nhà mới sửa đẹp, ngõ rộng ô tô đỗ cửa, thích hợp làm văn phòng công ty hoặc gia đình ở kết hợp kinh doanh online.',
     18000000, 65, 3, 3, N'Ngõ 165 Cầu Giấy, Phường Dịch Vọng', N'Hà Nội', N'Cầu Giấy', N'Tây Nam', 'Rent', 'Approved', @LandlordId, GETDATE(),
     'https://images.unsplash.com/photo-1580587771525-78b9dba3b914?auto=format&fit=crop&w=1200&q=80', 31, 0, 1, NULL);
END
GO

PRINT N'====================================================================';
PRINT N'>>> KHỞI TẠO CƠ SỞ DỮ LIỆU SQL SERVER THÀNH CÔNG RỰC RỠ!';
PRINT N'>>> TÀI KHOẢN MẪU:';
PRINT N'    - Admin:     admin@batdongsan.vn    / 123456';
PRINT N'    - Người bán: seller@batdongsan.vn   / 123456';
PRINT N'    - Cho thuê:  landlord@batdongsan.vn / 123456';
PRINT N'    - Môi giới:  agent@batdongsan.vn    / 123456';
PRINT N'    - Người mua: buyer@batdongsan.vn    / 123456';
PRINT N'    - Người thuê:tenant@batdongsan.vn   / 123456';
PRINT N'====================================================================';
GO

