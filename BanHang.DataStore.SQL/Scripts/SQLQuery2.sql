-- =========================================================================================
-- FILE 2: 02_SeedData.sql
-- DỰ ÁN CAPSTONE: HỆ THỐNG QUẢN LÝ BÁN HÀNG VÀ CÔNG NỢ KHÁCH HÀNG
-- TÁC GIẢ: Sinh viên ngành Tin học Kinh tế - ECO2415
-- MÔ TẢ: Nạp dữ liệu mẫu chuẩn hóa để kiểm thử toàn diện:
--   1. Kiểm tra hạn mức công nợ (Credit Limit).
--   2. Thuật toán phân bổ thanh toán FIFO (kèm kiểm tra tính nhất quán).
--   3. Báo cáo tuổi nợ 5 nhóm (với ngày chốt báo cáo mốc: 15/10/2026).
-- QUY TẮC: Không chèn dữ liệu vào các cột tính toán (ConLai, ThanhTien).
-- =========================================================================================

USE [QuanLyBanHangCongNo];
GO

PRINT N'==> Bắt đầu nạp dữ liệu mẫu...';
GO

-- 1. XÓA DỮ LIỆU CŨ THEO THỨ TỰ THAM CHIẾU (NẾU CÓ)
DELETE FROM dbo.PhanBoThanhToans;
DELETE FROM dbo.ThanhToans;
DELETE FROM dbo.ChiTietHoaDons;
DELETE FROM dbo.HoaDons;
DELETE FROM dbo.SanPhams;
DELETE FROM dbo.KhachHangs;

-- Reset IDENTITY về 1
DBCC CHECKIDENT ('dbo.KhachHangs', RESEED, 0);
DBCC CHECKIDENT ('dbo.SanPhams', RESEED, 0);
DBCC CHECKIDENT ('dbo.HoaDons', RESEED, 0);
DBCC CHECKIDENT ('dbo.ChiTietHoaDons', RESEED, 0);
DBCC CHECKIDENT ('dbo.ThanhToans', RESEED, 0);
DBCC CHECKIDENT ('dbo.PhanBoThanhToans', RESEED, 0);
GO

-- =========================================================================================
-- 2. NẠP DỮ LIỆU KHÁCH HÀNG (4 khách hàng đại diện cho các mức hạn mức tín dụng khác nhau)
-- =========================================================================================
INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, SoDienThoai, Email, DiaChi, HanMucCongNo, NgayTao, TrangThai)
VALUES
(N'KH001', N'Công ty TNHH Minh Khang', N'0903123456', N'contact@minhkhang.vn', N'123 Nguyễn Huệ, Quận 1, TP.HCM', 100000000.00, '2026-01-10', 1),
(N'KH002', N'DNTN Thiết Bị Hoàng Long', N'0918765432', N'hoanglong@gmail.com', N'45 Lê Lợi, Quận Hải Châu, TP.Đà Nẵng', 50000000.00, '2026-02-15', 1),
(N'KH003', N'CTCP Thương Mại Bách Việt', N'0988654321', N'bachviet.corp@yahoo.com', N'78 Cầu Giấy, Quận Cầu Giấy, Hà Nội', 30000000.00, '2026-03-01', 1),
(N'KH004', N'Cửa Hàng Bán Lẻ Tân Tiến', N'0934112233', N'tantien.store@gmail.com', N'12 Trần Phú, TP.Nha Trang', 0.00, '2026-04-12', 1);
GO

-- =========================================================================================
-- 3. NẠP DỮ LIỆU SẢN PHẨM (6 sản phẩm thiết bị POS & bán hàng)
-- Ghi chú: Không trừ tồn kho khi bán theo phạm vi Bán hàng & Công nợ
-- =========================================================================================
INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, MoTa, NgayTao, TrangThai)
VALUES
(N'SP001', N'Máy in hóa đơn nhiệt Epson TM-T82III', N'Cái', 3500000.00, 25, N'Tốc độ in 250mm/s, khổ giấy K80, cổng LAN/USB', '2026-01-01', 1),
(N'SP002', N'Máy quét mã vạch Honeywell 1900GSR', N'Cái', 2200000.00, 40, N'Quét mã 1D và 2D QR Code, cắm cổng USB', '2026-01-01', 1),
(N'SP003', N'Giấy in nhiệt K80 bọc bạc (Thùng 50 cuộn)', N'Thùng', 450000.00, 100, N'Giấy Oji Nhật Bản cao cấp bọc giấy bạc chống ẩm', '2026-01-01', 1),
(N'SP004', N'Màn hình cảm ứng POS POSIFLEX 15 inch', N'Bộ', 8500000.00, 15, N'Màn hình cảm ứng điện dung, Intel J1900, 4GB RAM', '2026-01-01', 1),
(N'SP005', N'Két đựng tiền thu ngân Maken MK-410', N'Cái', 1200000.00, 30, N'5 ngăn tiền giấy, 4 ngăn tiền xu, mở khóa tự động', '2026-01-01', 1),
(N'SP006', N'Phần mềm Quản lý Bán hàng Bản quyền 1 năm', N'Gói', 2400000.00, 500, N'Bản quyền dịch vụ Cloud quản lý bán hàng', '2026-01-01', 1);
GO

