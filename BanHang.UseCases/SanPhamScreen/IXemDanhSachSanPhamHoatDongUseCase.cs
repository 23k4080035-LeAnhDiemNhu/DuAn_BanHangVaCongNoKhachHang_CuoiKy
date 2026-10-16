using BanHang.CoreBusiness;

namespace BanHang.UseCases.SanPhamScreen;

/// <summary>
/// Giao diện Use Case đọc danh sách sản phẩm đang hoạt động phục vụ lập hóa đơn
/// </summary>
public interface IXemDanhSachSanPhamHoatDongUseCase
{
    Task<IEnumerable<SanPham>> ExecuteAsync();
}
