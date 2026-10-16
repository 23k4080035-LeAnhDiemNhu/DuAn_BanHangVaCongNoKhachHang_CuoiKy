namespace BanHang.UseCases.HoaDonScreen;

/// <summary>
/// Giao diện Use Case đọc danh sách hóa đơn
/// </summary>
public interface IXemDanhSachHoaDonUseCase
{
    Task<IEnumerable<HoaDonListItemDto>> ExecuteAsync();
}
