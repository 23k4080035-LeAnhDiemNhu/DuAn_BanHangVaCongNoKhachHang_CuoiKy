using System.Threading.Tasks;
using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.KhachHangScreen;

public interface IDoiTrangThaiKhachHangUseCase
{
    Task<DoiTrangThaiKhachHangResult> ExecuteAsync(int khachHangId, bool trangThaiMoi);
}

public class DoiTrangThaiKhachHangUseCase : IDoiTrangThaiKhachHangUseCase
{
    private readonly IKhachHangRepository _khachHangRepo;
    private readonly Common.ICurrentUserService _currentUserService;

    public DoiTrangThaiKhachHangUseCase(
        IKhachHangRepository khachHangRepo,
        Common.ICurrentUserService currentUserService)
    {
        _khachHangRepo = khachHangRepo ?? throw new ArgumentNullException(nameof(khachHangRepo));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<DoiTrangThaiKhachHangResult> ExecuteAsync(int khachHangId, bool trangThaiMoi)
    {
        await Common.AuthorizeHelper.RequireRoleAsync(_currentUserService, CoreBusiness.VaiTroConstants.KeToanCongNo);
        if (khachHangId <= 0)
        {
            return new DoiTrangThaiKhachHangResult
            {
                ThanhCong = false,
                ThongBao = "Mã định danh khách hàng không hợp lệ."
            };
        }

        return await _khachHangRepo.DoiTrangThaiKhachHangAsync(khachHangId, trangThaiMoi);
    }
}
