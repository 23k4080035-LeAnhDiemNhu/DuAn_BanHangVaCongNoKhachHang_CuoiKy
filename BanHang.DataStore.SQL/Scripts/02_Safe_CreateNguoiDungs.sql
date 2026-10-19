-- =========================================================================================
-- FILE: 02_Safe_CreateNguoiDungs.sql
-- DỰ ÁN CAPSTONE: HỆ THỐNG QUẢN LÝ BÁN HÀNG VÀ CÔNG NỢ KHÁCH HÀNG (ECO2415)
-- MÔ TẢ: Khởi tạo bảng tài khoản [dbo].[NguoiDungs] phục vụ phân quyền 2 vai trò.
-- QUY TẮC AN TOÀN:
--   1. Idempotent, không xóa dữ liệu, kiểm tra chi tiết cấu trúc cột.
--   2. Không hardcode USE database; chặn thực thi trên database hệ thống.
--   3. Không chèn mật khẩu dạng văn bản rõ (plaintext).
-- =========================================================================================

DECLARE @CurrentDb NVARCHAR(128) = DB_NAME();
IF @CurrentDb IN ('master', 'model', 'msdb', 'tempdb')
BEGIN
    RAISERROR(N'LỖI AN TOÀN: Không được thực thi script trên CSDL hệ thống [%s]! Vui lòng chọn (USE) đúng database nghiệp vụ trước khi chạy.', 16, 1, @CurrentDb);
    SET NOEXEC ON;
END
GO

DECLARE @initMsg NVARCHAR(200) = N'==> [02_Safe_CreateNguoiDungs] Bắt đầu thiết lập bảng tài khoản trên CSDL: ' + DB_NAME();
PRINT @initMsg;
GO

IF OBJECT_ID('dbo.NguoiDungs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.NguoiDungs
    (
        NguoiDungId     INT IDENTITY(1,1)   NOT NULL,
        TenDangNhap     NVARCHAR(50)        NOT NULL,
        MatKhauHash     NVARCHAR(256)       NOT NULL,
        HoTen           NVARCHAR(100)       NOT NULL,
        VaiTro          NVARCHAR(50)        NOT NULL,
        TrangThai       BIT                 NOT NULL CONSTRAINT DF_NguoiDungs_TrangThai DEFAULT (1),
        NgayTao         DATETIME            NOT NULL CONSTRAINT DF_NguoiDungs_NgayTao DEFAULT (GETDATE()),

        CONSTRAINT PK_NguoiDungs PRIMARY KEY CLUSTERED (NguoiDungId),
        CONSTRAINT UQ_NguoiDungs_TenDangNhap UNIQUE (TenDangNhap),
        CONSTRAINT CK_NguoiDungs_VaiTro CHECK (VaiTro IN (N'NhanVienBanHang', N'KeToanCongNo'))
    );

    CREATE NONCLUSTERED INDEX IX_NguoiDungs_TenDangNhap ON dbo.NguoiDungs(TenDangNhap);
    PRINT N'  -> Đã tạo bảng dbo.NguoiDungs thành công.';
END
ELSE
BEGIN
    PRINT N'  -> Bảng dbo.NguoiDungs đã tồn tại. Đang kiểm tra cấu trúc...';
    
    DECLARE @MissingCols NVARCHAR(MAX) = N'';
    IF COL_LENGTH('dbo.NguoiDungs', 'NguoiDungId') IS NULL SET @MissingCols += N'NguoiDungId, ';
    IF COL_LENGTH('dbo.NguoiDungs', 'TenDangNhap') IS NULL SET @MissingCols += N'TenDangNhap, ';
    IF COL_LENGTH('dbo.NguoiDungs', 'MatKhauHash') IS NULL SET @MissingCols += N'MatKhauHash, ';
    IF COL_LENGTH('dbo.NguoiDungs', 'HoTen') IS NULL SET @MissingCols += N'HoTen, ';
    IF COL_LENGTH('dbo.NguoiDungs', 'VaiTro') IS NULL SET @MissingCols += N'VaiTro, ';
    IF COL_LENGTH('dbo.NguoiDungs', 'TrangThai') IS NULL SET @MissingCols += N'TrangThai, ';
    IF COL_LENGTH('dbo.NguoiDungs', 'NgayTao') IS NULL SET @MissingCols += N'NgayTao, ';

    IF LEN(@MissingCols) > 0
    BEGIN
        DECLARE @ErrMsg NVARCHAR(500) = N'LỖI SCHEMA: Bảng dbo.NguoiDungs đã tồn tại nhưng thiếu các cột: ' + SUBSTRING(@MissingCols, 1, LEN(@MissingCols) - 1);
        RAISERROR(@ErrMsg, 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_NguoiDungs_VaiTro' AND parent_object_id = OBJECT_ID('dbo.NguoiDungs'))
    BEGIN
        ALTER TABLE dbo.NguoiDungs ADD CONSTRAINT CK_NguoiDungs_VaiTro CHECK (VaiTro IN (N'NhanVienBanHang', N'KeToanCongNo'));
        PRINT N'    + Đã bổ sung ràng buộc CK_NguoiDungs_VaiTro.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_NguoiDungs_TenDangNhap' AND object_id = OBJECT_ID('dbo.NguoiDungs'))
    BEGIN
        ALTER TABLE dbo.NguoiDungs ADD CONSTRAINT UQ_NguoiDungs_TenDangNhap UNIQUE (TenDangNhap);
        PRINT N'    + Đã bổ sung ràng buộc UQ_NguoiDungs_TenDangNhap.';
    END

    PRINT N'  -> Bảng dbo.NguoiDungs hoàn toàn tương thích cấu trúc.';
END
GO

PRINT N'==> [02_Safe_CreateNguoiDungs] HOÀN TẤT THIẾT LẬP BẢNG TÀI KHOẢN AN TOÀN!';
GO
