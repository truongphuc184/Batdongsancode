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
        FullName NVARCHAR(100) NOT NULL DEFAULT '',
        Role NVARCHAR(50) NOT NULL DEFAULT 'Buyer',
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        RemainingPushes INT NOT NULL DEFAULT 5,
        CurrentPackage NVARCHAR(100) NULL DEFAULT N'Thành viên thường',
        IsVerified BIT NOT NULL DEFAULT 0,
        KycStatus NVARCHAR(50) NOT NULL DEFAULT 'None',
        Phone NVARCHAR(50) NULL,
        PhoneVerified BIT NOT NULL DEFAULT 0,
        IdCardNumber NVARCHAR(50) NULL,
        IdCardFront NVARCHAR(MAX) NULL,
        IdCardBack NVARCHAR(MAX) NULL,
        TaxCode NVARCHAR(50) NULL,
        TaxType NVARCHAR(50) NULL,
        BrokerLicenseNumber NVARCHAR(100) NULL,
        BrokerLicenseImage NVARCHAR(MAX) NULL,
        PendingRoles NVARCHAR(100) NULL,
        KycRejectReason NVARCHAR(500) NULL
    );
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'RemainingPushes')
        ALTER TABLE Users ADD RemainingPushes INT NOT NULL DEFAULT 5;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'CurrentPackage')
        ALTER TABLE Users ADD CurrentPackage NVARCHAR(100) NULL DEFAULT N'Thành viên thường';

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'IsVerified')
        ALTER TABLE Users ADD IsVerified BIT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'KycStatus')
        ALTER TABLE Users ADD KycStatus NVARCHAR(50) NOT NULL DEFAULT 'None';

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'Phone')
        ALTER TABLE Users ADD Phone NVARCHAR(50) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'PhoneVerified')
        ALTER TABLE Users ADD PhoneVerified BIT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'IdCardNumber')
        ALTER TABLE Users ADD IdCardNumber NVARCHAR(50) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'IdCardFront')
        ALTER TABLE Users ADD IdCardFront NVARCHAR(MAX) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'IdCardBack')
        ALTER TABLE Users ADD IdCardBack NVARCHAR(MAX) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'TaxCode')
        ALTER TABLE Users ADD TaxCode NVARCHAR(50) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'TaxType')
        ALTER TABLE Users ADD TaxType NVARCHAR(50) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'BrokerLicenseNumber')
        ALTER TABLE Users ADD BrokerLicenseNumber NVARCHAR(100) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'BrokerLicenseImage')
        ALTER TABLE Users ADD BrokerLicenseImage NVARCHAR(MAX) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'PendingRoles')
        ALTER TABLE Users ADD PendingRoles NVARCHAR(100) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'KycRejectReason')
        ALTER TABLE Users ADD KycRejectReason NVARCHAR(500) NULL;
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
        ResponseMessage NVARCHAR(500) NULL,
        RespondedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Leads_Listings FOREIGN KEY (ListingId) REFERENCES Listings(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Leads_Users FOREIGN KEY (OwnerId) REFERENCES Users(Id) ON DELETE NO ACTION
    );
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'ResponseMessage')
        ALTER TABLE Leads ADD ResponseMessage NVARCHAR(500) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'RespondedAt')
        ALTER TABLE Leads ADD RespondedAt DATETIME2 NULL;
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

-- 5. BẢNG KYCREQUESTS (Hồ sơ xác minh danh tính & Quyền đăng tin)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KycRequests')
BEGIN
    CREATE TABLE KycRequests (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        Phone NVARCHAR(50) NOT NULL,
        IdCardNumber NVARCHAR(50) NOT NULL,
        IdCardFront NVARCHAR(MAX) NOT NULL,
        IdCardBack NVARCHAR(MAX) NOT NULL,
        TaxCode NVARCHAR(50) NULL,
        TaxType NVARCHAR(50) NULL,
        RequestedRole NVARCHAR(100) NOT NULL,
        BrokerLicenseNumber NVARCHAR(100) NULL,
        BrokerLicenseImage NVARCHAR(MAX) NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'PENDING',
        RejectReason NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        ReviewedAt DATETIME2 NULL,
        ReviewedBy INT NULL,
        CONSTRAINT FK_KycRequests_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
END
GO

-- 5. ĐỒNG BỘ MẬT KHẨU '123456' VÀ CÁC TÀI KHOẢN MẪU ĐẦY ĐỦ CÁC GÓI
DECLARE @DefaultHash NVARCHAR(MAX) = '$2a$11$qNA0JOrSYqjFIXS/Z.kpjOSt./3QxrNL/BZHqEjK70cHOU/CDARY.';

UPDATE Users SET PasswordHash = @DefaultHash;
-- Quy tắc: Tất cả tài khoản đã lập từ trước đều mặc định đã được xác minh KYC (Approved)
UPDATE Users SET IsVerified = 1, KycStatus = 'Approved';

-- Tài khoản Quản trị & Hội viên tháng
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

-- Tài khoản người dùng đã mua các gói đẩy tin lẻ
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'owner10@batdongsan.vn')
    INSERT INTO Users (Email, PasswordHash, FullName, Role, IsActive, CreatedAt, RemainingPushes, CurrentPackage)
    VALUES ('owner10@batdongsan.vn', @DefaultHash, N'Vũ Đình Cường (Chủ biệt thự)', 'Seller', 1, DATEADD(DAY, -1, GETDATE()), 10, N'Gói Chính chủ 10 lượt (Vĩnh viễn)');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'weekend@batdongsan.vn')
    INSERT INTO Users (Email, PasswordHash, FullName, Role, IsActive, CreatedAt, RemainingPushes, CurrentPackage)
    VALUES ('weekend@batdongsan.vn', @DefaultHash, N'Phan Văn Nam (Chủ nhà)', 'Seller', 1, DATEADD(DAY, -2, GETDATE()), 4, N'Gói Cuối tuần vàng (4 lượt)');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'quick3@batdongsan.vn')
    INSERT INTO Users (Email, PasswordHash, FullName, Role, IsActive, CreatedAt, RemainingPushes, CurrentPackage)
    VALUES ('quick3@batdongsan.vn', @DefaultHash, N'Hoàng Thị Mai (Chủ căn hộ)', 'Seller', 1, DATEADD(DAY, -3, GETDATE()), 3, N'Combo Bán nhanh 3 lượt');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'single@batdongsan.vn')
    INSERT INTO Users (Email, PasswordHash, FullName, Role, IsActive, CreatedAt, RemainingPushes, CurrentPackage)
    VALUES ('single@batdongsan.vn', @DefaultHash, N'Đỗ Đức Toàn (Chính chủ)', 'Seller', 1, DATEADD(DAY, -4, GETDATE()), 1, N'Đẩy tin lẻ 1 lượt');

