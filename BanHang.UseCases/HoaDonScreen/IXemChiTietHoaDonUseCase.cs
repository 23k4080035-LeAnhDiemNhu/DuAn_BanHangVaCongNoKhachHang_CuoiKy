namespace BanHang.UseCases.HoaDonScreen;

/// <summary>
/// Giao diện Use Case xem chi tiết một hóa đơn
/// </summary>
public interface IXemChiTietHoaDonUseCase
{
    /// <summary>
    /// Thực thi lấy chi tiết hóa đơn theo HoaDonId
    /// Yêu cầu vai trò NhanVienBanHang hoặc KeToanCongNo
    /// </summary>
    Task<ChiTietHoaDonDto?> ExecuteAsync(int hoaDonId);
}
