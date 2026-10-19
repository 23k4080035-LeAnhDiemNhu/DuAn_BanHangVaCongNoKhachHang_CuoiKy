-- =========================================================================================
-- FILE: 01_Safe_CreateSchema.sql
-- DỰ ÁN CAPSTONE: HỆ THỐNG QUẢN LÝ BÁN HÀNG VÀ CÔNG NỢ KHÁCH HÀNG (ECO2415)
-- MÔ TẢ: Khởi tạo schema 6 bảng nghiệp vụ an toàn (Idempotent), chuẩn 3NF.
-- QUY TẮC AN TOÀN TUYỆT ĐỐI:
--   1. KHÔNG chứa lệnh DROP TABLE, DELETE, TRUNCATE hay RESEED.
--   2. KHÔNG hardcode tên CSDL; tự kiểm tra và từ chối chạy trên database hệ thống.
--   3. Nếu bảng đã tồn tại, kiểm tra đầy đủ các cột và ràng buộc thiết yếu.
-- =========================================================================================

-- 1. CHỐNG CHẠY NHẦM TRÊN CSDL HỆ THỐNG
DECLARE @CurrentDb NVARCHAR(128) = DB_NAME();
IF @CurrentDb IN ('master', 'model', 'msdb', 'tempdb')
BEGIN
    RAISERROR(N'LỖI AN TOÀN: Không được thực thi script trên CSDL hệ thống [%s]! Vui lòng chọn (USE) đúng database nghiệp vụ trước khi chạy.', 16, 1, @CurrentDb);
    SET NOEXEC ON;
END
GO

DECLARE @initMsg NVARCHAR(200) = N'==> [01_Safe_CreateSchema] Bắt đầu khởi tạo schema trên CSDL: ' + DB_NAME();
PRINT @initMsg;
GO

-- =========================================================================================
-- BẢNG 1: KhachHangs - Quản lý hồ sơ khách hàng, Hạn mức tín dụng & Số ngày nợ (Credit terms)
-- =========================================================================================
IF OBJECT_ID('dbo.KhachHangs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.KhachHangs
    (
        KhachHangId     INT IDENTITY(1,1)   NOT NULL,
        MaKhachHang     NVARCHAR(20)        NOT NULL,
        TenKhachHang    NVARCHAR(150)       NOT NULL,
        SoDienThoai     NVARCHAR(20)        NULL,
        Email           NVARCHAR(100)       NULL,
        DiaChi          NVARCHAR(250)       NULL,
        HanMucCongNo    DECIMAL(18,2)       NOT NULL CONSTRAINT DF_KhachHangs_HanMucCongNo DEFAULT (0),
        SoNgayNo        INT                 NOT NULL CONSTRAINT DF_KhachHangs_SoNgayNo DEFAULT (0),
        NgayTao         DATETIME            NOT NULL CONSTRAINT DF_KhachHangs_NgayTao DEFAULT (GETDATE()),
        TrangThai       BIT                 NOT NULL CONSTRAINT DF_KhachHangs_TrangThai DEFAULT (1),

        CONSTRAINT PK_KhachHangs PRIMARY KEY CLUSTERED (KhachHangId),
        CONSTRAINT UQ_KhachHangs_MaKhachHang UNIQUE (MaKhachHang),
        CONSTRAINT CK_KhachHangs_HanMucCongNo CHECK (HanMucCongNo >= 0),
        CONSTRAINT CK_KhachHangs_SoNgayNo_NonNegative CHECK (SoNgayNo >= 0)
    );
    PRINT N'  -> Đã tạo bảng dbo.KhachHangs thành công.';
END
ELSE
BEGIN
    PRINT N'  -> Bảng dbo.KhachHangs đã tồn tại. Đang kiểm tra cấu trúc...';

    -- Kiểm tra và bổ sung cột SoNgayNo nếu thiếu
    IF COL_LENGTH('dbo.KhachHangs', 'SoNgayNo') IS NULL
    BEGIN
        ALTER TABLE dbo.KhachHangs ADD SoNgayNo INT NOT NULL CONSTRAINT DF_KhachHangs_SoNgayNo DEFAULT (0);
        PRINT N'    + Đã bổ sung cột SoNgayNo vào dbo.KhachHangs.';
    END

    -- Đảm bảo có ràng buộc CHECK SoNgayNo >= 0
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name IN ('CK_KhachHangs_SoNgayNo_NonNegative', 'CK_KhachHangs_SoNgayNo') AND parent_object_id = OBJECT_ID('dbo.KhachHangs'))
    BEGIN
        ALTER TABLE dbo.KhachHangs ADD CONSTRAINT CK_KhachHangs_SoNgayNo_NonNegative CHECK (SoNgayNo >= 0);
        PRINT N'    + Đã bổ sung ràng buộc CK_KhachHangs_SoNgayNo_NonNegative.';
    END
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_KhachHangs_TenKhachHang' AND object_id = OBJECT_ID('dbo.KhachHangs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_KhachHangs_TenKhachHang ON dbo.KhachHangs(TenKhachHang);
END
GO

