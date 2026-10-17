using System.Collections.Generic;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.SanPhamScreen;

public interface IXemDanhSachSanPhamUseCase
{
    Task<IEnumerable<SanPham>> ExecuteAsync();
}

public class XemDanhSachSanPhamUseCase : IXemDanhSachSanPhamUseCase
{
    private readonly ISanPhamRepository _sanPhamRepository;
    private readonly Common.ICurrentUserService _currentUserService;

    public XemDanhSachSanPhamUseCase(
        ISanPhamRepository sanPhamRepository,
        Common.ICurrentUserService currentUserService)
    {
        _sanPhamRepository = sanPhamRepository ?? throw new System.ArgumentNullException(nameof(sanPhamRepository));
        _currentUserService = currentUserService ?? throw new System.ArgumentNullException(nameof(currentUserService));
    }

    public async Task<IEnumerable<SanPham>> ExecuteAsync()
    {
        await Common.AuthorizeHelper.RequireRoleAsync(_currentUserService, VaiTroConstants.KeToanCongNo);
        return await _sanPhamRepository.LayDanhSachTatCaSanPhamAsync();
    }
}
