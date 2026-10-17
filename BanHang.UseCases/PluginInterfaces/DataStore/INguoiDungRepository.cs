using System.Threading.Tasks;
using BanHang.CoreBusiness;

namespace BanHang.UseCases.PluginInterfaces.DataStore;

/// <summary>
/// Giao diện Repository truy xuất dữ liệu tài khoản người dùng
/// </summary>
public interface INguoiDungRepository
{
    /// <summary>
    /// Lấy thông tin tài khoản người dùng theo tên đăng nhập
    /// </summary>
    Task<NguoiDung?> LayTheoTenDangNhapAsync(string tenDangNhap);

    /// <summary>
    /// Kiểm tra xem tên đăng nhập đã tồn tại trong cơ sở dữ liệu chưa
    /// </summary>
    Task<bool> TonTaiTenDangNhapAsync(string tenDangNhap);

    /// <summary>
    /// Thêm mới tài khoản người dùng (mật khẩu đã băm)
    /// </summary>
    Task<int> TaoNguoiDungAsync(NguoiDung nguoiDung);

    /// <summary>
    /// Đếm tổng số lượng người dùng hiện có trong hệ thống
    /// </summary>
    Task<int> DemSoLuongNguoiDungAsync();
}
