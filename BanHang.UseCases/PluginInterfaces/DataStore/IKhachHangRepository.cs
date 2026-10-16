using BanHang.CoreBusiness;

namespace BanHang.UseCases.PluginInterfaces.DataStore;

/// <summary>
/// Giao diện Repository truy xuất dữ liệu Khách hàng
/// </summary>
public interface IKhachHangRepository
{
    /// <summary>
    /// Lấy danh sách toàn bộ khách hàng kèm số dư công nợ hiện tại
    /// </summary>
    Task<IEnumerable<KhachHang>> LayDanhSachKhachHangAsync();

    /// <summary>
    /// Lấy thông tin một khách hàng theo Id kèm số dư công nợ hiện tại
    /// </summary>
    Task<KhachHang?> LayKhachHangTheoIdAsync(int khachHangId);
}
