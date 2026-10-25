using System.ComponentModel.DataAnnotations;

namespace BanHang.CoreBusiness;

/// <summary>
/// Đại diện cho Phiếu thu tiền / Giao dịch thanh toán của khách hàng
/// </summary>
public class ThanhToan
{
    public int ThanhToanId { get; set; }

    [Required(ErrorMessage = "Mã phiếu thu không được để trống")]
    [StringLength(20, ErrorMessage = "Mã phiếu thu tối đa 20 ký tự")]
    public string MaPhieuThu { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn khách hàng nộp tiền")]
    [Range(1, int.MaxValue, ErrorMessage = "Khách hàng không hợp lệ")]
    public int KhachHangId { get; set; }

    /// <summary>
    /// Đối tượng khách hàng nộp tiền
    /// </summary>
    public KhachHang? KhachHang { get; set; }

    /// <summary>
    /// Ngày thu tiền
    /// </summary>
    public DateTime NgayThanhToan { get; set; } = DateTime.Today;

    /// <summary>
    /// Số tiền khách hàng thanh toán
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền thanh toán phải lớn hơn 0")]
    public decimal SoTien { get; set; } = 0;

    /// <summary>
    /// Phương thức thanh toán: "TienMat" hoặc "ChuyenKhoan"
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
    [StringLength(50, ErrorMessage = "Phương thức thanh toán tối đa 50 ký tự")]
    public string PhuongThuc { get; set; } = "ChuyenKhoan";

    [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")]
    public string? GhiChu { get; set; }

    /// <summary>
    /// Thông tin đối soát chuyển khoản VietQR (chỉ áp dụng khi PhuongThuc == "ChuyenKhoan")
    /// </summary>
    [StringLength(100)]
    public string? NoiDungChuyenKhoan { get; set; }

    [StringLength(50)]
    public string? TaiKhoanThuHuong { get; set; }

    [StringLength(50)]
    public string? NganHangThuHuong { get; set; }

    public bool DaXacNhanTienVao { get; set; } = false;

    public DateTime? ThoiDiemXacNhan { get; set; }

    [StringLength(100)]
    public string? NguoiXacNhan { get; set; }

    /// <summary>
    /// Danh sách chi tiết phân bổ tiền thu vào các hóa đơn còn nợ (theo thuật toán FIFO)
    /// </summary>
    public List<PhanBoThanhToan> DanhSachPhanBo { get; set; } = new();
}