-- Tài khoản Người mua và Người thuê
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'buyer@batdongsan.vn')
    INSERT INTO Users (Email, PasswordHash, FullName, Role, IsActive, CreatedAt, RemainingPushes, CurrentPackage)
    VALUES ('buyer@batdongsan.vn', @DefaultHash, N'Phạm Khách Mua', 'Buyer', 1, GETDATE(), 0, N'Thành viên thường');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'tenant@batdongsan.vn')
    INSERT INTO Users (Email, PasswordHash, FullName, Role, IsActive, CreatedAt, RemainingPushes, CurrentPackage)
    VALUES ('tenant@batdongsan.vn', @DefaultHash, N'Vũ Khách Thuê Nhà', 'Tenant', 1, GETDATE(), 0, N'Thành viên thường');
GO

-- 6. DỮ LIỆU BẤT ĐỘNG SẢN MẪU ĐA DẠNG TIÊU CHÍ VÀ ĐẦY ĐỦ NHIỀU ẢNH THỰC TẾ
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
     'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600566753376-12c8ab7fb75b?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600573472591-ee6b68d14c68?auto=format&fit=crop&w=1200&q=80', 
     125, 1, 1, DATEADD(DAY, 1, GETDATE())),

    (N'Bán Căn hộ 2PN Landmark 81 Vinhomes Central Park view sông', 
     N'Căn hộ tầng cao thoáng mát, view trực diện sông Sài Gòn và công viên 14ha. Đã có sổ hồng, tặng toàn bộ nội thất cao cấp.',
     6800000000, 78.5, 2, 2, N'208 Nguyễn Hữu Cảnh, Phường 22', N'Hồ Chí Minh', N'Bình Thạnh', N'Đông', 'Sale', 'Approved', @AgentId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=1200&q=80', 
     145, 1, 1, DATEADD(DAY, 2, GETDATE())),

    (N'Cho thuê Căn hộ Penthouse Duplex Thảo Điền Quận 2 view trọn thành phố', 
     N'Căn hộ cao cấp 3 phòng ngủ master, hồ bơi riêng trên tầng thượng, nội thất phong cách Châu Âu hiện đại.',
     45000000, 210, 3, 3, N'Đường Xuân Thủy, Phường Thảo Điền', N'Hồ Chí Minh', N'Quận 2', N'Nam', 'Rent', 'Approved', @LandlordId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1512917774080-9991f1c4c750?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1512918728675-ed5a9ecdebfd?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1613490493576-7fde63acd811?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600607687920-4e2a09cf159d?auto=format&fit=crop&w=1200&q=80', 
     88, 1, 1, DATEADD(DAY, 1, GETDATE())),

    (N'Bán Nhà phố Mặt tiền Nguyễn Văn Linh Đà Nẵng tiện kinh doanh', 
     N'Vị trí đắc địa trung tâm thành phố Đà Nẵng, gần cầu Rồng. Diện tích đất 110m2, xây dựng 4 tầng kiên cố, đang cho ngân hàng thuê 50 tr/tháng.',
     16500000000, 110, 4, 4, N'Đường Nguyễn Văn Linh, Phường Nam Dương', N'Đà Nẵng', N'Hải Châu', N'Bắc', 'Sale', 'Approved', @AgentId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1568605117036-5fe5e7bab0b7?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1580587771525-78b9dba3b914?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1513694203232-719a280e022f?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1502005229762-ee1b2da97e06?auto=format&fit=crop&w=1200&q=80', 
     73, 0, 1, NULL),

    (N'Cho thuê Nhà nguyên căn 3 tầng ngõ ô tô Cầu Giấy Hà Nội', 
     N'Nhà mới sửa đẹp, ngõ rộng ô tô đỗ cửa, thích hợp làm văn phòng công ty hoặc gia đình ở kết hợp kinh doanh online.',
     18000000, 65, 3, 3, N'Ngõ 165 Cầu Giấy, Phường Dịch Vọng', N'Hà Nội', N'Cầu Giấy', N'Tây Nam', 'Rent', 'Approved', @LandlordId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1580587771525-78b9dba3b914?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1513694203232-719a280e022f?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1598928506311-c55ded91a20c?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1584622650111-993a426fbf0a?auto=format&fit=crop&w=1200&q=80', 
     56, 0, 1, NULL),

    (N'Bán Căn hộ Studio Vinhomes Smart City Tây Mỗ giá tốt', 
     N'Căn hộ Studio thiết kế thông minh, ban công thoáng mát, đầy đủ tiện ích biển hồ, vườn Nhật, trung tâm thương mại Vincom.',
     1350000000, 32, 1, 1, N'KĐT Vinhomes Smart City, Tây Mỗ', N'Hà Nội', N'Nam Từ Liêm', N'Tây', 'Sale', 'Approved', @SellerId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1484154218962-a197022b5858?auto=format&fit=crop&w=1200&q=80', 
     92, 0, 1, NULL),

    (N'Cho thuê Shophouse chân đế KĐT Ecopark Văn Giang', 
     N'Vị trí đắc địa ngay mặt đường trục chính Ecopark, dân cư đông đúc, vỉa hè rộng 8m thuận tiện mở cafe, nhà hàng, spa.',
     35000000, 120, 2, 2, N'Khu phố Thủy Nguyên, KĐT Ecopark', N'Hà Nội', N'Gia Lâm', N'Đông Bắc', 'Rent', 'Approved', @AgentId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1497366216548-37526070297c?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1497215728101-856f4ea42174?auto=format&fit=crop&w=1200&q=80', 
     67, 0, 1, NULL),

    (N'Biệt thự Đảo Ecopark Grand The Island ven hồ sinh thái',
     N'Tuyệt tác biệt thự ven hồ sinh thái Ecopark Grand. Diện tích 300m2, thiết kế 3 tầng phong cách Châu Âu hiện đại, sân vườn riêng biệt, bể bơi vô cực ngắm hồ. Sổ đỏ lâu dài.',
     32000000000, 300, 5, 6, N'Khu đô thị Ecopark, Phụng Công', N'Hưng Yên', N'Văn Giang', N'Đông Nam', 'Sale', 'Approved', @SellerId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1613490493576-7fde63acd811?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600566753376-12c8ab7fb75b?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1200&q=80',
     185, 1, 1, DATEADD(DAY, 5, GETDATE())),

    (N'Căn hộ The Metropole Thủ Thiêm 2PN view trực diện sông Sài Gòn',
     N'Cần bán căn hộ cao cấp The Metropole Thủ Thiêm phân khu The Opera Residence. Tầng cao view Panorama trực diện sông Sài Gòn và Bitexco, nội thất bàn giao ngoại nhập cao cấp.',
     11500000000, 85, 2, 2, N'Khu đô thị mới Thủ Thiêm, An Khánh', N'Hồ Chí Minh', N'Thành phố Thủ Đức', N'Tây Nam', 'Sale', 'Approved', @AgentId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1512918728675-ed5a9ecdebfd?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=1200&q=80',
     142, 1, 1, DATEADD(DAY, 4, GETDATE())),

    (N'Nhà phố liền kề Gamuda Gardens Hoàng Mai đầy đủ nội thất gỗ lim',
     N'Nhà xây 4 tầng chắc chắn, diện tích đất 118m2, mặt tiền 5.5m. Đường trước nhà 13m ô tô tránh thoải mái, tiện ích club house, bể bơi Olympic, trường quốc tế SIS.',
     18500000000, 118, 4, 5, N'KĐT Gamuda Gardens, Yên Sở', N'Hà Nội', N'Hoàng Mai', N'Nam', 'Sale', 'Approved', @SellerId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1580587771525-78b9dba3b914?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1513694203232-719a280e022f?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1598928506311-c55ded91a20c?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1200&q=80',
     98, 0, 1, NULL),

    (N'Căn hộ Masteri Centre Point Vinhomes Grand Park công viên 36ha',
     N'Căn 2 phòng ngủ 72m2 tại Masteri Centre Point phân khu compound cao cấp nhất đại đô thị. View trọn đại công viên 36ha và biển hồ cát trắng. Sắp bàn giao nhận nhà ngay.',
     3850000000, 72, 2, 2, N'Khu đô thị Vinhomes Grand Park, Long Thạnh Mỹ', N'Hồ Chí Minh', N'Thành phố Thủ Đức', N'Đông', 'Sale', 'Approved', @AgentId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1484154218962-a197022b5858?auto=format&fit=crop&w=1200&q=80',
     115, 0, 1, NULL),

    (N'Cho thuê Căn hộ Duplex DCapitale Trần Duy Hưng full nội thất Ý',
     N'Cho thuê căn Duplex thông tầng toà C6 DCapitale Trần Duy Hưng. Thiết kế sang trọng 3 phòng ngủ, kính Low-E full từ sàn tới trần, view hồ công viên Thanh Xuân. Phù hợp chuyên gia nước ngoài.',
     28000000, 120, 3, 3, N'119 Trần Duy Hưng, Trung Hòa', N'Hà Nội', N'Cầu Giấy', N'Đông Nam', 'Rent', 'Approved', @LandlordId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1502005229762-ee1b2da97e06?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1512918728675-ed5a9ecdebfd?auto=format&fit=crop&w=1200&q=80',
     89, 1, 1, DATEADD(DAY, 3, GETDATE())),

    (N'Cho thuê Nhà xưởng / Kho bãi KCN VSIP 1 Thuận An PCCC chuẩn quốc tế',
     N'Cho thuê nhà xưởng tiêu chuẩn công nghiệp 1.500m2 tại KCN VSIP 1 Bình Dương. Trạm biến áp riêng 400kVA, trần cao 9m, sàn epoxy tải trọng 5 tấn/m2, hệ thống PCCC tự động nghiệm thu đầy đủ.',
     65000000, 1500, 0, 4, N'Đại lộ Hữu Nghị, KCN VSIP 1, An Phú', N'Bình Dương', N'Thuận An', N'Bắc', 'Rent', 'Approved', @LandlordId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1587293852726-70cdb56c2866?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1504307651254-35680f356dfd?auto=format&fit=crop&w=1200&q=80',
     74, 0, 1, NULL);

    -- 6 TIN CHỜ KIỂM DUYỆT (PENDING) DÀNH CHO ADMIN
    INSERT INTO Listings (Title, Description, Price, Area, Bedrooms, Bathrooms, Address, City, District, Direction, Type, Status, OwnerId, CreatedAt, UpdatedAt, ImageUrls, ViewCount, IsVip, IsVerified, PriorityTime)
    VALUES 
    (N'Bán nhà riêng ngõ 3 tầng phố Thái Hà Đống Đa ngõ thông thoáng',
     N'Nhà xây khung cột bê tông 3 tầng kiên cố, ngõ 2.5m thông ra Hoàng Cầu, gần các trường đại học lớn. Sổ đỏ vuông vắn cất két sẵn sàng giao dịch.',
     6200000000, 45, 3, 3, N'Ngõ 117 Thái Hà, Trung Liệt', N'Hà Nội', N'Đống Đa', N'Đông', 'Sale', 'Pending', @SellerId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1568605117036-5fe5e7bab0b7?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1512917774080-9991f1c4c750?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1580587771525-78b9dba3b914?auto=format&fit=crop&w=1200&q=80',
     0, 0, 1, NULL),

    (N'Căn hộ góc 3PN Sky Oasis Ecopark tầng trung ngắm trọn vịnh đảo',
     N'Cần bán căn hộ 3 phòng ngủ 2 WC toà S2 Sky Oasis Ecopark. Ban công Đông Nam mát mẻ quanh năm, view trực diện biệt thự đảo Marina và hồ Thiên Nga.',
     4100000000, 88, 3, 2, N'KĐT Ecopark, Phụng Công', N'Hưng Yên', N'Văn Giang', N'Đông Nam', 'Sale', 'Pending', @AgentId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=1200&q=80',
     0, 0, 1, NULL),

    (N'Bán đất nền dự án KĐT Sinh Thái Hòa Xuân Cẩm Lệ đường 7.5m',
     N'Lô đất đẹp đường 7.5m vỉa hè mỗi bên 4m, khu dân cư đông đúc trí thức cao. Gần cầu Hòa Xuân, cách siêu thị Metro chỉ 5 phút đi xe.',
     3450000000, 100, 0, 0, N'Đường Mẹ Thứ, KĐT Nam Hòa Xuân', N'Đà Nẵng', N'Cẩm Lệ', N'Nam', 'Sale', 'Pending', @SellerId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1500382017468-9049fed747ef?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1524813686514-a57563d77d61?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1200&q=80',
     0, 0, 1, NULL),

    (N'Cho thuê Mặt bằng kinh doanh Phố Huế Hai Bà Trưng vị trí đắc địa',
     N'Mặt tiền 6m vỉa hè rộng 5m để được nhiều xe máy. Phù hợp làm showroom thời trang, nha khoa, văn phòng đại diện hoặc ngân hàng. Hợp đồng dài hạn.',
     45000000, 90, 1, 2, N'182 Phố Huế, Ngô Thì Nhậm', N'Hà Nội', N'Hai Bà Trưng', N'Tây', 'Rent', 'Pending', @LandlordId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1497366216548-37526070297c?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1497215728101-856f4ea42174?auto=format&fit=crop&w=1200&q=80',
     0, 0, 1, NULL),

    (N'Bán căn hộ 1PN+1 Vinhomes Ocean Park Gia Lâm bao phí sang tên',
     N'Căn hộ 1 phòng ngủ cộng toà S2.05 Vinhomes Ocean Park. Đã có nội thất cơ bản liền tường, ban công Đông Bắc thoáng mát, gần trường học Vinschool.',
     2150000000, 48, 1, 1, N'KĐT Vinhomes Ocean Park, Đa Tốn', N'Hà Nội', N'Gia Lâm', N'Đông Bắc', 'Sale', 'Pending', @AgentId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?auto=format&fit=crop&w=1200&q=80',
     0, 0, 1, NULL),

    (N'Cho thuê Căn hộ Officetel Wilton Tower Bình Thạnh đầy đủ tiện ích',
     N'Cho thuê căn officetel vừa ở vừa làm văn phòng công ty. Diện tích 42m2 có sẵn máy lạnh, rèm cửa, bàn làm việc, bảo vệ 24/7 an ninh tuyệt đối.',
     13500000, 42, 1, 1, N'71/3 Nguyễn Văn Thương, Phường 25', N'Hồ Chí Minh', N'Bình Thạnh', N'Nam', 'Rent', 'Pending', @LandlordId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1497366216548-37526070297c?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1502005229762-ee1b2da97e06?auto=format&fit=crop&w=1200&q=80',
     0, 0, 1, NULL);
