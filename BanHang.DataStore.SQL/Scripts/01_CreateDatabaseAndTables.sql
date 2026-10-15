-- =========================================================================================
-- FILE 1: 01_CreateDatabaseAndTables.sql
-- DỰ ÁN CAPSTONE: HỆ THỐNG QUẢN LÝ BÁN HÀNG VÀ CÔNG NỢ KHÁCH HÀNG
-- TÁC GIẢ: Sinh viên ngành Tin học Kinh tế - ECO2415
-- MÔ TẢ: Khởi tạo CSDL [QuanLyBanHangCongNo] và 6 bảng nghiệp vụ cốt lõi
-- CƠ CHẾ: Đầy đủ Khóa chính (PK), Khóa ngoại (FK), Ràng buộc (CHECK), Cột tính toán (COMPUTED)
--         và Chỉ mục (INDEX) phục vụ thuật toán FIFO và Báo cáo tuổi nợ 5 nhóm.
-- =========================================================================================

-- 1. TẠO CƠ SỞ DỮ LIỆU (NẾU CHƯA TỒN TẠI)
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'QuanLyBanHangCongNo')
BEGIN
    PRINT N'==> Đang tạo cơ sở dữ liệu [QuanLyBanHangCongNo]...';
    CREATE DATABASE [QuanLyBanHangCongNo];
END
ELSE
BEGIN
    PRINT N'==> Cơ sở dữ liệu [QuanLyBanHangCongNo] đã tồn tại.';
END
GO

-- 2. CHUYỂN NGỮ CẢNH SANG DATABASE QUANLYBANHANGCONGNO
USE [QuanLyBanHangCongNo];
GO

-- 3. XÓA CÁC BẢNG THEO THỨ TỰ NGƯỢC NẾU ĐÃ TỒN TẠI (ĐỂ CÓ THỂ CHẠY LẠI SCRIPT NHIỀU LẦN AN TOÀN)
IF OBJECT_ID('dbo.PhanBoThanhToans', 'U') IS NOT NULL DROP TABLE dbo.PhanBoThanhToans;
IF OBJECT_ID('dbo.ThanhToans', 'U') IS NOT NULL DROP TABLE dbo.ThanhToans;
IF OBJECT_ID('dbo.ChiTietHoaDons', 'U') IS NOT NULL DROP TABLE dbo.ChiTietHoaDons;
IF OBJECT_ID('dbo.HoaDons', 'U') IS NOT NULL DROP TABLE dbo.HoaDons;
IF OBJECT_ID('dbo.SanPhams', 'U') IS NOT NULL DROP TABLE dbo.SanPhams;
IF OBJECT_ID('dbo.KhachHangs', 'U') IS NOT NULL DROP TABLE dbo.KhachHangs;
GO

PRINT N'==> Đang tạo 6 bảng nghiệp vụ...';
GO

-- =========================================================================================
-- BẢNG 1: KhachHangs - Quản lý hồ sơ khách hàng & Hạn mức tín dụng
-- =========================================================================================
CREATE TABLE dbo.KhachHangs
(
    KhachHangId     INT IDENTITY(1,1)   NOT NULL,
    MaKhachHang     NVARCHAR(20)        NOT NULL,
    TenKhachHang    NVARCHAR(150)       NOT NULL,
    SoDienThoai     NVARCHAR(20)        NULL,
    Email           NVARCHAR(100)       NULL,
    DiaChi          NVARCHAR(250)       NULL,
    HanMucCongNo    DECIMAL(18,2)       NOT NULL CONSTRAINT DF_KhachHangs_HanMucCongNo DEFAULT (0),
    NgayTao         DATETIME            NOT NULL CONSTRAINT DF_KhachHangs_NgayTao DEFAULT (GETDATE()),
    TrangThai       BIT                 NOT NULL CONSTRAINT DF_KhachHangs_TrangThai DEFAULT (1), -- 1: Đang hoạt động, 0: Tạm khóa

    -- Ràng buộc
    CONSTRAINT PK_KhachHangs PRIMARY KEY CLUSTERED (KhachHangId),
    CONSTRAINT UQ_KhachHangs_MaKhachHang UNIQUE (MaKhachHang),
    CONSTRAINT CK_KhachHangs_HanMucCongNo CHECK (HanMucCongNo >= 0)
);
GO

-- Chỉ mục hỗ trợ tìm kiếm nhanh theo tên khách hàng
CREATE NONCLUSTERED INDEX IX_KhachHangs_TenKhachHang ON dbo.KhachHangs(TenKhachHang);
GO

