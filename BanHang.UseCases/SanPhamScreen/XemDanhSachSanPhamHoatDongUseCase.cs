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
    private readonly Common.ICurrentUserService _currentUserService;

    public XemDanhSachSanPhamHoatDongUseCase(
        ISanPhamRepository sanPhamRepository,
        Common.ICurrentUserService currentUserService)
    {
        _sanPhamRepository = sanPhamRepository ?? throw new ArgumentNullException(nameof(sanPhamRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<IEnumerable<SanPham>> ExecuteAsync()
    {
        await Common.AuthorizeHelper.RequireRoleAsync(_currentUserService, VaiTroConstants.NhanVienBanHang, VaiTroConstants.KeToanCongNo);
        return await _sanPhamRepository.LayDanhSachSanPhamHoatDongAsync();
    }
}