END

-- ĐỒNG BỘ CẬP NHẬT NHIỀU ẢNH THỰC TẾ CHO CÁC TIN HIỆN CÓ
UPDATE Listings 
SET ImageUrls = 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600566753376-12c8ab7fb75b?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600573472591-ee6b68d14c68?auto=format&fit=crop&w=1200&q=80'
WHERE Title LIKE N'%Riverside%';

UPDATE Listings 
SET ImageUrls = 'https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=1200&q=80'
WHERE Title LIKE N'%Landmark 81%';

UPDATE Listings 
SET ImageUrls = 'https://images.unsplash.com/photo-1512917774080-9991f1c4c750?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1512918728675-ed5a9ecdebfd?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1613490493576-7fde63acd811?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600607687920-4e2a09cf159d?auto=format&fit=crop&w=1200&q=80'
WHERE Title LIKE N'%Duplex Thảo Điền%';

UPDATE Listings 
SET ImageUrls = 'https://images.unsplash.com/photo-1568605117036-5fe5e7bab0b7?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1580587771525-78b9dba3b914?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1513694203232-719a280e022f?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1502005229762-ee1b2da97e06?auto=format&fit=crop&w=1200&q=80'
WHERE Title LIKE N'%Nguyễn Văn Linh%';

UPDATE Listings 
SET ImageUrls = 'https://images.unsplash.com/photo-1580587771525-78b9dba3b914?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1513694203232-719a280e022f?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1598928506311-c55ded91a20c?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1584622650111-993a426fbf0a?auto=format&fit=crop&w=1200&q=80'
WHERE Title LIKE N'%Cầu Giấy%';

