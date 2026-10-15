using System.ComponentModel.DataAnnotations;

namespace BanHang.CoreBusiness;

/// <summary>
/// Đại diện cho Sản phẩm / Hàng hóa trong hệ thống
/// </summary>
public class SanPham
{
    public int SanPhamId { get; set; }

    [Required(ErrorMessage = "Mã sản phẩm không được để trống")]
    [StringLength(20, ErrorMessage = "Mã sản phẩm tối đa 20 ký tự")]
    public string MaSanPham { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    [StringLength(200, ErrorMessage = "Tên sản phẩm tối đa 200 ký tự")]
    public string TenSanPham { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đơn vị tính không được để trống")]
    [StringLength(50, ErrorMessage = "Đơn vị tính tối đa 50 ký tự")]
    public string DonViTinh { get; set; } = "Cái";

    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không được âm")]
    public decimal DonGia { get; set; } = 0;

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn không được âm")]
    public int SoLuongTon { get; set; } = 0;

    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
    public string? MoTa { get; set; }

    [StringLength(300, ErrorMessage = "Đường dẫn hình ảnh tối đa 300 ký tự")]
    public string? HinhAnh { get; set; }

    /// <summary>
    /// Kiểm tra số lượng tồn kho có đủ đáp ứng số lượng cần xuất không
    /// </summary>
    public bool CoDuTonKho(int soLuongCan)
    {
        return SoLuongTon >= soLuongCan;
    }
}
