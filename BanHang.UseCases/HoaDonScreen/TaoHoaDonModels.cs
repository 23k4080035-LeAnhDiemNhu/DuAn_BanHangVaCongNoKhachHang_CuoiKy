namespace BanHang.UseCases.HoaDonScreen;

/// <summary>
/// DTO dòng hàng khi lập hóa đơn mới
/// </summary>
public class TaoChiTietHoaDonDto
{
    public int SanPhamId { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGiaHienThi { get; set; }
}

/// <summary>
/// DTO yêu cầu tạo hóa đơn bán chịu mới
/// </summary>
public class TaoHoaDonRequest
{
    public string MaHoaDon { get; set; } = string.Empty;
    public int KhachHangId { get; set; }
    public DateTime NgayLap { get; set; } = DateTime.Today;
    public DateTime? HanThanhToan { get; set; }
    public string? GhiChu { get; set; }
    public List<TaoChiTietHoaDonDto> ChiTietHoaDons { get; set; } = new();
}

/// <summary>
/// DTO kết quả thực hiện tạo hóa đơn
/// </summary>
public class TaoHoaDonResult
{
    public bool ThanhCong { get; set; }
    public string ThongBao { get; set; } = string.Empty;
    public int? HoaDonIdMoi { get; set; }
    public string? MaHoaDonMoi { get; set; }
    public decimal? TongTien { get; set; }
    public bool GiaBiThayDoi { get; set; }
}
