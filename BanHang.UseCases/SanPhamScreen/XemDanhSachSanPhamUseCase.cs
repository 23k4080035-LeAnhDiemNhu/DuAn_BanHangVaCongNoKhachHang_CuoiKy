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

    public XemDanhSachSanPhamUseCase(ISanPhamRepository sanPhamRepository)
    {
        _sanPhamRepository = sanPhamRepository ?? throw new System.ArgumentNullException(nameof(sanPhamRepository));
    }

    public async Task<IEnumerable<SanPham>> ExecuteAsync()
    {
        return await _sanPhamRepository.LayDanhSachTatCaSanPhamAsync();
    }
}
