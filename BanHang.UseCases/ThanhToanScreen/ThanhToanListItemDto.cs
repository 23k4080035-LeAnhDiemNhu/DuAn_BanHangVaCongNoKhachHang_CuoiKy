namespace BanHang.UseCases.ThanhToanScreen;

/// <summary>
/// DTO hiển thị danh sách phiếu thu kèm thông tin khách hàng và số tiền đã/chưa phân bổ
/// </summary>
public class ThanhToanListItemDto
{
    public int ThanhToanId { get; set; }
    public string MaPhieuThu { get; set; } = string.Empty;
    public int KhachHangId { get; set; }
    public string MaKhachHang { get; set; } = string.Empty;
    public string TenKhachHang { get; set; } = string.Empty;
    public DateTime NgayThanhToan { get; set; }
    public decimal SoTien { get; set; }
    public string PhuongThuc { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
    public decimal TongDaPhanBo { get; set; }
    public decimal TienChuaPhanBo { get; set; }
}
