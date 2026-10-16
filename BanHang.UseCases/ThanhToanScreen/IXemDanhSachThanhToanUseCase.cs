namespace BanHang.UseCases.ThanhToanScreen;

/// <summary>
/// Giao diện Use Case xem danh sách phiếu thu tiền
/// </summary>
public interface IXemDanhSachThanhToanUseCase
{
    Task<IEnumerable<ThanhToanListItemDto>> ExecuteAsync();
}
