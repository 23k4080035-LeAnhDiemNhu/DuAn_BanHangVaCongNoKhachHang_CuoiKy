using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.KhachHangScreen;

/// <summary>
/// Use Case xử lý nghiệp vụ đọc danh sách khách hàng
/// Nhận IKhachHangRepository qua constructor, hoàn toàn độc lập với Dapper hoặc SQL Server.
/// </summary>
public class XemDanhSachKhachHangUseCase : IXemDanhSachKhachHangUseCase
{
    private readonly IKhachHangRepository _khachHangRepository;
    private readonly Common.ICurrentUserService _currentUserService;

    public XemDanhSachKhachHangUseCase(
        IKhachHangRepository khachHangRepository,
        Common.ICurrentUserService currentUserService)
    {
        _khachHangRepository = khachHangRepository ?? throw new ArgumentNullException(nameof(khachHangRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<IEnumerable<KhachHang>> ExecuteAsync()
    {
        await Common.AuthorizeHelper.RequireRoleAsync(_currentUserService, VaiTroConstants.NhanVienBanHang, VaiTroConstants.KeToanCongNo);
        return await _khachHangRepository.LayDanhSachKhachHangAsync();
    }
}