-- =========================================================================================
-- 4. NẠP DỮ LIỆU HÓA ĐƠN & CHI TIẾT HÓA ĐƠN
-- Lưu ý: 
-- - Không insert cột tính toán: ConLai (bảng HoaDons) và ThanhTien (bảng ChiTietHoaDons).
-- - Tổng tiền hóa đơn (TongTien) = Tổng ThanhTien của các dòng con.
-- - Mốc ngày báo cáo tuổi nợ kiểm thử là: 2026-10-15.
-- =========================================================================================

-- --- HÓA ĐƠN 1 (HD001) - KH001: Lập 2026-07-01, Hạn 2026-07-31. ĐÃ THANH TOÁN XONG HẾT BẰNG FIFO.
INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu, NgayTao)
VALUES (N'HD001', 1, '2026-07-01', '2026-07-31', 5700000.00, 5700000.00, 2, N'Hóa đơn trang bị ban đầu (Đã thanh toán đủ)', '2026-07-01');

INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
VALUES 
(1, 1, N'Máy in hóa đơn nhiệt Epson TM-T82III', N'Cái', 1, 3500000.00), -- 3,500,000
(1, 2, N'Máy quét mã vạch Honeywell 1900GSR', N'Cái', 1, 2200000.00); -- 2,200,000 (Tổng = 5,700,000)

-- --- HÓA ĐƠN 2 (HD002) - KH001: Lập 2026-08-01, Hạn 2026-08-31. ĐÃ THANH TOÁN 1 PHẦN (2.3tr), CÒN NỢ 4.5tr.
-- Quá hạn tại 2026-10-15: DATEDIFF(day, '2026-08-31', '2026-10-15') = 45 ngày ➔ THUỘC NHÓM 3 (31–60 ngày).
INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu, NgayTao)
VALUES (N'HD002', 1, '2026-08-01', '2026-08-31', 6800000.00, 2300000.00, 1, N'Hóa đơn mua bổ sung máy quét và két tiền (Còn nợ 4.5tr)', '2026-08-01');

INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
VALUES 
(2, 2, N'Máy quét mã vạch Honeywell 1900GSR', N'Cái', 2, 2200000.00), -- 4,400,000
(2, 5, N'Két đựng tiền thu ngân Maken MK-410', N'Cái', 2, 1200000.00); -- 2,400,000 (Tổng = 6,800,000)

-- --- HÓA ĐƠN 3 (HD003) - KH001: Lập 2026-10-05, Hạn 2026-11-04. CHƯA THANH TOÁN.
-- Quá hạn tại 2026-10-15: Hạn 04/11 > 15/10 (DATEDIFF <= 0) ➔ THUỘC NHÓM 1 (Chưa quá hạn / Trong hạn).
INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu, NgayTao)
VALUES (N'HD003', 1, '2026-10-05', '2026-11-04', 8800000.00, 0.00, 0, N'Hóa đơn đợt tháng 10/2026 (Còn trong hạn tín dụng)', '2026-10-05');

INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
VALUES 
(3, 1, N'Máy in hóa đơn nhiệt Epson TM-T82III', N'Cái', 2, 3500000.00), -- 7,000,000
(3, 3, N'Giấy in nhiệt K80 bọc bạc (Thùng 50 cuộn)', N'Thùng', 4, 450000.00); -- 1,800,000 (Tổng = 8,800,000)

-- --- HÓA ĐƠN 4 (HD004) - KH001: Lập 2026-08-25, Hạn 2026-09-24. CHƯA THANH TOÁN.
-- Quá hạn tại 2026-10-15: DATEDIFF(day, '2026-09-24', '2026-10-15') = 21 ngày ➔ THUỘC NHÓM 2 (Quá hạn 1–30 ngày).
INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu, NgayTao)
VALUES (N'HD004', 1, '2026-08-25', '2026-09-24', 9700000.00, 0.00, 0, N'Hóa đơn màn hình POS và két tiền (Quá hạn 21 ngày)', '2026-08-25');

INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
VALUES 
(4, 4, N'Màn hình cảm ứng POS POSIFLEX 15 inch', N'Bộ', 1, 8500000.00), -- 8,500,000
(4, 5, N'Két đựng tiền thu ngân Maken MK-410', N'Cái', 1, 1200000.00); -- 1,200,000 (Tổng = 9,700,000)

