USE [QuanLyBanHangCongNo];
GO

-- 1. Đối chiếu tổng hóa đơn và tiền đã thanh toán
SELECT
    H.MaHoaDon,
    H.TongTien,
    ISNULL(C.TongChiTiet, 0) AS TongChiTiet,
    H.DaThanhToan,
    ISNULL(P.TongPhanBo, 0) AS TongPhanBo,
    CASE
        WHEN H.TongTien = ISNULL(C.TongChiTiet, 0)
         AND H.DaThanhToan = ISNULL(P.TongPhanBo, 0)
        THEN N'Khớp'
        ELSE N'Lệch'
    END AS KetQua
FROM dbo.HoaDons H
OUTER APPLY (
    SELECT SUM(ThanhTien) AS TongChiTiet
    FROM dbo.ChiTietHoaDons
    WHERE HoaDonId = H.HoaDonId
) C
OUTER APPLY (
    SELECT SUM(SoTienPhanBo) AS TongPhanBo
    FROM dbo.PhanBoThanhToans
    WHERE HoaDonId = H.HoaDonId
) P
ORDER BY H.HoaDonId;

-- 2. Kiểm tra tiền phân bổ có vượt tiền thu không
SELECT
    T.MaPhieuThu,
    T.SoTien,
    ISNULL(SUM(P.SoTienPhanBo), 0) AS TongPhanBo,
    CASE
        WHEN ISNULL(SUM(P.SoTienPhanBo), 0) <= T.SoTien
        THEN N'Hợp lệ'
        ELSE N'Vượt tiền thu'
    END AS KetQua
FROM dbo.ThanhToans T
LEFT JOIN dbo.PhanBoThanhToans P
    ON P.ThanhToanId = T.ThanhToanId
GROUP BY T.ThanhToanId, T.MaPhieuThu, T.SoTien;

-- 3. Tìm phân bổ sai khách hàng
SELECT
    T.MaPhieuThu,
    H.MaHoaDon,
    T.KhachHangId AS KhachHangPhieuThu,
    H.KhachHangId AS KhachHangHoaDon
FROM dbo.PhanBoThanhToans P
JOIN dbo.ThanhToans T ON T.ThanhToanId = P.ThanhToanId
JOIN dbo.HoaDons H ON H.HoaDonId = P.HoaDonId
WHERE T.KhachHangId <> H.KhachHangId;