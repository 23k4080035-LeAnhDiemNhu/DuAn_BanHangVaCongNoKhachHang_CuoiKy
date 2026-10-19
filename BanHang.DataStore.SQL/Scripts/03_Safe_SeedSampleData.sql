-- =========================================================================================
-- FILE: 03_Safe_SeedSampleData.sql
-- DỰ ÁN CAPSTONE: HỆ THỐNG QUẢN LÝ BÁN HÀNG VÀ CÔNG NỢ KHÁCH HÀNG (ECO2415)
-- MÔ TẢ: Nạp dữ liệu mẫu chuẩn hóa phục vụ kiểm thử đối soát hạn mức, FIFO và tuổi nợ.
-- QUY TẮC AN TOÀN:
--   1. Kiểm tra toàn bộ 6 bảng nghiệp vụ trước khi nạp; nếu đã có dữ liệu thì DỪNG LẠI.
--   2. KHÔNG DROP/DELETE/TRUNCATE hay RESEED dữ liệu hiện có.
--   3. Không hardcode USE database; chặn thực thi trên database hệ thống.
--   4. Chạy trong TRANSACTION; ánh xạ khóa ngoại động qua Mã nghiệp vụ (không phụ thuộc ID Identity).
-- =========================================================================================

DECLARE @CurrentDb NVARCHAR(128) = DB_NAME();
IF @CurrentDb IN ('master', 'model', 'msdb', 'tempdb')
BEGIN
    RAISERROR(N'LỖI AN TOÀN: Không được thực thi script trên CSDL hệ thống [%s]! Vui lòng chọn (USE) đúng database nghiệp vụ trước khi chạy.', 16, 1, @CurrentDb);
    SET NOEXEC ON;
END
GO

DECLARE @initMsg NVARCHAR(200) = N'==> [03_Safe_SeedSampleData] Bắt đầu kiểm tra điều kiện nạp dữ liệu trên CSDL: ' + DB_NAME();
PRINT @initMsg;
GO

-- 1. KIỂM TRA ĐIỀU KIỆN TIÊN QUYẾT: CHỈ NẠP TRÊN CSDL TRỐNG DỮ LIỆU NGHIỆP VỤ
IF EXISTS (SELECT 1 FROM dbo.KhachHangs)
   OR EXISTS (SELECT 1 FROM dbo.SanPhams)
   OR EXISTS (SELECT 1 FROM dbo.HoaDons)
   OR EXISTS (SELECT 1 FROM dbo.ChiTietHoaDons)
   OR EXISTS (SELECT 1 FROM dbo.ThanhToans)
   OR EXISTS (SELECT 1 FROM dbo.PhanBoThanhToans)
BEGIN
    PRINT N'THÔNG BÁO BẢO VỆ DỮ LIỆU: Một hoặc nhiều bảng nghiệp vụ đã có dữ liệu.';
    PRINT N'Script seed dừng lại an toàn để bảo toàn 100% dữ liệu hiện hữu (KHÔNG xóa, KHÔNG ghi đè).';