-- --- HÓA ĐƠN 5 (HD005) - KH002: Lập 2026-07-20, Hạn 2026-08-19. CHƯA THANH TOÁN.
-- Quá hạn tại 2026-10-15: DATEDIFF(day, '2026-08-19', '2026-10-15') = 57 ngày ➔ THUỘC NHÓM 3 (Quá hạn 31–60 ngày).
INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu, NgayTao)
VALUES (N'HD005', 2, '2026-07-20', '2026-08-19', 13600000.00, 0.00, 0, N'Hóa đơn Hoàng Long (Quá hạn 57 ngày)', '2026-07-20');

INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
VALUES 
(5, 2, N'Máy quét mã vạch Honeywell 1900GSR', N'Cái', 3, 2200000.00), -- 6,600,000
(5, 1, N'Máy in hóa đơn nhiệt Epson TM-T82III', N'Cái', 2, 3500000.00); -- 7,000,000 (Tổng = 13,600,000)

-- --- HÓA ĐƠN 6 (HD006) - KH002: Lập 2026-06-25, Hạn 2026-07-25. CHƯA THANH TOÁN.
-- Quá hạn tại 2026-10-15: DATEDIFF(day, '2026-07-25', '2026-10-15') = 82 ngày ➔ THUỘC NHÓM 4 (Quá hạn 61–90 ngày).
INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu, NgayTao)
VALUES (N'HD006', 2, '2026-06-25', '2026-07-25', 7200000.00, 0.00, 0, N'Hóa đơn phần mềm và két tiền (Quá hạn 82 ngày)', '2026-06-25');

INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
VALUES 
(6, 6, N'Phần mềm Quản lý Bán hàng Bản quyền 1 năm', N'Gói', 2, 2400000.00), -- 4,800,000
(6, 5, N'Két đựng tiền thu ngân Maken MK-410', N'Cái', 2, 1200000.00); -- 2,400,000 (Tổng = 7,200,000)

-- --- HÓA ĐƠN 7 (HD007) - KH003: Lập 2026-05-10, Hạn 2026-06-09. CHƯA THANH TOÁN.
-- Quá hạn tại 2026-10-15: DATEDIFF(day, '2026-06-09', '2026-10-15') = 128 ngày ➔ THUỘC NHÓM 5 (Quá hạn trên 90 ngày - Nợ xấu).
INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu, NgayTao)
VALUES (N'HD007', 3, '2026-05-10', '2026-06-09', 19250000.00, 0.00, 0, N'Hóa đơn Bách Việt (Quá hạn 128 ngày - Cảnh báo nợ xấu)', '2026-05-10');

INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
VALUES 
(7, 4, N'Màn hình cảm ứng POS POSIFLEX 15 inch', N'Bộ', 2, 8500000.00), -- 17,000,000
(7, 3, N'Giấy in nhiệt K80 bọc bạc (Thùng 50 cuộn)', N'Thùng', 5, 450000.00); -- 2,250,000 (Tổng = 19,250,000)
GO

-- =========================================================================================
-- 5. NẠP DỮ LIỆU PHIẾU THU & PHÂN BỔ THANH TOÁN FIFO
-- Tình huống: Khách hàng KH001 nộp phiếu thu PT001 số tiền 8,000,000 đ vào ngày 2026-09-05.
-- Theo thuật toán FIFO:
--   - Ưu tiên 1: Gạch hết nợ cho HD001 (hóa đơn cũ nhất ngày 01/07): 5,700,000 đ.
--   - Tiền còn lại: 8,000,000 - 5,700,000 = 2,300,000 đ.
--   - Ưu tiên 2: Cấn trừ vào HD002 (hóa đơn tiếp theo ngày 01/08): 2,300,000 đ.
--   - Tổng phân bổ = 5,700,000 + 2,300,000 = 8,000,000 đ (KHỚP 100% SỐ TIỀN THU).
-- =========================================================================================
INSERT INTO dbo.ThanhToans (MaPhieuThu, KhachHangId, NgayThanhToan, SoTien, PhuongThuc, GhiChu, NgayTao)
VALUES (N'PT001', 1, '2026-09-05', 8000000.00, N'ChuyenKhoan', N'Khách hàng KH001 thanh toán đợt 1 qua Vietcombank', '2026-09-05');

INSERT INTO dbo.PhanBoThanhToans (ThanhToanId, HoaDonId, SoTienPhanBo, NgayPhanBo)
VALUES 
(1, 1, 5700000.00, '2026-09-05'), -- Cấn trừ hết HĐ 1 (HD001)
(1, 2, 2300000.00, '2026-09-05'); -- Cấn trừ một phần HĐ 2 (HD002)
GO

-- =========================================================================================
-- 6. CÂU LỆNH KIỂM THỬ XÁC MINH NHANH TRÊN SSMS (Verification Queries)
-- =========================================================================================

