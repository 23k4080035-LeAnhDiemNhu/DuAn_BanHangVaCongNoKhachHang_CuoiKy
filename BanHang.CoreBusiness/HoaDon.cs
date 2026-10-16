using System.ComponentModel.DataAnnotations;

namespace BanHang.CoreBusiness;

/// <summary>
/// Đại diện cho Hóa đơn bán hàng kiêm theo dõi công nợ của từng đơn hàng
/// </summary>
public class HoaDon
{
    public int HoaDonId { get; set; }

    [Required(ErrorMessage = "Mã hóa đơn không được để trống")]
    [StringLength(20, ErrorMessage = "Mã hóa đơn tối đa 20 ký tự")]
    public string MaHoaDon { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn khách hàng")]
    [Range(1, int.MaxValue, ErrorMessage = "Khách hàng không hợp lệ")]
    public int KhachHangId { get; set; }

    /// <summary>
    /// Đối tượng khách hàng tham chiếu
    /// </summary>
    public KhachHang? KhachHang { get; set; }

    /// <summary>
    /// Ngày lập hóa đơn
    /// </summary>
    public DateTime NgayLap { get; set; } = DateTime.Today;

    /// <summary>
    /// Hạn chót phải thanh toán hóa đơn (dùng để tính tuổi nợ quá hạn)
    /// </summary>
    public DateTime HanThanhToan { get; set; } = DateTime.Today;

    /// <summary>
    /// Tổng tiền phải thanh toán của hóa đơn
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền không được âm")]
    public decimal TongTien { get; set; } = 0;

    /// <summary>
    /// Số tiền đã thanh toán tích lũy cho hóa đơn này
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Số tiền đã thanh toán không được âm")]
    public decimal DaThanhToan { get; set; } = 0;

    /// <summary>
    /// Số tiền còn nợ của hóa đơn này = Tổng tiền - Đã thanh toán
    /// </summary>
    public decimal ConLai => Math.Max(0, TongTien - DaThanhToan);

    /// <summary>
    /// Trạng thái thanh toán của hóa đơn
    /// </summary>
    public TrangThaiHoaDon TrangThai { get; set; } = TrangThaiHoaDon.ChuaThanhToan;

    [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")]
    public string? GhiChu { get; set; }

    /// <summary>
    /// Danh sách các sản phẩm trong hóa đơn
    /// </summary>
    public List<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new();

    /// <summary>
    /// Tự động tính lại tổng tiền từ danh sách chi tiết hóa đơn
    /// </summary>
    public void CapNhatTongTien()
    {
        TongTien = ChiTietHoaDons.Sum(c => c.ThanhTien);
        CapNhatTrangThai();
    }

    /// <summary>
    /// Cập nhật trạng thái thanh toán dựa trên DaThanhToan và TongTien
    /// </summary>
    public void CapNhatTrangThai()
    {
        if (DaThanhToan <= 0)
        {
            TrangThai = TrangThaiHoaDon.ChuaThanhToan;
        }
        else if (DaThanhToan >= TongTien)
        {
            TrangThai = TrangThaiHoaDon.DaThanhToan;
        }
        else
        {
            TrangThai = TrangThaiHoaDon.ThanhToanMotPhan;
        }
    }

    /// <summary>
    /// Ghi nhận phân bổ thanh toán vào hóa đơn
    /// </summary>
    public void GhiNhanThanhToan(decimal soTienPhanBo)
    {
        if (soTienPhanBo <= 0)
        {
            throw new QuyTacNghiepVuException("Số tiền thanh toán phân bổ phải lớn hơn 0.");
        }

        if (soTienPhanBo > ConLai)
        {
            throw new QuyTacNghiepVuException($"Số tiền phân bổ ({soTienPhanBo:N0} đ) vượt quá số tiền nợ còn lại của hóa đơn ({ConLai:N0} đ).");
        }

        DaThanhToan += soTienPhanBo;
        CapNhatTrangThai();
    }

    /// <summary>
    /// Tính số ngày quá hạn thanh toán so với một ngày cụ thể (mặc định là hôm nay)
    /// </summary>
    public int TinhSoNgayQuaHan(DateTime? ngayKiemTra = null)
    {
        var ngay = ngayKiemTra ?? DateTime.Today;
        if (ngay <= HanThanhToan) return 0;
        return (ngay - HanThanhToan).Days;
    }
}
