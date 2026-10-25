-- =========================================================================
-- Migration: Bổ sung chỉ mục (Index) tối ưu hóa truy vấn lọc và phân trang
-- cho bảng dbo.NhatKyEmailNhacNos.
-- Các cột tối ưu: ThoiDiemGui, TrangThaiGui, EmailNguoiNhan.
-- LƯU Ý: Migration được thiết kế Idempotent (chạy nhiều lần an toàn).
-- TUYỆT ĐỐI không tự động chạy migration trên DB chính hoặc DB demo.
-- =========================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NhatKyEmailNhacNos]') AND type in (N'U'))
BEGIN
    -- 1. Index cho cột ThoiDiemGui phục vụ sắp xếp thời gian mới nhất và lọc khoảng ngày
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NhatKyEmailNhacNos_ThoiDiemGui' AND object_id = OBJECT_ID(N'[dbo].[NhatKyEmailNhacNos]'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_NhatKyEmailNhacNos_ThoiDiemGui]
        ON [dbo].[NhatKyEmailNhacNos] ([ThoiDiemGui] DESC)
        INCLUDE ([NhatKyId], [KhachHangId], [TenKhachHang], [EmailNguoiNhan], [TrangThaiGui], [CheDoGui], [NguonSoan]);
        PRINT 'Đã tạo index IX_NhatKyEmailNhacNos_ThoiDiemGui.';
    END

    -- 2. Index cho cột TrangThaiGui phục vụ lọc theo trạng thái
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NhatKyEmailNhacNos_TrangThaiGui' AND object_id = OBJECT_ID(N'[dbo].[NhatKyEmailNhacNos]'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_NhatKyEmailNhacNos_TrangThaiGui]
        ON [dbo].[NhatKyEmailNhacNos] ([TrangThaiGui], [ThoiDiemGui] DESC)
        INCLUDE ([NhatKyId], [KhachHangId], [TenKhachHang], [EmailNguoiNhan]);
        PRINT 'Đã tạo index IX_NhatKyEmailNhacNos_TrangThaiGui.';
    END

    -- 3. Index cho cột EmailNguoiNhan phục vụ tìm kiếm theo địa chỉ email
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NhatKyEmailNhacNos_EmailNguoiNhan' AND object_id = OBJECT_ID(N'[dbo].[NhatKyEmailNhacNos]'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_NhatKyEmailNhacNos_EmailNguoiNhan]
        ON [dbo].[NhatKyEmailNhacNos] ([EmailNguoiNhan])
        INCLUDE ([NhatKyId], [TenKhachHang], [ThoiDiemGui], [TrangThaiGui]);
        PRINT 'Đã tạo index IX_NhatKyEmailNhacNos_EmailNguoiNhan.';
    END
END
ELSE
BEGIN
    PRINT 'Bảng dbo.NhatKyEmailNhacNos chưa tồn tại, vui lòng chạy migration khởi tạo trước.';
END
GO
