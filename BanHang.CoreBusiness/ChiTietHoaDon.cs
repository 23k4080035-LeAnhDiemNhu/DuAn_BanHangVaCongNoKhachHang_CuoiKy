using System.ComponentModel.DataAnnotations;

namespace BanHang.CoreBusiness;

/// <summary>
/// Đại diện cho từng dòng sản phẩm trong Hóa đơn bán hàng
/// </summary>
public class ChiTietHoaDon
{
    public int ChiTietHoaDonId { get; set; }

    public int HoaDonId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã sản phẩm không hợp lệ")]
    public int SanPhamId { get; set; }

    /// <summary>
    /// Lưu tên sản phẩm tại thời điểm lập hóa đơn
    /// </summary>
    public string TenSanPham { get; set; } = string.Empty;

    /// <summary>
    /// Đơn vị tính tại thời điểm bán
    /// </summary>
    public string DonViTinh { get; set; } = "Cái";

    [Range(1, 100000, ErrorMessage = "Số lượng mua phải lớn hơn 0")]
    public int SoLuong { get; set; } = 1;

    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không được âm")]
    public decimal DonGia { get; set; } = 0;

    /// <summary>
    /// Thành tiền của dòng hàng = Số lượng * Đơn giá
    /// </summary>
    public decimal ThanhTien => SoLuong * DonGia;

    /// <summary>
    /// Đối tượng sản phẩm tham chiếu (nếu cần)
    /// </summary>
    public SanPham? SanPham { get; set; }
}
