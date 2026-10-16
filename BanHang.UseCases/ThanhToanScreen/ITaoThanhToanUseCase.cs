namespace BanHang.UseCases.ThanhToanScreen;

/// <summary>
/// Giao diện Use Case lập phiếu thu tiền và phân bổ FIFO
/// </summary>
public interface ITaoThanhToanUseCase
{
    Task<TaoThanhToanResult> ExecuteAsync(TaoThanhToanRequest request);
}
