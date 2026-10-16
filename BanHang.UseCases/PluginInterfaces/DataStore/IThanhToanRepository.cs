using BanHang.CoreBusiness;
using BanHang.UseCases.ThanhToanScreen;

namespace BanHang.UseCases.PluginInterfaces.DataStore;

/// <summary>
/// Giao diện Repository truy xuất và thao tác dữ liệu Phiếu thu & Phân bổ thanh toán
/// </summary>
public interface IThanhToanRepository
{
    /// <summary>
    /// Lấy danh sách toàn bộ phiếu thu kèm thông tin khách hàng, số tiền đã/chưa phân bổ
    /// </summary>
    Task<IEnumerable<ThanhToanListItemDto>> LayDanhSachThanhToanAsync();

    /// <summary>
    /// Lấy danh sách hóa đơn còn nợ của một khách hàng (ConLai > 0) sắp xếp theo thứ tự FIFO (NgayLap ASC, HoaDonId ASC)
    /// </summary>
    Task<IEnumerable<HoaDonNoModel>> LayDanhSachHoaDonConNoTheoKhachHangAsync(int khachHangId);

    /// <summary>
    /// Ghi nhận phiếu thu và thực hiện phân bổ thanh toán FIFO trong một transaction nguyên tử với khóa chống xung đột đồng thời
    /// </summary>
    Task<TaoThanhToanResult> GhiNhanThanhToanAsync(TaoThanhToanRequest request);
}