-- =========================================================================================
-- BẢNG 2: SanPhams - Danh mục hàng hóa & Đơn giá niêm yết
-- =========================================================================================
IF OBJECT_ID('dbo.SanPhams', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SanPhams
    (
        SanPhamId       INT IDENTITY(1,1)   NOT NULL,
        MaSanPham       NVARCHAR(20)        NOT NULL,
        TenSanPham      NVARCHAR(200)       NOT NULL,
        DonViTinh       NVARCHAR(50)        NOT NULL CONSTRAINT DF_SanPhams_DonViTinh DEFAULT (N'Cái'),
        DonGia          DECIMAL(18,2)       NOT NULL CONSTRAINT DF_SanPhams_DonGia DEFAULT (0),
        SoLuongTon      INT                 NOT NULL CONSTRAINT DF_SanPhams_SoLuongTon DEFAULT (0),
        MoTa            NVARCHAR(500)       NULL,
        HinhAnh         NVARCHAR(300)       NULL,
        NgayTao         DATETIME            NOT NULL CONSTRAINT DF_SanPhams_NgayTao DEFAULT (GETDATE()),
        TrangThai       BIT                 NOT NULL CONSTRAINT DF_SanPhams_TrangThai DEFAULT (1),

        CONSTRAINT PK_SanPhams PRIMARY KEY CLUSTERED (SanPhamId),
        CONSTRAINT UQ_SanPhams_MaSanPham UNIQUE (MaSanPham),
        CONSTRAINT CK_SanPhams_DonGia CHECK (DonGia >= 0),
        CONSTRAINT CK_SanPhams_SoLuongTon CHECK (SoLuongTon >= 0)
    );
    PRINT N'  -> Đã tạo bảng dbo.SanPhams thành công.';
END
ELSE
BEGIN
    PRINT N'  -> Bảng dbo.SanPhams đã tồn tại.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SanPhams_TenSanPham' AND object_id = OBJECT_ID('dbo.SanPhams'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SanPhams_TenSanPham ON dbo.SanPhams(TenSanPham);
END
GO

-- =========================================================================================
-- BẢNG 3: HoaDons - Hóa đơn bán chịu kiêm theo dõi công nợ chi tiết từng đơn
-- =========================================================================================
IF OBJECT_ID('dbo.HoaDons', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HoaDons
    (
        HoaDonId        INT IDENTITY(1,1)   NOT NULL,
        MaHoaDon        NVARCHAR(20)        NOT NULL,
        KhachHangId     INT                 NOT NULL,
        NgayLap         DATETIME            NOT NULL CONSTRAINT DF_HoaDons_NgayLap DEFAULT (GETDATE()),
        HanThanhToan    DATETIME            NOT NULL,
        TongTien        DECIMAL(18,2)       NOT NULL CONSTRAINT DF_HoaDons_TongTien DEFAULT (0),
        DaThanhToan     DECIMAL(18,2)       NOT NULL CONSTRAINT DF_HoaDons_DaThanhToan DEFAULT (0),
        
        -- Cột tính toán số nợ còn lại (PERSISTED để đánh chỉ mục tối ưu FIFO)
        ConLai          AS (TongTien - DaThanhToan) PERSISTED,
        
        TrangThai       INT                 NOT NULL CONSTRAINT DF_HoaDons_TrangThai DEFAULT (0), -- 0: Chưa TT, 1: TT 1 phần, 2: Đã TT xong
        GhiChu          NVARCHAR(500)       NULL,
        NgayTao         DATETIME            NOT NULL CONSTRAINT DF_HoaDons_NgayTao DEFAULT (GETDATE()),

        CONSTRAINT PK_HoaDons PRIMARY KEY CLUSTERED (HoaDonId),
        CONSTRAINT UQ_HoaDons_MaHoaDon UNIQUE (MaHoaDon),
        CONSTRAINT FK_HoaDons_KhachHangs FOREIGN KEY (KhachHangId) REFERENCES dbo.KhachHangs(KhachHangId),
        CONSTRAINT CK_HoaDons_TongTien CHECK (TongTien >= 0),
        CONSTRAINT CK_HoaDons_DaThanhToan CHECK (DaThanhToan >= 0 AND DaThanhToan <= TongTien),
        CONSTRAINT CK_HoaDons_HanThanhToan CHECK (HanThanhToan >= NgayLap),
        CONSTRAINT CK_HoaDons_TrangThai CHECK (TrangThai IN (0, 1, 2))
    );
    PRINT N'  -> Đã tạo bảng dbo.HoaDons thành công.';
END
ELSE
BEGIN
    PRINT N'  -> Bảng dbo.HoaDons đã tồn tại.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HoaDons_KhachHangId_TrangThai_NgayLap' AND object_id = OBJECT_ID('dbo.HoaDons'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_HoaDons_KhachHangId_TrangThai_NgayLap 
    ON dbo.HoaDons(KhachHangId, TrangThai, NgayLap, HoaDonId)
    INCLUDE (TongTien, DaThanhToan, ConLai);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HoaDons_HanThanhToan' AND object_id = OBJECT_ID('dbo.HoaDons'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_HoaDons_HanThanhToan 
    ON dbo.HoaDons(HanThanhToan)
    INCLUDE (KhachHangId, TongTien, DaThanhToan, ConLai, TrangThai);
END
GO

-- =========================================================================================
-- BẢNG 4: ChiTietHoaDons - Các dòng sản phẩm xuất bán thuộc hóa đơn (OrderLineItem)
-- =========================================================================================
IF OBJECT_ID('dbo.ChiTietHoaDons', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChiTietHoaDons
    (
        ChiTietHoaDonId INT IDENTITY(1,1)   NOT NULL,
        HoaDonId        INT                 NOT NULL,
        SanPhamId       INT                 NOT NULL,
        TenSanPham      NVARCHAR(200)       NOT NULL,
        DonViTinh       NVARCHAR(50)        NOT NULL,
        SoLuong         INT                 NOT NULL,
        DonGia          DECIMAL(18,2)       NOT NULL,
        
        -- Cột tính toán Thành tiền (PERSISTED)
        ThanhTien       AS (SoLuong * DonGia) PERSISTED,

        CONSTRAINT PK_ChiTietHoaDons PRIMARY KEY CLUSTERED (ChiTietHoaDonId),
        CONSTRAINT FK_ChiTietHoaDons_HoaDons FOREIGN KEY (HoaDonId) REFERENCES dbo.HoaDons(HoaDonId) ON DELETE CASCADE,
        CONSTRAINT FK_ChiTietHoaDons_SanPhams FOREIGN KEY (SanPhamId) REFERENCES dbo.SanPhams(SanPhamId),
        CONSTRAINT CK_ChiTietHoaDons_SoLuong CHECK (SoLuong > 0),
        CONSTRAINT CK_ChiTietHoaDons_DonGia CHECK (DonGia >= 0)
    );
    PRINT N'  -> Đã tạo bảng dbo.ChiTietHoaDons thành công.';
END
ELSE
BEGIN
    PRINT N'  -> Bảng dbo.ChiTietHoaDons đã tồn tại.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChiTietHoaDons_HoaDonId' AND object_id = OBJECT_ID('dbo.ChiTietHoaDons'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ChiTietHoaDons_HoaDonId ON dbo.ChiTietHoaDons(HoaDonId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChiTietHoaDons_SanPhamId' AND object_id = OBJECT_ID('dbo.ChiTietHoaDons'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ChiTietHoaDons_SanPhamId ON dbo.ChiTietHoaDons(SanPhamId);
END
GO

-- =========================================================================================
-- BẢNG 5: ThanhToans - Phiếu thu tiền từ khách hàng
-- =========================================================================================
IF OBJECT_ID('dbo.ThanhToans', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ThanhToans
    (
        ThanhToanId     INT IDENTITY(1,1)   NOT NULL,
        MaPhieuThu      NVARCHAR(20)        NOT NULL,
        KhachHangId     INT                 NOT NULL,
        NgayThanhToan   DATETIME            NOT NULL CONSTRAINT DF_ThanhToans_NgayThanhToan DEFAULT (GETDATE()),
        SoTien          DECIMAL(18,2)       NOT NULL,
        PhuongThuc      NVARCHAR(50)        NOT NULL CONSTRAINT DF_ThanhToans_PhuongThuc DEFAULT (N'ChuyenKhoan'),
        GhiChu          NVARCHAR(500)       NULL,
        NgayTao         DATETIME            NOT NULL CONSTRAINT DF_ThanhToans_NgayTao DEFAULT (GETDATE()),

        CONSTRAINT PK_ThanhToans PRIMARY KEY CLUSTERED (ThanhToanId),
        CONSTRAINT UQ_ThanhToans_MaPhieuThu UNIQUE (MaPhieuThu),
        CONSTRAINT FK_ThanhToans_KhachHangs FOREIGN KEY (KhachHangId) REFERENCES dbo.KhachHangs(KhachHangId),
        CONSTRAINT CK_ThanhToans_SoTien CHECK (SoTien > 0)
    );
    PRINT N'  -> Đã tạo bảng dbo.ThanhToans thành công.';
END
ELSE
BEGIN
    PRINT N'  -> Bảng dbo.ThanhToans đã tồn tại.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ThanhToans_KhachHangId_NgayThanhToan' AND object_id = OBJECT_ID('dbo.ThanhToans'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ThanhToans_KhachHangId_NgayThanhToan ON dbo.ThanhToans(KhachHangId, NgayThanhToan);
END
GO

-- =========================================================================================
-- BẢNG 6: PhanBoThanhToans - Chi tiết cấn trừ nợ của Phiếu thu vào từng Hóa đơn (Thuật toán FIFO)
-- =========================================================================================
IF OBJECT_ID('dbo.PhanBoThanhToans', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PhanBoThanhToans
    (
        PhanBoThanhToanId INT IDENTITY(1,1) NOT NULL,
        ThanhToanId       INT               NOT NULL,
        HoaDonId          INT               NOT NULL,
        SoTienPhanBo      DECIMAL(18,2)     NOT NULL,
        NgayPhanBo        DATETIME          NOT NULL CONSTRAINT DF_PhanBoThanhToans_NgayPhanBo DEFAULT (GETDATE()),

        CONSTRAINT PK_PhanBoThanhToans PRIMARY KEY CLUSTERED (PhanBoThanhToanId),
        CONSTRAINT FK_PhanBoThanhToans_ThanhToans FOREIGN KEY (ThanhToanId) REFERENCES dbo.ThanhToans(ThanhToanId) ON DELETE CASCADE,
        CONSTRAINT FK_PhanBoThanhToans_HoaDons FOREIGN KEY (HoaDonId) REFERENCES dbo.HoaDons(HoaDonId),
        CONSTRAINT CK_PhanBoThanhToans_SoTienPhanBo CHECK (SoTienPhanBo > 0)
    );
    PRINT N'  -> Đã tạo bảng dbo.PhanBoThanhToans thành công.';
END
ELSE
BEGIN
    PRINT N'  -> Bảng dbo.PhanBoThanhToans đã tồn tại.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PhanBoThanhToans_ThanhToanId' AND object_id = OBJECT_ID('dbo.PhanBoThanhToans'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PhanBoThanhToans_ThanhToanId ON dbo.PhanBoThanhToans(ThanhToanId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PhanBoThanhToans_HoaDonId' AND object_id = OBJECT_ID('dbo.PhanBoThanhToans'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PhanBoThanhToans_HoaDonId ON dbo.PhanBoThanhToans(HoaDonId);
END
GO

PRINT N'==> [01_Safe_CreateSchema] HOÀN TẤT THIẾT LẬP 6 BẢNG NGHIỆP VỤ AN TOÀN!';
GO
