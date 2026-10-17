using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.ThanhToanScreen;

/// <summary>
/// Triển khai Use Case xem trước kết quả phân bổ thanh toán FIFO
/// </summary>
public class XemTruocPhanBoFifoUseCase : IXemTruocPhanBoFifoUseCase
{
    private readonly IThanhToanRepository _thanhToanRepository;
    private readonly Common.ICurrentUserService _currentUserService;

    public XemTruocPhanBoFifoUseCase(
        IThanhToanRepository thanhToanRepository,
        Common.ICurrentUserService currentUserService)
    {
        _thanhToanRepository = thanhToanRepository ?? throw new ArgumentNullException(nameof(thanhToanRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<XemTruocPhanBoResult> ExecuteAsync(XemTruocPhanBoRequest request)
    {
        await Common.AuthorizeHelper.RequireRoleAsync(_currentUserService, CoreBusiness.VaiTroConstants.KeToanCongNo);
        if (request == null)
        {
            return new XemTruocPhanBoResult
            {
                ThanhCong = false,
                ThongBao = "Yêu cầu xem trước không hợp lệ."
            };
        }

        if (request.KhachHangId <= 0)
        {
            return new XemTruocPhanBoResult
            {
                ThanhCong = false,
                ThongBao = "Vui lòng chọn khách hàng."
            };
        }

        if (request.SoTien <= 0)
        {
            return new XemTruocPhanBoResult
            {
                ThanhCong = false,
                ThongBao = "Số tiền thanh toán phải lớn hơn 0."
            };
        }

        if (decimal.Round(request.SoTien, 2) != request.SoTien)
        {
            return new XemTruocPhanBoResult
            {
                ThanhCong = false,
                ThongBao = "Số tiền thanh toán tối đa 2 chữ số thập phân."
            };
        }

        // Đọc danh sách hóa đơn còn nợ của khách hàng
        var hoaDons = (await _thanhToanRepository.LayDanhSachHoaDonConNoTheoKhachHangAsync(request.KhachHangId)).ToList();
        decimal tongNo = hoaDons.Sum(h => h.ConLai);

        // Chạy thuật toán FIFO thuần túy trong CoreBusiness
        var ketQuaFifo = ThuatToanFIFO.PhanBo(hoaDons, request.SoTien, request.NgayThanhToan);

        return new XemTruocPhanBoResult
        {
            ThanhCong = true,
            TongCongNoHienTai = tongNo,
            KetQuaFifo = ketQuaFifo
        };
    }
}
