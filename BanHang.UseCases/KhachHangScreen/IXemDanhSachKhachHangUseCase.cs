using BanHang.CoreBusiness;

namespace BanHang.UseCases.KhachHangScreen;

/// <summary>
/// Giao diện Use Case đọc danh sách khách hàng
/// </summary>
public interface IXemDanhSachKhachHangUseCase
{
    Task<IEnumerable<KhachHang>> ExecuteAsync();
}
