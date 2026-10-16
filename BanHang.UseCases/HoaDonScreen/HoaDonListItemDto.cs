using BanHang.CoreBusiness;

namespace BanHang.UseCases.HoaDonScreen;

/// <summary>
/// DTO chứa thông tin hóa đơn kèm thông tin khách hàng (phục vụ hiển thị danh sách hóa đơn)
/// </summary>
public class HoaDonListItemDto
{
    public int HoaDonId { get; set; }
    public string MaHoaDon { get; set; } = string.Empty;
    public int KhachHangId { get; set; }
    public string MaKhachHang { get; set; } = string.Empty;
    public string TenKhachHang { get; set; } = string.Empty;
    public DateTime NgayLap { get; set; }
    public DateTime HanThanhToan { get; set; }
    public decimal TongTien { get; set; }
    public decimal DaThanhToan { get; set; }
    public decimal ConLai { get; set; }
    public TrangThaiHoaDon TrangThai { get; set; }
    public string? GhiChu { get; set; }
}
