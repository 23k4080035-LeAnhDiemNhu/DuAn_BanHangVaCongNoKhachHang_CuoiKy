using System;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.PluginInterfaces.Security;

namespace BanHang.UseCases.Security;

public class DangNhapRequest
{
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
}

public class DangNhapResult
{
    public bool ThanhCong { get; set; }
    public string ThongBao { get; set; } = string.Empty;
    public bool BiKhoaDoThuSai { get; set; }
    public NguoiDung? NguoiDung { get; set; }
    public string? DiagnosticReason { get; set; }
}

public interface IDangNhapUseCase
{
    Task<DangNhapResult> ExecuteAsync(DangNhapRequest request);
}

public class DangNhapUseCase : IDangNhapUseCase
{
    private readonly INguoiDungRepository _nguoiDungRepository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ILoginRateLimiter _rateLimiter;

    public DangNhapUseCase(
        INguoiDungRepository nguoiDungRepository,
        IPasswordHasherService passwordHasher,
        ILoginRateLimiter rateLimiter)
    {
        _nguoiDungRepository = nguoiDungRepository ?? throw new ArgumentNullException(nameof(nguoiDungRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
    }

    public async Task<DangNhapResult> ExecuteAsync(DangNhapRequest request)
    {
        if (request == null)
        {
            return new DangNhapResult
            {
                ThanhCong = false,
                ThongBao = "Yêu cầu đăng nhập không hợp lệ.",
                DiagnosticReason = "RequestNull"
            };
        }

        string tenDangNhap = request.TenDangNhap?.Trim().ToLowerInvariant() ?? string.Empty;
        string matKhau = request.MatKhau ?? string.Empty;

        if (string.IsNullOrWhiteSpace(tenDangNhap) || string.IsNullOrWhiteSpace(matKhau))
        {
            return new DangNhapResult
            {
                ThanhCong = false,
                ThongBao = "Tên đăng nhập và mật khẩu không được để trống.",
                DiagnosticReason = "TenDangNhapHoacMatKhauRong"
            };
        }

        // 1. Kiểm tra giới hạn số lần thử sai
        if (_rateLimiter.IsLockedOut(tenDangNhap))
        {
            return new DangNhapResult
            {
                ThanhCong = false,
                BiKhoaDoThuSai = true,
                ThongBao = "Bạn đã thử đăng nhập sai quá nhiều lần. Vui lòng thử lại sau 15 phút.",
                DiagnosticReason = "BiKhoaDoThuSaiQua5Lan_15Phut"
            };
        }

        // 2. Tra cứu tài khoản từ cơ sở dữ liệu
        NguoiDung? nguoiDung;
        try
        {
            nguoiDung = await _nguoiDungRepository.LayTheoTenDangNhapAsync(tenDangNhap);
        }
        catch (Exception ex)
        {
            return new DangNhapResult
            {
                ThanhCong = false,
                ThongBao = "Không thể kết nối cơ sở dữ liệu để xác thực tài khoản.",
                DiagnosticReason = $"LoiTruyVanDatabase: {ex.GetType().Name} - {ex.Message}"
            };
        }

        // 3. Kiểm tra: Tài khoản không tồn tại, bị vô hiệu hóa hoặc sai mật khẩu đều trả thông báo chung trên giao diện
        if (nguoiDung == null)
        {
            _rateLimiter.RecordFailedAttempt(tenDangNhap);
            return new DangNhapResult
            {
                ThanhCong = false,
                ThongBao = "Tên đăng nhập hoặc mật khẩu không chính xác.",
                DiagnosticReason = "TaiKhoanKhongTonTaiTrongDatabase"
            };
        }

        if (!nguoiDung.TrangThai)
        {
            _rateLimiter.RecordFailedAttempt(tenDangNhap);
            return new DangNhapResult
            {
                ThanhCong = false,
                ThongBao = "Tên đăng nhập hoặc mật khẩu không chính xác.",
                DiagnosticReason = "TaiKhoanBiTamKhoa (TrangThai=0)"
            };
        }

        if (!_passwordHasher.VerifyPassword(nguoiDung.MatKhauHash, matKhau))
        {
            _rateLimiter.RecordFailedAttempt(tenDangNhap);
            return new DangNhapResult
            {
                ThanhCong = false,
                ThongBao = "Tên đăng nhập hoặc mật khẩu không chính xác.",
                DiagnosticReason = "SaiMatKhau (PasswordHasherVerifyFailed)"
            };
        }

        // 4. Đăng nhập thành công -> Xóa bộ đếm thử sai
        _rateLimiter.ResetAttempts(tenDangNhap);
        return new DangNhapResult
        {
            ThanhCong = true,
            ThongBao = "Đăng nhập thành công.",
            DiagnosticReason = "ThanhCong",
            NguoiDung = nguoiDung
        };
    }
}