END
ELSE
BEGIN
    BEGIN TRANSACTION;
    BEGIN TRY
    PRINT N'  -> Đang nạp danh mục Khách hàng (kèm chính sách Hạn mức và Số ngày nợ)...';
    INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, SoDienThoai, Email, DiaChi, HanMucCongNo, SoNgayNo, NgayTao, TrangThai)
    VALUES
    (N'KH001', N'Công ty TNHH Minh Khang', N'0903123456', N'contact@minhkhang.vn', N'123 Nguyễn Huệ, Quận 1, TP.HCM', 100000000.00, 30, '2026-01-10', 1),
    (N'KH002', N'DNTN Thiết Bị Hoàng Long', N'0918765432', N'hoanglong@gmail.com', N'45 Lê Lợi, Quận Hải Châu, TP.Đà Nẵng', 50000000.00, 15, '2026-02-15', 1),
    (N'KH003', N'CTCP Thương Mại Bách Việt', N'0988654321', N'bachviet.corp@yahoo.com', N'78 Cầu Giấy, Quận Cầu Giấy, Hà Nội', 30000000.00, 45, '2026-03-01', 1),
    (N'KH004', N'Cửa Hàng Bán Lẻ Tân Tiến', N'0934112233', N'tantien.store@gmail.com', N'12 Trần Phú, TP.Nha Trang', 0.00, 0, '2026-04-12', 1);

    PRINT N'  -> Đang nạp danh mục Sản phẩm niêm yết...';
    INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, MoTa, NgayTao, TrangThai)
    VALUES
    (N'SP001', N'Máy in hóa đơn nhiệt Epson TM-T82III', N'Cái', 3500000.00, 25, N'Tốc độ in 250mm/s, khổ giấy K80, cổng LAN/USB', '2026-01-01', 1),
    (N'SP002', N'Máy quét mã vạch Honeywell 1900GSR', N'Cái', 2200000.00, 40, N'Quét mã 1D và 2D QR Code, cắm cổng USB', '2026-01-01', 1),
    (N'SP003', N'Giấy in nhiệt K80 bọc bạc (Thùng 50 cuộn)', N'Thùng', 450000.00, 100, N'Giấy Oji Nhật Bản cao cấp bọc giấy bạc chống ẩm', '2026-01-01', 1),
    (N'SP004', N'Màn hình cảm ứng POS POSIFLEX 15 inch', N'Bộ', 8500000.00, 15, N'Màn hình cảm ứng điện dung, Intel J1900, 4GB RAM', '2026-01-01', 1),
    (N'SP005', N'Két đựng tiền thu ngân Maken MK-410', N'Cái', 1200000.00, 30, N'5 ngăn tiền giấy, 4 ngăn tiền xu, mở khóa tự động', '2026-01-01', 1),
    (N'SP006', N'Phần mềm Quản lý Bán hàng Bản quyền 1 năm', N'Gói', 2400000.00, 500, N'Bản quyền dịch vụ Cloud quản lý bán hàng', '2026-01-01', 1);

    -- Lấy ID động của khách hàng và sản phẩm theo Mã nghiệp vụ
    DECLARE @Id_KH001 INT = (SELECT KhachHangId FROM dbo.KhachHangs WHERE MaKhachHang = N'KH001');
    DECLARE @Id_KH002 INT = (SELECT KhachHangId FROM dbo.KhachHangs WHERE MaKhachHang = N'KH002');
    DECLARE @Id_KH003 INT = (SELECT KhachHangId FROM dbo.KhachHangs WHERE MaKhachHang = N'KH003');

    DECLARE @Id_SP001 INT = (SELECT SanPhamId FROM dbo.SanPhams WHERE MaSanPham = N'SP001');
    DECLARE @Id_SP002 INT = (SELECT SanPhamId FROM dbo.SanPhams WHERE MaSanPham = N'SP002');
    DECLARE @Id_SP003 INT = (SELECT SanPhamId FROM dbo.SanPhams WHERE MaSanPham = N'SP003');
    DECLARE @Id_SP004 INT = (SELECT SanPhamId FROM dbo.SanPhams WHERE MaSanPham = N'SP004');
    DECLARE @Id_SP005 INT = (SELECT SanPhamId FROM dbo.SanPhams WHERE MaSanPham = N'SP005');
    DECLARE @Id_SP006 INT = (SELECT SanPhamId FROM dbo.SanPhams WHERE MaSanPham = N'SP006');

    PRINT N'  -> Đang nạp Hóa đơn bán chịu và Chi tiết hàng hóa...';
    -- HD001: KH001, Lập 01/07, Hạn 31/07 (30 ngày), Đã thanh toán đủ bằng FIFO
    INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu, NgayTao)
    VALUES (N'HD001', @Id_KH001, '2026-07-01', '2026-07-31', 5700000.00, 5700000.00, 2, N'Hóa đơn trang bị ban đầu (Đã thanh toán đủ)', '2026-07-01');
    DECLARE @Id_HD001 INT = SCOPE_IDENTITY();

    INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
    VALUES 
    (@Id_HD001, @Id_SP001, N'Máy in hóa đơn nhiệt Epson TM-T82III', N'Cái', 1, 3500000.00),
    (@Id_HD001, @Id_SP002, N'Máy quét mã vạch Honeywell 1900GSR', N'Cái', 1, 2200000.00);

    -- HD002: KH001, Lập 01/08, Hạn 31/08 (30 ngày), Đã TT 2.3tr, còn nợ 4.5tr
    INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu, NgayTao)
    VALUES (N'HD002', @Id_KH001, '2026-08-01', '2026-08-31', 6800000.00, 2300000.00, 1, N'Hóa đơn mua bổ sung máy quét và két tiền (Còn nợ 4.5tr)', '2026-08-01');
    DECLARE @Id_HD002 INT = SCOPE_IDENTITY();

    INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
    VALUES 
    (@Id_HD002, @Id_SP002, N'Máy quét mã vạch Honeywell 1900GSR', N'Cái', 2, 2200000.00),
    (@Id_HD002, @Id_SP005, N'Két đựng tiền thu ngân Maken MK-410', N'Cái', 2, 1200000.00);

    -- HD003: KH001, Lập 05/10, Hạn 04/11 (30 ngày), Chưa TT (trong hạn)
    INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu, NgayTao)
    VALUES (N'HD003', @Id_KH001, '2026-10-05', '2026-11-04', 8800000.00, 0.00, 0, N'Hóa đơn đợt tháng 10/2026 (Còn trong hạn tín dụng)', '2026-10-05');
    DECLARE @Id_HD003 INT = SCOPE_IDENTITY();

    INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
    VALUES 
    (@Id_HD003, @Id_SP001, N'Máy in hóa đơn nhiệt Epson TM-T82III', N'Cái', 2, 3500000.00),
    (@Id_HD003, @Id_SP003, N'Giấy in nhiệt K80 bọc bạc (Thùng 50 cuộn)', N'Thùng', 4, 450000.00);

    -- HD004: KH001, Lập 25/08, Hạn 24/09 (30 ngày), Chưa TT
    INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu, NgayTao)
    VALUES (N'HD004', @Id_KH001, '2026-08-25', '2026-09-24', 9700000.00, 0.00, 0, N'Hóa đơn màn hình POS và két tiền (Quá hạn 21 ngày)', '2026-08-25');
    DECLARE @Id_HD004 INT = SCOPE_IDENTITY();

    INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
    VALUES 
    (@Id_HD004, @Id_SP004, N'Màn hình cảm ứng POS POSIFLEX 15 inch', N'Bộ', 1, 8500000.00),
    (@Id_HD004, @Id_SP005, N'Két đựng tiền thu ngân Maken MK-410', N'Cái', 1, 1200000.00);

    -- HD005: KH002, Lập 20/07, Hạn 19/08 (30 ngày thỏa thuận), Chưa TT
    INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu, NgayTao)
    VALUES (N'HD005', @Id_KH002, '2026-07-20', '2026-08-19', 13600000.00, 0.00, 0, N'Hóa đơn Hoàng Long (Quá hạn 57 ngày)', '2026-07-20');
    DECLARE @Id_HD005 INT = SCOPE_IDENTITY();

    INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
    VALUES 
    (@Id_HD005, @Id_SP002, N'Máy quét mã vạch Honeywell 1900GSR', N'Cái', 3, 2200000.00),
    (@Id_HD005, @Id_SP001, N'Máy in hóa đơn nhiệt Epson TM-T82III', N'Cái', 2, 3500000.00);

    -- HD006: KH002, Lập 25/06, Hạn 25/07 (30 ngày), Chưa TT
    INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu, NgayTao)
    VALUES (N'HD006', @Id_KH002, '2026-06-25', '2026-07-25', 7200000.00, 0.00, 0, N'Hóa đơn phần mềm và két tiền (Quá hạn 82 ngày)', '2026-06-25');
    DECLARE @Id_HD006 INT = SCOPE_IDENTITY();

    INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
    VALUES 
    (@Id_HD006, @Id_SP006, N'Phần mềm Quản lý Bán hàng Bản quyền 1 năm', N'Gói', 2, 2400000.00),
    (@Id_HD006, @Id_SP005, N'Két đựng tiền thu ngân Maken MK-410', N'Cái', 2, 1200000.00);

    -- HD007: KH003, Lập 10/05, Hạn 09/06 (30 ngày), Chưa TT
    INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu, NgayTao)
    VALUES (N'HD007', @Id_KH003, '2026-05-10', '2026-06-09', 19250000.00, 0.00, 0, N'Hóa đơn Bách Việt (Quá hạn 128 ngày - Nợ xấu)', '2026-05-10');
    DECLARE @Id_HD007 INT = SCOPE_IDENTITY();

    INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
    VALUES 
    (@Id_HD007, @Id_SP004, N'Màn hình cảm ứng POS POSIFLEX 15 inch', N'Bộ', 2, 8500000.00),
    (@Id_HD007, @Id_SP003, N'Giấy in nhiệt K80 bọc bạc (Thùng 50 cuộn)', N'Thùng', 5, 450000.00);

    PRINT N'  -> Đang nạp Phiếu thu tiền và Phân bổ thanh toán FIFO...';
    -- PT001: KH001 nộp 8,000,000 đ
    INSERT INTO dbo.ThanhToans (MaPhieuThu, KhachHangId, NgayThanhToan, SoTien, PhuongThuc, GhiChu, NgayTao)
    VALUES (N'PT001', @Id_KH001, '2026-09-05', 8000000.00, N'ChuyenKhoan', N'Khách hàng KH001 thanh toán đợt 1 qua Vietcombank', '2026-09-05');
    DECLARE @Id_PT001 INT = SCOPE_IDENTITY();

    INSERT INTO dbo.PhanBoThanhToans (ThanhToanId, HoaDonId, SoTienPhanBo, NgayPhanBo)
    VALUES 
    (@Id_PT001, @Id_HD001, 5700000.00, '2026-09-05'),
    (@Id_PT001, @Id_HD002, 2300000.00, '2026-09-05');

    COMMIT TRANSACTION;
    PRINT N'==> [03_Safe_SeedSampleData] NẠP DỮ LIỆU MẪU THÀNH CÔNG VÀ AN TOÀN TRONG TRANSACTION!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'LỖI KHI NẠP DỮ LIỆU MẪU: %s', 16, 1, @ErrorMessage);
END CATCH
END
GO
