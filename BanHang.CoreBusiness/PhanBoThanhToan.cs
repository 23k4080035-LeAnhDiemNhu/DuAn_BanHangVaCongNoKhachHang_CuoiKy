using System.ComponentModel.DataAnnotations;

namespace BanHang.CoreBusiness;

/// <summary>
/// Đại diện cho kết quả phân bổ số tiền thanh toán từ Phiếu thu sang từng Hóa đơn (theo thuật toán FIFO)
/// </summary>
public class PhanBoThanhToan
{
    public int PhanBoThanhToanId { get; set; }

    public int ThanhToanId { get; set; }

    [Required(ErrorMessage = "Mã hóa đơn không hợp lệ")]
    public int HoaDonId { get; set; }

    /// <summary>
    /// Mã hóa đơn để tiện hiển thị trên báo cáo/giao dịch
    /// </summary>
    public string MaHoaDon { get; set; } = string.Empty;

    /// <summary>
    /// Số tiền phân bổ để cấn trừ nợ cho hóa đơn này
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phân bổ phải lớn hơn 0")]
    public decimal SoTienPhanBo { get; set; } = 0;

    /// <summary>
    /// Ngày thực hiện phân bổ
    /// </summary>
    public DateTime NgayPhanBo { get; set; } = DateTime.Now;

    /// <summary>
    /// Đối tượng hóa đơn tham chiếu
    /// </summary>
    public HoaDon? HoaDon { get; set; }
}
