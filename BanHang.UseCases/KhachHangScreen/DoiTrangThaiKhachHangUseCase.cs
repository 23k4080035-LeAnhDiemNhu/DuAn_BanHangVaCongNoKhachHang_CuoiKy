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

    public DoiTrangThaiKhachHangUseCase(IKhachHangRepository khachHangRepo)
    {
        _khachHangRepo = khachHangRepo;
    }

    public async Task<DoiTrangThaiKhachHangResult> ExecuteAsync(int khachHangId, bool trangThaiMoi)
    {
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
