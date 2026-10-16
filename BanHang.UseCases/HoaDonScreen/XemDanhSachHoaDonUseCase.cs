using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.HoaDonScreen;

/// <summary>
/// Use Case xử lý nghiệp vụ đọc danh sách hóa đơn bán hàng
/// Nhận IHoaDonRepository qua constructor, hoàn toàn độc lập với Dapper hoặc SQL Server.
/// </summary>
public class XemDanhSachHoaDonUseCase : IXemDanhSachHoaDonUseCase
{
    private readonly IHoaDonRepository _hoaDonRepository;

    public XemDanhSachHoaDonUseCase(IHoaDonRepository hoaDonRepository)
    {
        _hoaDonRepository = hoaDonRepository ?? throw new ArgumentNullException(nameof(hoaDonRepository));
    }

    public async Task<IEnumerable<HoaDonListItemDto>> ExecuteAsync()
    {
        return await _hoaDonRepository.LayDanhSachHoaDonAsync();
    }
}
