using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.ThanhToanScreen;

/// <summary>
/// Triển khai Use Case xem danh sách phiếu thu
/// </summary>
public class XemDanhSachThanhToanUseCase : IXemDanhSachThanhToanUseCase
{
    private readonly IThanhToanRepository _thanhToanRepository;
    private readonly Common.ICurrentUserService _currentUserService;

    public XemDanhSachThanhToanUseCase(
        IThanhToanRepository thanhToanRepository,
        Common.ICurrentUserService currentUserService)
    {
        _thanhToanRepository = thanhToanRepository ?? throw new ArgumentNullException(nameof(thanhToanRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<IEnumerable<ThanhToanListItemDto>> ExecuteAsync()
    {
        await Common.AuthorizeHelper.RequireRoleAsync(_currentUserService, CoreBusiness.VaiTroConstants.KeToanCongNo);
        return await _thanhToanRepository.LayDanhSachThanhToanAsync();
    }
}
