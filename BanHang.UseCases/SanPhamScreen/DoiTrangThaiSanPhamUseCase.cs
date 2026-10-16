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

    public DoiTrangThaiSanPhamUseCase(ISanPhamRepository sanPhamRepository)
    {
        _sanPhamRepository = sanPhamRepository ?? throw new ArgumentNullException(nameof(sanPhamRepository));
    }

    public async Task<DoiTrangThaiSanPhamResult> ExecuteAsync(int sanPhamId, bool trangThaiMoi)
    {
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
