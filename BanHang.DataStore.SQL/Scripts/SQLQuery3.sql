USE [QuanLyBanHangCongNo];
GO

SELECT KhachHangId, MaKhachHang, TenKhachHang
FROM dbo.KhachHangs
ORDER BY KhachHangId;

SELECT SanPhamId, MaSanPham
FROM dbo.SanPhams
ORDER BY SanPhamId;

SELECT HoaDonId, MaHoaDon, KhachHangId,
       TongTien, DaThanhToan
FROM dbo.HoaDons
ORDER BY HoaDonId;

SELECT ThanhToanId, MaPhieuThu, KhachHangId, SoTien
FROM dbo.ThanhToans
ORDER BY ThanhToanId;

SELECT COUNT(*) AS SoDongChiTiet
FROM dbo.ChiTietHoaDons;

SELECT COUNT(*) AS SoDongPhanBo
FROM dbo.PhanBoThanhToans;