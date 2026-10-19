using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.HoaDonScreen;

/// <summary>
/// Triển khai Use Case tạo hóa đơn bán chịu kiêm kiểm tra hạn mức tín dụng theo Clean Architecture
/// </summary>
public class TaoHoaDonBanChiuUseCase : ITaoHoaDonBanChiuUseCase
{
    private readonly IHoaDonRepository _hoaDonRepository;
    private readonly Common.ICurrentUserService _currentUserService;

    public TaoHoaDonBanChiuUseCase(
        IHoaDonRepository hoaDonRepository,
        Common.ICurrentUserService currentUserService)
    {
        _hoaDonRepository = hoaDonRepository ?? throw new ArgumentNullException(nameof(hoaDonRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<string> LayMaHoaDonTiepTheoAsync()
    {
        await Common.AuthorizeHelper.RequireRoleAsync(_currentUserService, CoreBusiness.VaiTroConstants.NhanVienBanHang);
        return await _hoaDonRepository.LayMaHoaDonTiepTheoAsync();
    }

    public async Task<TaoHoaDonResult> ExecuteAsync(TaoHoaDonRequest request)
    {
        await Common.AuthorizeHelper.RequireRoleAsync(_currentUserService, CoreBusiness.VaiTroConstants.NhanVienBanHang);

        // 1. Kiểm tra đối tượng yêu cầu
        if (request == null)
        {
            return new TaoHoaDonResult
            {
                ThanhCong = false,
                ThongBao = "Yêu cầu tạo hóa đơn không hợp lệ (null)."
            };
        }

        // 2. Kiểm tra Mã hóa đơn (mã có thể để trống hoặc gửi mã dự kiến; máy chủ quyết định mã thực tế trong transaction)
        if (!string.IsNullOrWhiteSpace(request.MaHoaDon))
        {
            request.MaHoaDon = request.MaHoaDon.Trim();
            if (request.MaHoaDon.Length > 20)
            {
                return new TaoHoaDonResult
                {
                    ThanhCong = false,
                    ThongBao = "Mã hóa đơn tối đa 20 ký tự."
                };
            }
        }

        // 3. Kiểm tra thông tin khách hàng
        if (request.KhachHangId <= 0)
        {
            return new TaoHoaDonResult
            {
                ThanhCong = false,
                ThongBao = "Vui lòng chọn khách hàng."
            };
        }

        // 4. Kiểm tra ngày lập và hạn thanh toán: bắt buộc chọn và hạn thanh toán không trước ngày lập
        if (!request.HanThanhToan.HasValue || request.HanThanhToan.Value == default)
        {
            return new TaoHoaDonResult
            {
                ThanhCong = false,
                ThongBao = "Vui lòng chọn hạn thanh toán cho hóa đơn."
            };
        }

        if (request.HanThanhToan.Value.Date < request.NgayLap.Date)
        {
            return new TaoHoaDonResult
            {
                ThanhCong = false,
                ThongBao = "Hạn thanh toán không được trước ngày lập hóa đơn."
            };
        }

        if (request.HanThanhToan.Value.Date > new DateTime(9999, 12, 31))
        {
            return new TaoHoaDonResult
            {
                ThanhCong = false,
                ThongBao = "Hạn thanh toán vượt quá phạm vi ngày hợp lệ (tối đa 31/12/9999)."
            };
        }

        // 5. Kiểm tra dòng hàng: ít nhất một dòng sản phẩm
        if (request.ChiTietHoaDons == null || !request.ChiTietHoaDons.Any())
        {
            return new TaoHoaDonResult
            {
                ThanhCong = false,
                ThongBao = "Hóa đơn phải có ít nhất một dòng sản phẩm."
            };
        }

        // 6. Kiểm tra tính hợp lệ của từng dòng sản phẩm
        for (int i = 0; i < request.ChiTietHoaDons.Count; i++)
        {
            var ct = request.ChiTietHoaDons[i];
            if (ct.SanPhamId <= 0)
            {
                return new TaoHoaDonResult
                {
                    ThanhCong = false,
                    ThongBao = $"Dòng thứ {i + 1}: Vui lòng chọn sản phẩm hợp lệ."
                };
            }

            if (ct.SoLuong <= 0)
            {
                return new TaoHoaDonResult
                {
                    ThanhCong = false,
                    ThongBao = $"Dòng thứ {i + 1}: Số lượng sản phẩm phải là số nguyên dương lớn hơn 0."
                };
            }
        }

        // 7. Kiểm tra trùng lặp sản phẩm giữa các dòng
        var trungSanPham = request.ChiTietHoaDons
            .GroupBy(c => c.SanPhamId)
            .FirstOrDefault(g => g.Count() > 1);

        if (trungSanPham != null)
        {
            return new TaoHoaDonResult
            {
                ThanhCong = false,
                ThongBao = "Phát hiện sản phẩm bị chọn trùng trên nhiều dòng. Vui lòng gộp số lượng sản phẩm vào một dòng."
            };
        }

        // 8. Chuyển xuống tầng DataStore để thực hiện lưu nguyên tử và kiểm tra hạn mức với khóa chống xung đột đồng thời
        return await _hoaDonRepository.TaoHoaDonBanChiuAsync(request);
    }
}
