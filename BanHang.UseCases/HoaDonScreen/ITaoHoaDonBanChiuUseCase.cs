namespace BanHang.UseCases.HoaDonScreen;

/// <summary>
/// Giao diện Use Case tạo hóa đơn bán chịu và kiểm tra hạn mức tín dụng
/// </summary>
public interface ITaoHoaDonBanChiuUseCase
{
    Task<string> LayMaHoaDonTiepTheoAsync();
    Task<TaoHoaDonResult> ExecuteAsync(TaoHoaDonRequest request);
}
