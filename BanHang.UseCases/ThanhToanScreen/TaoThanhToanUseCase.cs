using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.ThanhToanScreen;

/// <summary>
/// Triển khai Use Case lập phiếu thu tiền và phân bổ FIFO theo Clean Architecture
/// </summary>
public class TaoThanhToanUseCase : ITaoThanhToanUseCase
{
    private readonly IThanhToanRepository _thanhToanRepository;

    public TaoThanhToanUseCase(IThanhToanRepository thanhToanRepository)
    {
        _thanhToanRepository = thanhToanRepository ?? throw new ArgumentNullException(nameof(thanhToanRepository));
    }

    public async Task<TaoThanhToanResult> ExecuteAsync(TaoThanhToanRequest request)
    {
        if (request == null)
        {
            return new TaoThanhToanResult
            {
                ThanhCong = false,
                ThongBao = "Yêu cầu tạo phiếu thu không hợp lệ (null)."
            };
        }

        if (string.IsNullOrWhiteSpace(request.MaPhieuThu))
        {
            return new TaoThanhToanResult
            {
                ThanhCong = false,
                ThongBao = "Vui lòng nhập mã phiếu thu."
            };
        }

        request.MaPhieuThu = request.MaPhieuThu.Trim();
        if (request.MaPhieuThu.Length > 20)
        {
            return new TaoThanhToanResult
            {
                ThanhCong = false,
                ThongBao = "Mã phiếu thu tối đa 20 ký tự."
            };
        }

        if (request.KhachHangId <= 0)
        {
            return new TaoThanhToanResult
            {
                ThanhCong = false,
                ThongBao = "Vui lòng chọn khách hàng nộp tiền."
            };
        }

        if (request.SoTien <= 0)
        {
            return new TaoThanhToanResult
            {
                ThanhCong = false,
                ThongBao = "Số tiền thanh toán phải lớn hơn 0."
            };
        }

        if (decimal.Round(request.SoTien, 2) != request.SoTien)
        {
            return new TaoThanhToanResult
            {
                ThanhCong = false,
                ThongBao = "Số tiền thanh toán chỉ được phép có tối đa 2 chữ số thập phân."
            };
        }

        if (string.IsNullOrWhiteSpace(request.PhuongThuc))
        {
            return new TaoThanhToanResult
            {
                ThanhCong = false,
                ThongBao = "Vui lòng chọn phương thức thanh toán."
            };
        }

        request.PhuongThuc = request.PhuongThuc.Trim();
        if (request.PhuongThuc.Length > 50)
        {
            return new TaoThanhToanResult
            {
                ThanhCong = false,
                ThongBao = "Phương thức thanh toán tối đa 50 ký tự."
            };
        }

        if (request.GhiChu != null && request.GhiChu.Length > 500)
        {
            return new TaoThanhToanResult
            {
                ThanhCong = false,
                ThongBao = "Ghi chú tối đa 500 ký tự."
            };
        }

        // Chuyển xuống tầng DataStore để thực hiện nguyên tử trong Transaction có khóa khách hàng
        return await _thanhToanRepository.GhiNhanThanhToanAsync(request);
    }
}
