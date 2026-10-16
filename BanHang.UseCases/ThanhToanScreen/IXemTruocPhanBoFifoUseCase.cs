namespace BanHang.UseCases.ThanhToanScreen;

/// <summary>
/// Giao diện Use Case xem trước kết quả phân bổ FIFO trước khi lưu
/// </summary>
public interface IXemTruocPhanBoFifoUseCase
{
    Task<XemTruocPhanBoResult> ExecuteAsync(XemTruocPhanBoRequest request);
}