-- =========================================================================================
-- BẢNG 2: SanPhams - Danh mục hàng hóa & Đơn giá bán niêm yết
-- Ghi chú: Chưa tích hợp nghiệp vụ trừ tồn kho tự động khi bán (theo phạm vi đã thống nhất)
-- =========================================================================================
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
    TrangThai       BIT                 NOT NULL CONSTRAINT DF_SanPhams_TrangThai DEFAULT (1), -- 1: Đang bán, 0: Ngừng bán

    -- Ràng buộc
    CONSTRAINT PK_SanPhams PRIMARY KEY CLUSTERED (SanPhamId),
    CONSTRAINT UQ_SanPhams_MaSanPham UNIQUE (MaSanPham),
    CONSTRAINT CK_SanPhams_DonGia CHECK (DonGia >= 0),
    CONSTRAINT CK_SanPhams_SoLuongTon CHECK (SoLuongTon >= 0)
);
GO

CREATE NONCLUSTERED INDEX IX_SanPhams_TenSanPham ON dbo.SanPhams(TenSanPham);
GO

-- =========================================================================================
-- BẢNG 3: HoaDons - Hóa đơn bán hàng kiêm theo dõi công nợ chi tiết từng đơn
-- =========================================================================================
CREATE TABLE dbo.HoaDons
(
    HoaDonId        INT IDENTITY(1,1)   NOT NULL,
    MaHoaDon        NVARCHAR(20)        NOT NULL,
    KhachHangId     INT                 NOT NULL,
    NgayLap         DATETIME            NOT NULL CONSTRAINT DF_HoaDons_NgayLap DEFAULT (GETDATE()),
    HanThanhToan    DATETIME            NOT NULL,
    TongTien        DECIMAL(18,2)       NOT NULL CONSTRAINT DF_HoaDons_TongTien DEFAULT (0),
    DaThanhToan     DECIMAL(18,2)       NOT NULL CONSTRAINT DF_HoaDons_DaThanhToan DEFAULT (0),
    
    -- Cột tính toán số nợ còn lại (PERSISTED để đánh chỉ mục và truy vấn siêu tốc)
    ConLai          AS (TongTien - DaThanhToan) PERSISTED,
    
    TrangThai       INT                 NOT NULL CONSTRAINT DF_HoaDons_TrangThai DEFAULT (0), -- 0: Chưa thanh toán, 1: Thanh toán một phần, 2: Đã thanh toán xong
    GhiChu          NVARCHAR(500)       NULL,
    NgayTao         DATETIME            NOT NULL CONSTRAINT DF_HoaDons_NgayTao DEFAULT (GETDATE()),

    -- Ràng buộc
    CONSTRAINT PK_HoaDons PRIMARY KEY CLUSTERED (HoaDonId),
    CONSTRAINT UQ_HoaDons_MaHoaDon UNIQUE (MaHoaDon),
    CONSTRAINT FK_HoaDons_KhachHangs FOREIGN KEY (KhachHangId) REFERENCES dbo.KhachHangs(KhachHangId),
    CONSTRAINT CK_HoaDons_TongTien CHECK (TongTien >= 0),
    CONSTRAINT CK_HoaDons_DaThanhToan CHECK (DaThanhToan >= 0 AND DaThanhToan <= TongTien), -- BẢO ĐẢM SỐ DƯ NỢ KHÔNG BAO GIỜ ÂM
    CONSTRAINT CK_HoaDons_HanThanhToan CHECK (HanThanhToan >= NgayLap),
    CONSTRAINT CK_HoaDons_TrangThai CHECK (TrangThai IN (0, 1, 2))
);
GO

-- Chỉ mục 1: Tối ưu hóa thuật toán FIFO (lấy nhanh các hóa đơn còn nợ của một khách hàng theo ngày lập tăng dần)
CREATE NONCLUSTERED INDEX IX_HoaDons_KhachHangId_TrangThai_NgayLap 
ON dbo.HoaDons(KhachHangId, TrangThai, NgayLap, HoaDonId)
INCLUDE (TongTien, DaThanhToan, ConLai);
GO

-- Chỉ mục 2: Tối ưu hóa truy vấn Báo cáo tuổi nợ 5 nhóm theo Hạn thanh toán
CREATE NONCLUSTERED INDEX IX_HoaDons_HanThanhToan 
ON dbo.HoaDons(HanThanhToan)
INCLUDE (KhachHangId, TongTien, DaThanhToan, ConLai, TrangThai);
GO

