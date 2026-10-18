using BanHang.CoreBusiness;
using BanHang.UseCases.SanPhamScreen;

namespace BanHang.UseCases.PluginInterfaces.DataStore;

/// <summary>
/// Giao diện Repository truy xuất dữ liệu Sản phẩm
/// </summary>
public interface ISanPhamRepository
{
    /// <summary>
    /// Lấy danh sách sản phẩm đang hoạt động phục vụ chọn hàng khi lập hóa đơn
    /// </summary>
    Task<IEnumerable<SanPham>> LayDanhSachSanPhamHoatDongAsync();

    /// <summary>
    /// Lấy toàn bộ danh sách sản phẩm (cả đang bán và ngừng bán) phục vụ trang quản trị sản phẩm
    /// </summary>
    Task<IEnumerable<SanPham>> LayDanhSachTatCaSanPhamAsync();

    /// <summary>
    /// Lấy chi tiết thông tin một sản phẩm theo Id
    /// </summary>
    Task<SanPham?> LaySanPhamTheoIdAsync(int sanPhamId);

    /// <summary>
    /// Kiểm tra xem mã sản phẩm đã tồn tại hay chưa
    /// </summary>
    Task<bool> KiemTraTrungMaSanPhamAsync(string maSanPham);

    /// <summary>
    /// Thêm sản phẩm mới vào hệ thống
    /// </summary>
    Task<TaoSanPhamResult> ThemSanPhamAsync(TaoSanPhamRequest request);

    /// <summary>
    /// Cập nhật thông tin sản phẩm (giữ nguyên SanPhamId, MaSanPham, NgayTao và SoLuongTon)
    /// </summary>
    Task<CapNhatSanPhamResult> CapNhatSanPhamAsync(CapNhatSanPhamRequest request);

    /// <summary>
    /// Thay đổi trạng thái kinh doanh của sản phẩm (Ngừng bán / Mở bán lại), không DELETE
    /// </summary>
    Task<DoiTrangThaiSanPhamResult> DoiTrangThaiSanPhamAsync(int sanPhamId, bool trangThaiMoi);

    /// <summary>
    /// Lấy mã sản phẩm tiếp theo dự kiến dạng SPxxx (không chèn dữ liệu)
    /// </summary>
    Task<string> LayMaSanPhamTiepTheoAsync();
}