UPDATE Listings 
SET ImageUrls = 'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1484154218962-a197022b5858?auto=format&fit=crop&w=1200&q=80'
WHERE Title LIKE N'%Smart City%';

UPDATE Listings 
SET ImageUrls = 'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1497366216548-37526070297c?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1497215728101-856f4ea42174?auto=format&fit=crop&w=1200&q=80'
WHERE Title LIKE N'%Shophouse chân đế%';

    (N'Biệt thự Đảo Ecopark Grand The Island ven hồ sinh thái',
     N'Tuyệt tác biệt thự ven hồ sinh thái Ecopark Grand. Diện tích 300m2, thiết kế 3 tầng phong cách Châu Âu hiện đại, sân vườn riêng biệt, bể bơi vô cực ngắm hồ. Sổ đỏ lâu dài.',
     32000000000, 300, 5, 6, N'Khu đô thị Ecopark, Phụng Công', N'Hưng Yên', N'Văn Giang', N'Đông Nam', 'Sale', 'Approved', @SellerId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1613490493576-7fde63acd811?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1600566753376-12c8ab7fb75b?auto=format&fit=crop&w=1200&q=80',
     185, 1, 1, DATEADD(DAY, 5, GETDATE())),

    (N'Căn hộ The Metropole Thủ Thiêm 2PN view trực diện sông Sài Gòn',
     N'Cần bán căn hộ cao cấp The Metropole Thủ Thiêm phân khu The Opera Residence. Tầng cao view Panorama trực diện sông Sài Gòn và Bitexco, nội thất bàn giao ngoại nhập cao cấp.',
     11500000000, 85, 2, 2, N'Khu đô thị mới Thủ Thiêm, An Khánh', N'Hồ Chí Minh', N'Thành phố Thủ Đức', N'Tây Nam', 'Sale', 'Approved', @AgentId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1512918728675-ed5a9ecdebfd?auto=format&fit=crop&w=1200&q=80',
     142, 1, 1, DATEADD(DAY, 4, GETDATE())),

    (N'Nhà phố liền kề Gamuda Gardens Hoàng Mai đầy đủ nội thất gỗ lim',
     N'Nhà xây 4 tầng chắc chắn, diện tích đất 118m2, mặt tiền 5.5m. Đường trước nhà 13m ô tô tránh thoải mái, tiện ích club house, bể bơi Olympic, trường quốc tế SIS.',
     18500000000, 118, 4, 5, N'KĐT Gamuda Gardens, Yên Sở', N'Hà Nội', N'Hoàng Mai', N'Nam', 'Sale', 'Approved', @SellerId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1580587771525-78b9dba3b914?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1513694203232-719a280e022f?auto=format&fit=crop&w=1200&q=80',
     98, 0, 1, NULL),

    (N'Căn hộ Masteri Centre Point Vinhomes Grand Park công viên 36ha',
     N'Căn 2 phòng ngủ 72m2 tại Masteri Centre Point phân khu compound cao cấp nhất đại đô thị. View trọn đại công viên 36ha và biển hồ cát trắng. Sắp bàn giao nhận nhà ngay.',
     3850000000, 72, 2, 2, N'Khu đô thị Vinhomes Grand Park, Long Thạnh Mỹ', N'Hồ Chí Minh', N'Thành phố Thủ Đức', N'Đông', 'Sale', 'Approved', @AgentId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=1200&q=80',
     115, 0, 1, NULL),

    (N'Cho thuê Căn hộ Duplex DCapitale Trần Duy Hưng full nội thất Ý',
     N'Cho thuê căn Duplex thông tầng toà C6 DCapitale Trần Duy Hưng. Thiết kế sang trọng 3 phòng ngủ, kính Low-E full từ sàn tới trần, view hồ công viên Thanh Xuân. Phù hợp chuyên gia nước ngoài.',
     28000000, 120, 3, 3, N'119 Trần Duy Hưng, Trung Hòa', N'Hà Nội', N'Cầu Giấy', N'Đông Nam', 'Rent', 'Approved', @LandlordId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1502005229762-ee1b2da97e06?auto=format&fit=crop&w=1200&q=80',
     89, 1, 1, DATEADD(DAY, 3, GETDATE())),

    (N'Cho thuê Nhà xưởng / Kho bãi KCN VSIP 1 Thuận An PCCC chuẩn quốc tế',
     N'Cho thuê nhà xưởng tiêu chuẩn công nghiệp 1.500m2 tại KCN VSIP 1 Bình Dương. Trạm biến áp riêng 400kVA, trần cao 9m, sàn epoxy tải trọng 5 tấn/m2, hệ thống PCCC tự động nghiệm thu đầy đủ.',
     65000000, 1500, 0, 4, N'Đại lộ Hữu Nghị, KCN VSIP 1, An Phú', N'Bình Dương', N'Thuận An', N'Bắc', 'Rent', 'Approved', @LandlordId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1587293852726-70cdb56c2866?auto=format&fit=crop&w=1200&q=80',
     74, 0, 1, NULL);

    -- 6 TIN CHỜ KIỂM DUYỆT (PENDING) DÀNH CHO ADMIN
    INSERT INTO Listings (Title, Description, Price, Area, Bedrooms, Bathrooms, Address, City, District, Direction, Type, Status, OwnerId, CreatedAt, UpdatedAt, ImageUrls, ViewCount, IsVip, IsVerified, PriorityTime)
    VALUES 
    (N'Bán nhà riêng ngõ 3 tầng phố Thái Hà Đống Đa ngõ thông thoáng',
     N'Nhà xây khung cột bê tông 3 tầng kiên cố, ngõ 2.5m thông ra Hoàng Cầu, gần các trường đại học lớn. Sổ đỏ vuông vắn cất két sẵn sàng giao dịch.',
     6200000000, 45, 3, 3, N'Ngõ 117 Thái Hà, Trung Liệt', N'Hà Nội', N'Đống Đa', N'Đông', 'Sale', 'Pending', @SellerId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1568605117036-5fe5e7bab0b7?auto=format&fit=crop&w=1200&q=80,https://images.unsplash.com/photo-1512917774080-9991f1c4c750?auto=format&fit=crop&w=1200&q=80',
     0, 0, 1, NULL),

    (N'Căn hộ góc 3PN Sky Oasis Ecopark tầng trung ngắm trọn vịnh đảo',
     N'Cần bán căn hộ 3 phòng ngủ 2 WC toà S2 Sky Oasis Ecopark. Ban công Đông Nam mát mẻ quanh năm, view trực diện biệt thự đảo Marina và hồ Thiên Nga.',
     4100000000, 88, 3, 2, N'KĐT Ecopark, Phụng Công', N'Hưng Yên', N'Văn Giang', N'Đông Nam', 'Sale', 'Pending', @AgentId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?auto=format&fit=crop&w=1200&q=80',
     0, 0, 1, NULL),

    (N'Bán đất nền dự án KĐT Sinh Thái Hòa Xuân Cẩm Lệ đường 7.5m',
     N'Lô đất đẹp đường 7.5m vỉa hè mỗi bên 4m, khu dân cư đông đúc trí thức cao. Gần cầu Hòa Xuân, cách siêu thị Metro chỉ 5 phút đi xe.',
     3450000000, 100, 0, 0, N'Đường Mẹ Thứ, KĐT Nam Hòa Xuân', N'Đà Nẵng', N'Cẩm Lệ', N'Nam', 'Sale', 'Pending', @SellerId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1500382017468-9049fed747ef?auto=format&fit=crop&w=1200&q=80',
     0, 0, 1, NULL),

    (N'Cho thuê Mặt bằng kinh doanh Phố Huế Hai Bà Trưng vị trí đắc địa',
     N'Mặt tiền 6m vỉa hè rộng 5m để được nhiều xe máy. Phù hợp làm showroom thời trang, nha khoa, văn phòng đại diện hoặc ngân hàng. Hợp đồng dài hạn.',
     45000000, 90, 1, 2, N'182 Phố Huế, Ngô Thì Nhậm', N'Hà Nội', N'Hai Bà Trưng', N'Tây', 'Rent', 'Pending', @LandlordId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=1200&q=80',
     0, 0, 1, NULL),

    (N'Bán căn hộ 1PN+1 Vinhomes Ocean Park Gia Lâm bao phí sang tên',
     N'Căn hộ 1 phòng ngủ cộng toà S2.05 Vinhomes Ocean Park. Đã có nội thất cơ bản liền tường, ban công Đông Bắc thoáng mát, gần trường học Vinschool.',
     2150000000, 48, 1, 1, N'KĐT Vinhomes Ocean Park, Đa Tốn', N'Hà Nội', N'Gia Lâm', N'Đông Bắc', 'Sale', 'Pending', @AgentId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80',
     0, 0, 1, NULL),

    (N'Cho thuê Căn hộ Officetel Wilton Tower Bình Thạnh đầy đủ tiện ích',
     N'Cho thuê căn officetel vừa ở vừa làm văn phòng công ty. Diện tích 42m2 có sẵn máy lạnh, rèm cửa, bàn làm việc, bảo vệ 24/7 an ninh tuyệt đối.',
     13500000, 42, 1, 1, N'71/3 Nguyễn Văn Thương, Phường 25', N'Hồ Chí Minh', N'Bình Thạnh', N'Nam', 'Rent', 'Pending', @LandlordId, GETDATE(), GETDATE(),
     'https://images.unsplash.com/photo-1497366216548-37526070297c?auto=format&fit=crop&w=1200&q=80',
     0, 0, 1, NULL);
