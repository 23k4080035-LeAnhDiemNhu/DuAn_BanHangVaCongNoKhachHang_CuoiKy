using System;
using System.Threading.Tasks;
using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.SanPhamScreen;

public interface IDoiTrangThaiSanPhamUseCase
{
    Task<DoiTrangThaiSanPhamResult> ExecuteAsync(int sanPhamId, bool trangThaiMoi);
}

public class DoiTrangThaiSanPhamUseCase : IDoiTrangThaiSanPhamUseCase
{
    private readonly ISanPhamRepository _sanPhamRepository;
    private readonly Common.ICurrentUserService _currentUserService;

    public DoiTrangThaiSanPhamUseCase(
        ISanPhamRepository sanPhamRepository,
        Common.ICurrentUserService currentUserService)
    {
        _sanPhamRepository = sanPhamRepository ?? throw new ArgumentNullException(nameof(sanPhamRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<DoiTrangThaiSanPhamResult> ExecuteAsync(int sanPhamId, bool trangThaiMoi)
    {
        await Common.AuthorizeHelper.RequireRoleAsync(_currentUserService, CoreBusiness.VaiTroConstants.KeToanCongNo);
        if (sanPhamId <= 0)
        {
            return new DoiTrangThaiSanPhamResult
            {
                ThanhCong = false,
                ThongBao = "Mã định danh sản phẩm không hợp lệ."
            };
        }

        return await _sanPhamRepository.DoiTrangThaiSanPhamAsync(sanPhamId, trangThaiMoi);
    }
}
