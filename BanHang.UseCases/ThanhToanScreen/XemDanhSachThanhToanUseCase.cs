using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.ThanhToanScreen;

/// <summary>
/// Triển khai Use Case xem danh sách phiếu thu
/// </summary>
public class XemDanhSachThanhToanUseCase : IXemDanhSachThanhToanUseCase
{
    private readonly IThanhToanRepository _thanhToanRepository;

    public XemDanhSachThanhToanUseCase(IThanhToanRepository thanhToanRepository)
    {
        _thanhToanRepository = thanhToanRepository ?? throw new ArgumentNullException(nameof(thanhToanRepository));
    }

    public async Task<IEnumerable<ThanhToanListItemDto>> ExecuteAsync()
    {
        return await _thanhToanRepository.LayDanhSachThanhToanAsync();
    }
}
