using BanHang.UseCases.HoaDonScreen;

namespace BanHang.UseCases.PluginInterfaces.DataStore;

/// <summary>
/// Giao diện Repository truy xuất dữ liệu Hóa đơn
/// </summary>
public interface IHoaDonRepository
{
    /// <summary>
    /// Lấy danh sách toàn bộ hóa đơn kèm thông tin khách hàng, sắp xếp mới nhất trước
    /// </summary>
    Task<IEnumerable<HoaDonListItemDto>> LayDanhSachHoaDonAsync();

    /// <summary>
    /// Tạo hóa đơn bán chịu mới trong một transaction nguyên tử với khóa chống xung đột đồng thời
    /// </summary>
    Task<TaoHoaDonResult> TaoHoaDonBanChiuAsync(TaoHoaDonRequest request);

    /// <summary>
    /// Lấy chi tiết một hóa đơn theo HoaDonId, gồm thông tin hóa đơn, danh sách dòng hàng và lịch sử phân bổ
    /// Đọc nhất quán từ các tập dữ liệu độc lập, ngăn ngừa hoàn toàn lỗi nhân đôi số tiền
    /// </summary>
    Task<ChiTietHoaDonDto?> LayChiTietHoaDonAsync(int hoaDonId);
}