END

-- 7. DỮ LIỆU MẪU CHO LEADS (CRM KHÁCH HÀNG & LỊCH XEM NHÀ & PHẢN HỒI)
DECLARE @FirstListingId INT = (SELECT TOP 1 Id FROM Listings WHERE Status = 'Approved' ORDER BY Id ASC);
DECLARE @SecondListingId INT = (SELECT TOP 1 Id FROM Listings WHERE Status = 'Approved' ORDER BY Id DESC);
IF @FirstListingId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Leads)
BEGIN
    INSERT INTO Leads (ListingId, OwnerId, CustomerName, CustomerPhone, CustomerEmail, Notes, Status, ResponseMessage, RespondedAt, CreatedAt)
    VALUES 
    -- Dành cho Người bán (OwnerId = @SellerId)
    (@FirstListingId, @SellerId, N'Nguyễn Văn Hùng', '0912888999', 'hung.nv@gmail.com', N'Tôi muốn hẹn xem thực tế biệt thự đảo Ecopark vào sáng thứ 7 tuần này lúc 9h30, gia đình có xe riêng qua xem.', 'New', NULL, NULL, GETDATE()),
    (@FirstListingId, @SellerId, N'Trần Đình Trọng', '0977222333', 'trong.tran@vietcombank.com.vn', N'Tư vấn giúp tôi thủ tục vay ngân hàng 50% giá trị căn biệt thự này, gửi sổ đỏ qua Zalo trước giúp tôi.', 'Contacted', NULL, NULL, DATEADD(HOUR, -3, GETDATE())),

    -- Dành cho Chủ nhà cho thuê (OwnerId = @LandlordId)
    (@SecondListingId, @LandlordId, N'Michael Brown', '0903111222', 'michael.brown@expats-vn.com', N'Hi, I am looking to rent this duplex penthouse for my family for 2 years starting next month. Can we view tomorrow?', 'New', NULL, NULL, DATEADD(HOUR, -6, GETDATE())),
    (@SecondListingId, @LandlordId, N'Đỗ Kim Ngân', '0988555666', 'kimngan.media@gmail.com', N'Tôi thuê nhà làm văn phòng công ty truyền thông (khoảng 8 người), chủ nhà có xuất hóa đơn VAT được không?', 'Contacted', NULL, NULL, DATEADD(DAY, -1, GETDATE())),

    -- Dành cho Môi giới (OwnerId = @AgentId)
    (@FirstListingId, @AgentId, N'Phan Thanh Sơn', '0938444555', 'son.pt@novagroup.vn', N'Anh Sơn quan tâm căn Landmark 81 2PN, cần chốt sớm trước tết để làm nội thất đón năm mới.', 'New', NULL, NULL, DATEADD(DAY, -1, GETDATE())),
    (@SecondListingId, @AgentId, N'Lê Hồng Nhung', '0969888777', 'nhung.lehong@gmail.com', N'Khách muốn đàm phán giá bớt lộc và thương lượng lại phí môi giới sang tên. Hẹn gặp tại sảnh toà nhà.', 'Completed', NULL, NULL, DATEADD(DAY, -2, GETDATE())),

    -- Dành cho Người mua (buyer@batdongsan.vn) đã nhận phản hồi
    (@FirstListingId, @SellerId, N'Nguyễn Văn Mua', '0988111222', 'buyer@batdongsan.vn', N'Tôi muốn hẹn xem thực tế biệt thự đảo Ecopark vào sáng thứ 7 tuần này lúc 9h30, gia đình có xe riêng qua xem.', 'VIEWING', N'Chào anh/chị, tôi là chủ nhà đã nhận được lịch hẹn. Sáng thứ 7 lúc 9h30 tôi sẽ đón gia đình anh/chị tại sảnh Clubhouse đảo Ecopark nhé!', DATEADD(HOUR, -2, GETDATE()), DATEADD(HOUR, -6, GETDATE())),
    (@SecondListingId, @AgentId, N'Nguyễn Văn Mua', '0988111222', 'buyer@batdongsan.vn', N'Tư vấn giúp tôi thủ tục vay ngân hàng 50% giá trị căn hộ này và tiến độ thanh toán.', 'CONSULTING', N'Chào anh/chị, chuyên viên môi giới đã gửi bảng tính chi tiết lãi suất ngân hàng và hồ sơ pháp lý qua Zalo/Email cho anh/chị rồi nhé.', DATEADD(HOUR, -3, GETDATE()), DATEADD(HOUR, -8, GETDATE())),

    -- Dành cho Người thuê (tenant@batdongsan.vn) đã nhận phản hồi
    (@SecondListingId, @LandlordId, N'Vũ Khách Thuê', '0903222333', 'tenant@batdongsan.vn', N'Gia đình tôi muốn thuê dài hạn 2 năm, chiều mai 14h có thể xem nhà được không?', 'VIEWING', N'Chủ nhà đồng ý lịch hẹn lúc 14h chiều mai. Lễ tân tòa nhà sẽ mở thang máy hỗ trợ bạn lên xem trực tiếp nhé.', DATEADD(HOUR, -1, GETDATE()), DATEADD(HOUR, -5, GETDATE())),
    (@FirstListingId, @LandlordId, N'Vũ Khách Thuê', '0903222333', 'tenant@batdongsan.vn', N'Bên mình thuê làm kho chứa linh kiện điện tử, chủ nhà có xuất hóa đơn VAT không?', 'CONSULTING', N'Xưởng tiêu chuẩn công nghiệp đầy đủ pháp lý, bên mình xuất hóa đơn VAT điện tử hàng tháng và ký hợp đồng công chứng.', DATEADD(HOUR, -2, GETDATE()), DATEADD(HOUR, -7, GETDATE()));
