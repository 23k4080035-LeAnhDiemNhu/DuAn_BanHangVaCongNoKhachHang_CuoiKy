-- =========================================================================
-- Migration: Tạo bảng lưu trữ nhật ký gửi email nhắc nợ (dbo.NhatKyEmailNhacNos)
-- Phục vụ Audit Trail: Khách hàng, người nhận, người duyệt, thời điểm, ngày chốt,
-- nội dung đã duyệt, trạng thái gửi.
-- LƯU Ý: Migration này được thiết kế idempotent (chạy nhiều lần an toàn).
-- Kiểm chứng trên DB Test trước, KHÔNG tự ý chạy trên DB chính khi chưa có lệnh.
-- =========================================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NhatKyEmailNhacNos]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[NhatKyEmailNhacNos] (
        [NhatKyId] INT IDENTITY(1,1) NOT NULL,
        [KhachHangId] INT NOT NULL,
        [MaKhachHang] NVARCHAR(50) NOT NULL,
        [TenKhachHang] NVARCHAR(200) NOT NULL,
        [EmailNguoiNhan] NVARCHAR(256) NOT NULL,
        [NguoiDuyet] NVARCHAR(100) NOT NULL,
        [ThoiDiemDuyet] DATETIME2 NOT NULL,
        [ThoiDiemGui] DATETIME2 NOT NULL,
        [NgayChotBaoCao] DATETIME2 NOT NULL,
        [TieuDe] NVARCHAR(500) NOT NULL,
        [NoiDung] NVARCHAR(MAX) NOT NULL,
        [GiongVan] INT NOT NULL DEFAULT 1, -- 0: NheNhang, 1: TieuChuan, 2: KhanTruong
        [TrangThaiGui] INT NOT NULL DEFAULT 0, -- 0: GuiThu, 1: TiepNhan, 2: ThatBai, 3: ChuaRoKetQua
        [ChiTietTrangThai] NVARCHAR(1000) NULL,
        [MaPhanHoiDichVu] NVARCHAR(200) NULL,
        CONSTRAINT [PK_NhatKyEmailNhacNos] PRIMARY KEY CLUSTERED ([NhatKyId] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_NhatKyEmailNhacNos_KhachHangId] 
    ON [dbo].[NhatKyEmailNhacNos] ([KhachHangId], [ThoiDiemGui] DESC);

    PRINT 'Đã tạo bảng dbo.NhatKyEmailNhacNos thành công.';
END
ELSE
BEGIN
    PRINT 'Bảng dbo.NhatKyEmailNhacNos đã tồn tại.';
END
GO
