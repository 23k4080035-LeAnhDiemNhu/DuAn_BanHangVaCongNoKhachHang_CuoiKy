-- =========================================================================================
-- Migration: Bổ sung các cột thông tin đối soát chuyển khoản VietQR vào bảng dbo.ThanhToans
-- Hệ thống: Dự án Bán hàng & Công nợ khách hàng (Đề số 18)
-- LƯU Ý QUAN TRỌNG:
-- 1. Script này được thiết kế IDEMPOTENT (chạy nhiều lần an toàn, không báo lỗi trùng lặp).
-- 2. KHÔNG TỰ CHẠY trên DB chính (QuanLyBanHangCongNo) hoặc DB demo (QuanLyBanHangCongNo_K21_Verify).
-- 3. Chỉ chạy trên môi trường kiểm thử (QuanLyBanHangCongNo_Test) khi có lệnh.
-- =========================================================================================

IF OBJECT_ID('dbo.ThanhToans', 'U') IS NOT NULL
BEGIN
    -- 1. Cột NoiDungChuyenKhoan (Lưu nội dung chuyển khoản chuẩn hóa, VD: THU PT001 KH001)
    IF COL_LENGTH('dbo.ThanhToans', 'NoiDungChuyenKhoan') IS NULL
    BEGIN
        ALTER TABLE dbo.ThanhToans ADD NoiDungChuyenKhoan NVARCHAR(100) NULL;
        PRINT N'  -> Đã thêm cột NoiDungChuyenKhoan vào dbo.ThanhToans.';
    END
    ELSE
    BEGIN
        PRINT N'  -> Cột NoiDungChuyenKhoan đã tồn tại.';
    END

    -- 2. Cột TaiKhoanThuHuong (Số tài khoản thụ hưởng tại thời điểm lập phiếu)
    IF COL_LENGTH('dbo.ThanhToans', 'TaiKhoanThuHuong') IS NULL
    BEGIN
        ALTER TABLE dbo.ThanhToans ADD TaiKhoanThuHuong NVARCHAR(50) NULL;
        PRINT N'  -> Đã thêm cột TaiKhoanThuHuong vào dbo.ThanhToans.';
    END
    ELSE
    BEGIN
        PRINT N'  -> Cột TaiKhoanThuHuong đã tồn tại.';
    END

    -- 3. Cột NganHangThuHuong (Mã ngân hàng / BIN thụ hưởng, VD: ICB / 970415)
    IF COL_LENGTH('dbo.ThanhToans', 'NganHangThuHuong') IS NULL
    BEGIN
        ALTER TABLE dbo.ThanhToans ADD NganHangThuHuong NVARCHAR(50) NULL;
        PRINT N'  -> Đã thêm cột NganHangThuHuong vào dbo.ThanhToans.';
    END
    ELSE
    BEGIN
        PRINT N'  -> Cột NganHangThuHuong đã tồn tại.';
    END

    -- 4. Cột DaXacNhanTienVao (Trạng thái kế toán đã kiểm tra và xác nhận tiền vào tài khoản)
    IF COL_LENGTH('dbo.ThanhToans', 'DaXacNhanTienVao') IS NULL
    BEGIN
        ALTER TABLE dbo.ThanhToans ADD DaXacNhanTienVao BIT NOT NULL CONSTRAINT DF_ThanhToans_DaXacNhanTienVao DEFAULT (0);
        PRINT N'  -> Đã thêm cột DaXacNhanTienVao vào dbo.ThanhToans.';
    END
    ELSE
    BEGIN
        PRINT N'  -> Cột DaXacNhanTienVao đã tồn tại.';
    END

    -- 5. Cột ThoiDiemXacNhan (Thời điểm kế toán tích chọn xác nhận)
    IF COL_LENGTH('dbo.ThanhToans', 'ThoiDiemXacNhan') IS NULL
    BEGIN
        ALTER TABLE dbo.ThanhToans ADD ThoiDiemXacNhan DATETIME NULL;
        PRINT N'  -> Đã thêm cột ThoiDiemXacNhan vào dbo.ThanhToans.';
    END
    ELSE
    BEGIN
        PRINT N'  -> Cột ThoiDiemXacNhan đã tồn tại.';
    END

    -- 6. Cột NguoiXacNhan (Tài khoản kế toán đã thực hiện xác nhận tiền vào)
    IF COL_LENGTH('dbo.ThanhToans', 'NguoiXacNhan') IS NULL
    BEGIN
        ALTER TABLE dbo.ThanhToans ADD NguoiXacNhan NVARCHAR(100) NULL;
        PRINT N'  -> Đã thêm cột NguoiXacNhan vào dbo.ThanhToans.';
    END
    ELSE
    BEGIN
        PRINT N'  -> Cột NguoiXacNhan đã tồn tại.';
    END

    -- 7. Chỉ mục hỗ trợ tra cứu đối soát nhanh
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ThanhToans_DaXacNhan_ThoiDiem' AND object_id = OBJECT_ID('dbo.ThanhToans'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ThanhToans_DaXacNhan_ThoiDiem 
        ON dbo.ThanhToans(DaXacNhanTienVao, ThoiDiemXacNhan)
        INCLUDE (MaPhieuThu, SoTien, NoiDungChuyenKhoan);
        PRINT N'  -> Đã tạo chỉ mục IX_ThanhToans_DaXacNhan_ThoiDiem.';
    END
END
ELSE
BEGIN
    PRINT N'  -> Bảng dbo.ThanhToans chưa tồn tại trong cơ sở dữ liệu.';
END
GO
