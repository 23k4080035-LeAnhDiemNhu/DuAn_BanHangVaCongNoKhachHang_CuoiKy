using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.HoaDonScreen;

/// <summary>
/// Use Case xử lý nghiệp vụ đọc danh sách hóa đơn bán hàng
/// Nhận IHoaDonRepository qua constructor, hoàn toàn độc lập với Dapper hoặc SQL Server.
/// </summary>
public class XemDanhSachHoaDonUseCase : IXemDanhSachHoaDonUseCase
{
    private readonly IHoaDonRepository _hoaDonRepository;
    private readonly Common.ICurrentUserService _currentUserService;

    public XemDanhSachHoaDonUseCase(
        IHoaDonRepository hoaDonRepository,
        Common.ICurrentUserService currentUserService)
    {
        _hoaDonRepository = hoaDonRepository ?? throw new ArgumentNullException(nameof(hoaDonRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<IEnumerable<HoaDonListItemDto>> ExecuteAsync()
    {
        await Common.AuthorizeHelper.RequireRoleAsync(_currentUserService, CoreBusiness.VaiTroConstants.NhanVienBanHang, CoreBusiness.VaiTroConstants.KeToanCongNo);
        return await _hoaDonRepository.LayDanhSachHoaDonAsync();
    }
}
