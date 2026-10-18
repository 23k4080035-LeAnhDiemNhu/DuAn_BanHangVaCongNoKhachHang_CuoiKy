namespace BanHang.UseCases.HoaDonScreen;

/// <summary>
/// Dòng sản phẩm lưu tạm trong hóa đơn nháp.
/// Chỉ lưu ID sản phẩm và số lượng yêu cầu; không lưu giá hay tồn kho cũ làm nguồn quyết định.
/// </summary>
public class DongHangNhapDto
{
    public int SanPhamId { get; set; }
    public int SoLuong { get; set; } = 1;
}

/// <summary>
/// Dữ liệu trạng thái hóa đơn nháp đang thao tác trong phiên làm việc (circuit) hiện tại.
/// </summary>
public class HoaDonNhapDto
{
    public int KhachHangId { get; set; }
    public DateTime NgayLap { get; set; } = DateTime.Today;
    public DateTime? HanThanhToan { get; set; }
    public string? GhiChu { get; set; }
    public List<DongHangNhapDto> ChiTietDongs { get; set; } = new();

    /// <summary>
    /// Kiểm tra người dùng đã bắt đầu nhập bất kỳ thông tin nào hay chưa
    /// </summary>
    public bool CoDuLieu => KhachHangId > 0
        || (ChiTietDongs != null && ChiTietDongs.Any(d => d.SanPhamId > 0))
        || !string.IsNullOrWhiteSpace(GhiChu)
        || HanThanhToan.HasValue;
}

/// <summary>
/// Service quản lý trạng thái hóa đơn bán chịu nháp theo vòng đời Scoped (tồn tại trong 1 circuit của Blazor Server).
/// Độc lập hoàn toàn với CSDL SQL và kiểu giao diện.
/// </summary>
public interface IHoaDonNhapStateService
{
    /// <summary>
    /// Cho biết hiện tại trong circuit có dữ liệu nháp đang lưu hay không
    /// </summary>
    bool CoDuLieuNhap { get; }

    /// <summary>
    /// Lấy bản sao dữ liệu hóa đơn nháp hiện tại
    /// </summary>
    HoaDonNhapDto LayDuLieuNhap();

    /// <summary>
    /// Cập nhật dữ liệu hóa đơn nháp
    /// </summary>
    void CapNhatNhap(int khachHangId, DateTime ngayLap, DateTime? hanThanhToan, string? ghiChu, IEnumerable<DongHangNhapDto> chiTiets);

    /// <summary>
    /// Xóa toàn bộ dữ liệu nháp khi lưu thành công hoặc người dùng chủ động bấm Hủy
    /// </summary>
    void XoaNhap();
}
