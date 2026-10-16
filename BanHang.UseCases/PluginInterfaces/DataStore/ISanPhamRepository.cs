using BanHang.CoreBusiness;

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
}