PRINT N'-----------------------------------------------------------------------------------------';
PRINT N'==> KIỂM TRA 1: TÍNH NHẤT QUÁN GIỮA PHIẾU THU VÀ CÁC DÒNG PHÂN BỔ FIFO:';
SELECT 
    T.MaPhieuThu,
    T.SoTien AS SoTienPhieuThu,
    ISNULL(SUM(P.SoTienPhanBo), 0) AS TongTienDaPhanBo,
    CASE 
        WHEN T.SoTien = ISNULL(SUM(P.SoTienPhanBo), 0) THEN N'HỢP LỆ (Khớp 100%)'
        ELSE N'LỖI: LỆCH TIỀN PHÂN BỔ'
    END AS KetQuaKiemTra
FROM dbo.ThanhToans T
LEFT JOIN dbo.PhanBoThanhToans P ON T.ThanhToanId = P.ThanhToanId
GROUP BY T.MaPhieuThu, T.SoTien;

PRINT N'-----------------------------------------------------------------------------------------';
PRINT N'==> KIỂM TRA 2: TÍNH NHẤT QUÁN GIỮA TỔNG TIỀN HÓA ĐƠN VÀ TỔNG CHI TIẾT:';
SELECT 
    H.MaHoaDon,
    H.TongTien AS TongTienHoaDon,
    ISNULL(SUM(C.ThanhTien), 0) AS TongThanhTienChiTiet,
    H.DaThanhToan,
    H.ConLai,
    H.TrangThai,
    CASE 
        WHEN H.TongTien = ISNULL(SUM(C.ThanhTien), 0) THEN N'HỢP LỆ (Khớp 100%)'
        ELSE N'LỖI: LỆCH TỔNG TIỀN'
    END AS KetQuaKiemTra
FROM dbo.HoaDons H
LEFT JOIN dbo.ChiTietHoaDons C ON H.HoaDonId = C.HoaDonId
GROUP BY H.MaHoaDon, H.TongTien, H.DaThanhToan, H.ConLai, H.TrangThai;

PRINT N'-----------------------------------------------------------------------------------------';
PRINT N'==> KIỂM TRA 3: BÁO CÁO PHÂN TÍCH TUỔI NỢ CÔNG NỢ 5 NHÓM (Tại mốc: 15/10/2026):';
DECLARE @NgayBaoCao DATETIME = '2026-10-15';

SELECT 
    K.MaKhachHang,
    K.TenKhachHang,
    K.HanMucCongNo,
    ISNULL(SUM(H.ConLai), 0) AS TongCongNoHienTai,
    
    -- Nhóm 1: Chưa quá hạn (Trong hạn)
    ISNULL(SUM(CASE WHEN DATEDIFF(day, H.HanThanhToan, @NgayBaoCao) <= 0 THEN H.ConLai ELSE 0 END), 0) AS Nhom1_ChuaQuaHan,
    
    -- Nhóm 2: Quá hạn 1 - 30 ngày
    ISNULL(SUM(CASE WHEN DATEDIFF(day, H.HanThanhToan, @NgayBaoCao) BETWEEN 1 AND 30 THEN H.ConLai ELSE 0 END), 0) AS Nhom2_QuaHan_1_30,
    
    -- Nhóm 3: Quá hạn 31 - 60 ngày
    ISNULL(SUM(CASE WHEN DATEDIFF(day, H.HanThanhToan, @NgayBaoCao) BETWEEN 31 AND 60 THEN H.ConLai ELSE 0 END), 0) AS Nhom3_QuaHan_31_60,
    
    -- Nhóm 4: Quá hạn 61 - 90 ngày
    ISNULL(SUM(CASE WHEN DATEDIFF(day, H.HanThanhToan, @NgayBaoCao) BETWEEN 61 AND 90 THEN H.ConLai ELSE 0 END), 0) AS Nhom4_QuaHan_61_90,
    
    -- Nhóm 5: Quá hạn trên 90 ngày (Nợ xấu)
    ISNULL(SUM(CASE WHEN DATEDIFF(day, H.HanThanhToan, @NgayBaoCao) > 90 THEN H.ConLai ELSE 0 END), 0) AS Nhom5_QuaHan_Tren_90

FROM dbo.KhachHangs K
INNER JOIN dbo.HoaDons H ON K.KhachHangId = H.KhachHangId
WHERE H.ConLai > 0 -- CHỈ LẤY CÁC HÓA ĐƠN CÒN SỐ DƯ (LOẠI BỎ HÓA ĐƠN ĐÃ THANH TOÁN XONG)
GROUP BY K.MaKhachHang, K.TenKhachHang, K.HanMucCongNo
ORDER BY K.MaKhachHang;

PRINT N'==> NẠP DỮ LIỆU MẪU VÀ KIỂM THỬ THÀNH CÔNG!';
GO
