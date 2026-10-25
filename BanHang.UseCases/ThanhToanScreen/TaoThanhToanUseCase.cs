using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.ThanhToanScreen;

/// <summary>
/// Triển khai Use Case lập phiếu thu tiền và phân bổ FIFO theo Clean Architecture
/// </summary>
public class TaoThanhToanUseCase : ITaoThanhToanUseCase
{
    private readonly IThanhToanRepository _thanhToanRepository;
    private readonly Common.ICurrentUserService _currentUserService;
    private readonly IKhachHangRepository? _khachHangRepository;
    private readonly IVietQrService _vietQrService;

    public TaoThanhToanUseCase(
        IThanhToanRepository thanhToanRepository,
        Common.ICurrentUserService currentUserService,
        IKhachHangRepository? khachHangRepository = null,
        IVietQrService? vietQrService = null)
    {
        _thanhToanRepository = thanhToanRepository ?? throw new ArgumentNullException(nameof(thanhToanRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _khachHangRepository = khachHangRepository;
        _vietQrService = vietQrService ?? new VietQrService();
    }

    public Task<string> LayMaPhieuThuTiepTheoAsync()
    {
        return _thanhToanRepository.LayMaPhieuThuTiepTheoAsync();
    }

    public async Task<TaoThanhToanResult> ExecuteAsync(TaoThanhToanRequest request)
    {
        await Common.AuthorizeHelper.RequireRoleAsync(_currentUserService, CoreBusiness.VaiTroConstants.KeToanCongNo);
        if (request == null)
        {
            return new TaoThanhToanResult
            {
                ThanhCong = false,
                ThongBao = "Yêu cầu tạo phiếu thu không hợp lệ (null)."
            };
        }

        request.MaPhieuThu = request.MaPhieuThu?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(request.MaPhieuThu) && request.MaPhieuThu.Length > 20)
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

        // Kiểm tra an toàn theo phương thức thanh toán
        bool laChuyenKhoan = string.Equals(request.PhuongThuc, "ChuyenKhoan", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(request.PhuongThuc, "Chuyển khoản", StringComparison.OrdinalIgnoreCase);

        if (laChuyenKhoan)
        {
            var isAuth = await _currentUserService.IsAuthenticatedAsync();
            var tenDangNhap = await _currentUserService.GetTenDangNhapAsync();
            if (!isAuth || string.IsNullOrWhiteSpace(tenDangNhap))
            {
                return new TaoThanhToanResult
                {
                    ThanhCong = false,
                    ThongBao = "Không xác định được danh tính người dùng hiện tại để xác nhận giao dịch chuyển khoản."
                };
            }

            var laKeToan = await _currentUserService.IsInRoleAsync(CoreBusiness.VaiTroConstants.KeToanCongNo);
            if (!laKeToan)
            {
                return new TaoThanhToanResult
                {
                    ThanhCong = false,
                    ThongBao = "Người xác nhận giao dịch chuyển khoản phải là tài khoản có vai trò Kế toán công nợ (KeToanCongNo)."
                };
            }

            if (!request.DaXacNhanTienVao)
            {
                return new TaoThanhToanResult
                {
                    ThanhCong = false,
                    ThongBao = "Kế toán phải kiểm tra tiền đã vào tài khoản và tích chọn xác nhận trước khi lưu phiếu thu chuyển khoản."
                };
            }

            var kiemTraTien = _vietQrService.KiemTraSoTienVietQr(request.SoTien);
            if (!kiemTraTien.HopLe)
            {
                return new TaoThanhToanResult
                {
                    ThanhCong = false,
                    ThongBao = kiemTraTien.Loi ?? "Số tiền thanh toán chuyển khoản không hợp lệ."
                };
            }

            string maKhachHang;
            if (_khachHangRepository != null)
            {
                var kh = await _khachHangRepository.LayKhachHangTheoIdAsync(request.KhachHangId);
                if (kh == null)
                {
                    return new TaoThanhToanResult
                    {
                        ThanhCong = false,
                        ThongBao = "Khách hàng không tồn tại trong hệ thống."
                    };
                }
                maKhachHang = kh.MaKhachHang;
            }
            else
            {
                maKhachHang = $"KH{request.KhachHangId:D3}";
            }

            if (string.IsNullOrWhiteSpace(request.MaPhieuThu))
            {
                request.MaPhieuThu = await _thanhToanRepository.LayMaPhieuThuTiepTheoAsync();
            }

            request.NguoiXacNhan = tenDangNhap.Trim();
            request.ThoiDiemXacNhan = DateTime.Now;
            request.TaiKhoanThuHuong = _vietQrService.Settings.AccountNumber;
            request.NganHangThuHuong = _vietQrService.Settings.BankId;
            request.NoiDungChuyenKhoan = _vietQrService.TaoNoiDungChuyenKhoan(request.MaPhieuThu, maKhachHang);
        }
        else
        {
            request.DaXacNhanTienVao = false;
            request.NoiDungChuyenKhoan = null;
            request.TaiKhoanThuHuong = null;
            request.NganHangThuHuong = null;
            request.ThoiDiemXacNhan = null;
            request.NguoiXacNhan = null;
        }

        return await _thanhToanRepository.GhiNhanThanhToanAsync(request);
    }
}
