using System.ComponentModel.DataAnnotations;

namespace BanHang.CoreBusiness;

/// <summary>
/// Đại diện cho Khách hàng trong hệ thống bán hàng và theo dõi công nợ
/// </summary>
public class KhachHang
{
    public int KhachHangId { get; set; }

    [Required(ErrorMessage = "Mã khách hàng không được để trống")]
    [StringLength(20, ErrorMessage = "Mã khách hàng tối đa 20 ký tự")]
    public string MaKhachHang { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên khách hàng không được để trống")]
    [StringLength(150, ErrorMessage = "Tên khách hàng tối đa 150 ký tự")]
    public string TenKhachHang { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
    [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
    public string? SoDienThoai { get; set; }

    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
    public string? Email { get; set; }

    [StringLength(250, ErrorMessage = "Địa chỉ tối đa 250 ký tự")]
    public string? DiaChi { get; set; }

    /// <summary>
    /// Hạn mức công nợ tối đa cho phép khách hàng được nợ (VNĐ). Mặc định là 0 (phải thanh toán ngay).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Hạn mức công nợ không được âm")]
    public decimal HanMucCongNo { get; set; } = 0;

    /// <summary>
    /// Số dư công nợ hiện tại mà khách hàng đang nợ doanh nghiệp (VNĐ).
    /// </summary>
    public decimal SoDuCongNo { get; set; } = 0;

    /// <summary>
    /// Số tiền công nợ còn lại mà khách hàng được phép mua chịu tiếp.
    /// </summary>
    public decimal ConDuocMuaNo => Math.Max(0, HanMucCongNo - SoDuCongNo);

    /// <summary>
    /// Kiểm tra khách hàng hiện tại có đang bị vượt hạn mức công nợ hay không
    /// </summary>
    public bool DaVuotHanMuc => SoDuCongNo > HanMucCongNo;

    /// <summary>
    /// Kiểm tra xem nếu khách mua thêm một đơn hàng với giá trị soTienDonHangMoi thì có vượt hạn mức không
    /// </summary>
    public bool KiemTraVuotHanMucKhiMua(decimal soTienDonHangMoi)
    {
        return (SoDuCongNo + soTienDonHangMoi) > HanMucCongNo;
    }
}
