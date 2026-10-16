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

    public XemDanhSachKhachHangUseCase(IKhachHangRepository khachHangRepository)
    {
        _khachHangRepository = khachHangRepository ?? throw new ArgumentNullException(nameof(khachHangRepository));
    }

    public async Task<IEnumerable<KhachHang>> ExecuteAsync()
    {
        return await _khachHangRepository.LayDanhSachKhachHangAsync();
    }
}
