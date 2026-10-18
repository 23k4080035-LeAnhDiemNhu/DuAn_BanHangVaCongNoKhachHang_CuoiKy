using System;
using System.Collections.Generic;
using System.Linq;
using BanHang.CoreBusiness;

namespace BanHang.UseCases.HoaDonScreen;

/// <summary>
/// DTO chứa toàn bộ thông tin chi tiết một hóa đơn, gồm:
/// - Thông tin chung hóa đơn & khách hàng
/// - Danh sách các dòng hàng sản phẩm (snapshot lịch sử)
/// - Lịch sử các lần phân bổ thanh toán từ phiếu thu
/// - Các thuộc tính tổng hợp để đối chiếu tính nhất quán dữ liệu
/// </summary>
public class ChiTietHoaDonDto
{
    // Thông tin hóa đơn
    public int HoaDonId { get; set; }
    public string MaHoaDon { get; set; } = string.Empty;
    public DateTime NgayLap { get; set; }
    public DateTime HanThanhToan { get; set; }
    public decimal TongTien { get; set; }
    public decimal DaThanhToan { get; set; }
    public decimal ConLai { get; set; }
    public TrangThaiHoaDon TrangThai { get; set; }
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }

    // Thông tin khách hàng
    public int KhachHangId { get; set; }
    public string MaKhachHang { get; set; } = string.Empty;
    public string TenKhachHang { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string? DiaChi { get; set; }

    // Danh sách dòng sản phẩm xuất bán (Snapshot từ ChiTietHoaDons)
    public List<DongHangChiTietHoaDonDto> DanhSachDongHang { get; set; } = new();

    // Lịch sử phân bổ thanh toán vào hóa đơn
    public List<LichSuPhanBoHoaDonDto> DanhSachPhanBo { get; set; } = new();

    // Thuộc tính tính toán để đối chiếu
    public decimal TongTienDongHang => DanhSachDongHang?.Sum(d => d.ThanhTien) ?? 0;
    public decimal TongTienPhanBo => DanhSachPhanBo?.Sum(p => p.SoTienPhanBo) ?? 0;
}

/// <summary>
/// DTO từng dòng hàng chi tiết trong hóa đơn (sử dụng snapshot đã lưu tại thời điểm bán)
/// </summary>
public class DongHangChiTietHoaDonDto
{
    public int ChiTietHoaDonId { get; set; }
    public int HoaDonId { get; set; }
    public int SanPhamId { get; set; }
    public string TenSanPham { get; set; } = string.Empty;
    public string DonViTinh { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
}

/// <summary>
/// DTO ghi nhận từng lượt thanh toán được phân bổ vào hóa đơn này
/// </summary>
public class LichSuPhanBoHoaDonDto
{
    public int PhanBoThanhToanId { get; set; }
    public int ThanhToanId { get; set; }
    public string MaPhieuThu { get; set; } = string.Empty;
    public DateTime NgayThanhToan { get; set; }
    public DateTime NgayPhanBo { get; set; }
    public decimal SoTienPhanBo { get; set; }
    public string PhuongThuc { get; set; } = string.Empty;
}