END

-- 8. DỮ LIỆU MẪU CHO REPORTS (BÁO CÁO VI PHẠM DÀNH CHO ADMIN)
IF @FirstListingId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reports)
BEGIN
    INSERT INTO Reports (ListingId, Reason, Details, ReporterName, ReporterPhone, Status, CreatedAt)
    VALUES 
    (@FirstListingId, N'Sai lệch giá bán thực tế', N'Tôi có liên hệ người đăng nhưng báo giá thực tế chênh hơn 2 tỷ so với giá niêm yết trên web.', N'Hoàng Tuấn Anh', '0912345678', 'Pending', GETDATE()),
    (@SecondListingId, N'Hình ảnh không đúng thực tế', N'Hình ảnh đăng là căn góc toà The Landmark nhưng địa chỉ thực tế khi tới xem lại ở toà Park 2.', N'Trịnh Mai Phương', '0987654321', 'Pending', DATEADD(HOUR, -2, GETDATE())),
    (@FirstListingId, N'Bất động sản đã cho thuê nhưng không gỡ tin', N'Căn hộ đã có khách ký hợp đồng thuê tuần trước nhưng chủ vẫn để tin đăng gây nhầm lẫn.', N'Vũ Quang Huy', '0934567890', 'Pending', DATEADD(HOUR, -5, GETDATE())),
    (@SecondListingId, N'Địa chỉ bất động sản không chính xác', N'Số nhà trên đường bị ghi sai, thực tế căn nhà nằm sâu trong ngõ hẹp chứ không phải mặt tiền như miêu tả.', N'Lê Thị Bích', '0971239876', 'Pending', DATEADD(DAY, -1, GETDATE())),
    (@FirstListingId, N'Tin đăng trùng lặp nhiều lần', N'Tin bất động sản này bị người đăng gửi liên tục 3 lần trong ngày hôm qua làm loãng trang tìm kiếm.', N'Phạm Đình Trọng', '0908765432', 'Pending', DATEADD(DAY, -1, GETDATE())),
    (@SecondListingId, N'Thông tin pháp lý chưa rõ ràng', N'Người bán báo căn hộ đang tranh chấp hợp đồng mua bán với chủ đầu tư chưa giải quyết xong.', N'Đặng Thu Thảo', '0945671234', 'Pending', DATEADD(DAY, -2, GETDATE()));
