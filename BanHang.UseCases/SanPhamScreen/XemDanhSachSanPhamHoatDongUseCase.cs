using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.SanPhamScreen;

/// <summary>
/// Use Case xử lý lấy danh sách sản phẩm đang kinh doanh
/// Nhận ISanPhamRepository qua constructor, độc lập hoàn toàn với Dapper
/// </summary>
public class XemDanhSachSanPhamHoatDongUseCase : IXemDanhSachSanPhamHoatDongUseCase
{
    private readonly ISanPhamRepository _sanPhamRepository;

    public XemDanhSachSanPhamHoatDongUseCase(ISanPhamRepository sanPhamRepository)
    {
        _sanPhamRepository = sanPhamRepository ?? throw new ArgumentNullException(nameof(sanPhamRepository));
    }

    public async Task<IEnumerable<SanPham>> ExecuteAsync()
    {
        return await _sanPhamRepository.LayDanhSachSanPhamHoatDongAsync();
    }
}
