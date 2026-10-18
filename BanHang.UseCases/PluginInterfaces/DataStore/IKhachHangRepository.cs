using BanHang.CoreBusiness;
using BanHang.UseCases.KhachHangScreen;

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

    /// <summary>
    /// Kiểm tra trùng mã khách hàng (hỗ trợ loại trừ ID khi cập nhật)
    /// </summary>
    Task<bool> KiemTraTrungMaKhachHangAsync(string maKhachHang, int? loaiTruKhachHangId = null);

    /// <summary>
    /// Lấy số dư công nợ hiện tại của khách hàng
    /// </summary>
    Task<decimal> LayCongNoHienTaiCuaKhachHangAsync(int khachHangId);

    /// <summary>
    /// Thêm mới một khách hàng
    /// </summary>
    Task<TaoKhachHangResult> ThemKhachHangAsync(TaoKhachHangRequest request);

    /// <summary>
    /// Cập nhật thông tin khách hàng có điều phối khóa UPDLOCK, HOLDLOCK
    /// </summary>
    Task<CapNhatKhachHangResult> CapNhatKhachHangAsync(CapNhatKhachHangRequest request);

    /// <summary>
    /// Thay đổi trạng thái hoạt động (Tạm khóa / Mở lại)
    /// </summary>
    Task<DoiTrangThaiKhachHangResult> DoiTrangThaiKhachHangAsync(int khachHangId, bool trangThaiMoi);

    /// <summary>
    /// Lấy mã khách hàng tiếp theo dự kiến dạng KHxxx (không chèn dữ liệu)
    /// </summary>
    Task<string> LayMaKhachHangTiepTheoAsync();
}