END
GO

-- 9. DỮ LIỆU MẪU HỒ SƠ KYC CHỜ DUYỆT (PENDING) DÀNH CHO ADMIN
DECLARE @BuyerUser INT = (SELECT TOP 1 Id FROM Users WHERE Email = 'buyer@batdongsan.vn');
DECLARE @TenantUser INT = (SELECT TOP 1 Id FROM Users WHERE Email = 'tenant@batdongsan.vn');

IF @BuyerUser IS NOT NULL AND NOT EXISTS (SELECT 1 FROM KycRequests WHERE UserId = @BuyerUser)
BEGIN
    INSERT INTO KycRequests (UserId, FullName, Phone, IdCardNumber, IdCardFront, IdCardBack, TaxCode, TaxType, RequestedRole, BrokerLicenseNumber, BrokerLicenseImage, Status, CreatedAt)
    VALUES 
    (@BuyerUser, N'Nguyễn Văn Mua (Chính chủ đăng ký)', '0988111222', '001201008899', 
     'https://images.unsplash.com/photo-1544717305-2782549b5136?auto=format&fit=crop&w=600&q=80',
     'https://images.unsplash.com/photo-1544717305-2782549b5136?auto=format&fit=crop&w=600&q=80',
     '8401928374', N'Cá nhân', 'Seller', NULL, NULL, 'PENDING', GETDATE());

    UPDATE Users SET KycStatus = 'Pending', PendingRoles = 'Seller', Phone = '0988111222', PhoneVerified = 1,
                     IdCardNumber = '001201008899', TaxCode = '8401928374', TaxType = N'Cá nhân'
    WHERE Id = @BuyerUser;
END

IF @TenantUser IS NOT NULL AND NOT EXISTS (SELECT 1 FROM KycRequests WHERE UserId = @TenantUser)
BEGIN
    INSERT INTO KycRequests (UserId, FullName, Phone, IdCardNumber, IdCardFront, IdCardBack, TaxCode, TaxType, RequestedRole, BrokerLicenseNumber, BrokerLicenseImage, Status, CreatedAt)
    VALUES 
    (@TenantUser, N'Vũ Khách Thuê (Đăng ký Môi giới)', '0903222333', '079201019988',
     'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=600&q=80',
     'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=600&q=80',
     '0312987654', N'Doanh nghiệp / Hộ kinh doanh', 'Agent', 
     'BĐS-HN-2024-8899', 'https://images.unsplash.com/photo-1450133064473-71024230f91b?auto=format&fit=crop&w=600&q=80',
     'PENDING', DATEADD(HOUR, -2, GETDATE()));
END
GO

PRINT N'====================================================================';
PRINT N'>>> CẬP NHẬT DATABASE THÀNH CÔNG VÀ NẠP ĐẦY ĐỦ DATA 100%!';
PRINT N'====================================================================';
GO