-- =========================================================================================
-- BẢNG 4: ChiTietHoaDons - Các dòng sản phẩm xuất bán thuộc hóa đơn
-- =========================================================================================
CREATE TABLE dbo.ChiTietHoaDons
(
    ChiTietHoaDonId INT IDENTITY(1,1)   NOT NULL,
    HoaDonId        INT                 NOT NULL,
    SanPhamId       INT                 NOT NULL,
    TenSanPham      NVARCHAR(200)       NOT NULL, -- Lưu snapshot tên tại thời điểm bán
    DonViTinh       NVARCHAR(50)        NOT NULL, -- Lưu snapshot đơn vị tính
    SoLuong         INT                 NOT NULL,
    DonGia          DECIMAL(18,2)       NOT NULL,
    
    -- Cột tính toán Thành tiền của dòng hàng (PERSISTED để bảo đảm dữ liệu luôn đúng phép nhân)
    ThanhTien       AS (SoLuong * DonGia) PERSISTED,

    -- Ràng buộc
    CONSTRAINT PK_ChiTietHoaDons PRIMARY KEY CLUSTERED (ChiTietHoaDonId),
    CONSTRAINT FK_ChiTietHoaDons_HoaDons FOREIGN KEY (HoaDonId) REFERENCES dbo.HoaDons(HoaDonId) ON DELETE CASCADE,
    CONSTRAINT FK_ChiTietHoaDons_SanPhams FOREIGN KEY (SanPhamId) REFERENCES dbo.SanPhams(SanPhamId),
    CONSTRAINT CK_ChiTietHoaDons_SoLuong CHECK (SoLuong > 0),
    CONSTRAINT CK_ChiTietHoaDons_DonGia CHECK (DonGia >= 0)
);
GO

CREATE NONCLUSTERED INDEX IX_ChiTietHoaDons_HoaDonId ON dbo.ChiTietHoaDons(HoaDonId);
CREATE NONCLUSTERED INDEX IX_ChiTietHoaDons_SanPhamId ON dbo.ChiTietHoaDons(SanPhamId);
GO

-- =========================================================================================
-- BẢNG 5: ThanhToans - Chứng từ / Phiếu thu tiền từ khách hàng
-- =========================================================================================
CREATE TABLE dbo.ThanhToans
(
    ThanhToanId     INT IDENTITY(1,1)   NOT NULL,
    MaPhieuThu      NVARCHAR(20)        NOT NULL,
    KhachHangId     INT                 NOT NULL,
    NgayThanhToan   DATETIME            NOT NULL CONSTRAINT DF_ThanhToans_NgayThanhToan DEFAULT (GETDATE()),
    SoTien          DECIMAL(18,2)       NOT NULL,
    PhuongThuc      NVARCHAR(50)        NOT NULL CONSTRAINT DF_ThanhToans_PhuongThuc DEFAULT (N'ChuyenKhoan'), -- 'TienMat', 'ChuyenKhoan'
    GhiChu          NVARCHAR(500)       NULL,
    NgayTao         DATETIME            NOT NULL CONSTRAINT DF_ThanhToans_NgayTao DEFAULT (GETDATE()),

    -- Ràng buộc
    CONSTRAINT PK_ThanhToans PRIMARY KEY CLUSTERED (ThanhToanId),
    CONSTRAINT UQ_ThanhToans_MaPhieuThu UNIQUE (MaPhieuThu),
    CONSTRAINT FK_ThanhToans_KhachHangs FOREIGN KEY (KhachHangId) REFERENCES dbo.KhachHangs(KhachHangId),
    CONSTRAINT CK_ThanhToans_SoTien CHECK (SoTien > 0)
);
GO

CREATE NONCLUSTERED INDEX IX_ThanhToans_KhachHangId_NgayThanhToan ON dbo.ThanhToans(KhachHangId, NgayThanhToan);
GO

-- =========================================================================================
-- BẢNG 6: PhanBoThanhToans - Chi tiết cấn trừ nợ của Phiếu thu vào từng Hóa đơn (Thuật toán FIFO)
-- =========================================================================================
CREATE TABLE dbo.PhanBoThanhToans
(
    PhanBoThanhToanId INT IDENTITY(1,1) NOT NULL,
    ThanhToanId       INT               NOT NULL,
    HoaDonId          INT               NOT NULL,
    SoTienPhanBo      DECIMAL(18,2)     NOT NULL,
    NgayPhanBo        DATETIME          NOT NULL CONSTRAINT DF_PhanBoThanhToans_NgayPhanBo DEFAULT (GETDATE()),

    -- Ràng buộc
    CONSTRAINT PK_PhanBoThanhToans PRIMARY KEY CLUSTERED (PhanBoThanhToanId),
    CONSTRAINT FK_PhanBoThanhToans_ThanhToans FOREIGN KEY (ThanhToanId) REFERENCES dbo.ThanhToans(ThanhToanId) ON DELETE CASCADE,
    CONSTRAINT FK_PhanBoThanhToans_HoaDons FOREIGN KEY (HoaDonId) REFERENCES dbo.HoaDons(HoaDonId),
    CONSTRAINT CK_PhanBoThanhToans_SoTienPhanBo CHECK (SoTienPhanBo > 0)
);
GO

CREATE NONCLUSTERED INDEX IX_PhanBoThanhToans_ThanhToanId ON dbo.PhanBoThanhToans(ThanhToanId);
CREATE NONCLUSTERED INDEX IX_PhanBoThanhToans_HoaDonId ON dbo.PhanBoThanhToans(HoaDonId);
GO

PRINT N'==> ĐÃ KHỞI TẠO THÀNH CÔNG DATABASE [QuanLyBanHangCongNo] VÀ 6 BẢNG DỮ LIỆU!';
GO
