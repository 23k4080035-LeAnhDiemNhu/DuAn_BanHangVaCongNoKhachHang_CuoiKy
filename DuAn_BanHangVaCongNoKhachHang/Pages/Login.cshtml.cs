using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using BanHang.UseCases.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using DuAn_BanHangVaCongNoKhachHang.Services;
using Microsoft.Extensions.Logging;

namespace DuAn_BanHangVaCongNoKhachHang.Pages;

[AllowAnonymous]
[ValidateAntiForgeryToken]
public class LoginModel : PageModel
{
    private readonly IDangNhapUseCase _dangNhapUseCase;
    private readonly ILogger<LoginModel>? _logger;

    public LoginModel(IDangNhapUseCase dangNhapUseCase, ILogger<LoginModel>? logger = null)
    {
        _dangNhapUseCase = dangNhapUseCase ?? throw new ArgumentNullException(nameof(dangNhapUseCase));
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ThongBaoLoi { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ phiên đăng nhập")]
        public bool RememberMe { get; set; }
    }

    public IActionResult OnGet()
    {
        AuthDiagnosticLogger.Log(_logger, $"[AUTH-DIAG] GET /login | IsAuthenticated={User.Identity?.IsAuthenticated == true} | User='{User.Identity?.Name}' | ReturnUrl='{ReturnUrl}'");

        // Nếu đã đăng nhập thì chuyển hướng về trang đích hợp lệ (mặc định /hoa-don)
        if (User.Identity?.IsAuthenticated == true)
        {
            var destination = NormalizeReturnUrl(ReturnUrl);
            AuthDiagnosticLogger.Log(_logger, $"[AUTH-DIAG] GET /login: Người dùng đã đăng nhập, tự động chuyển hướng đến '{destination}'");
            return LocalRedirect(destination);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        string corrId = Guid.NewGuid().ToString("N")[..8];
        string returnUrl = NormalizeReturnUrl(ReturnUrl);

        AuthDiagnosticLogger.Log(_logger, $"[AUTH-DIAG] [{corrId}] POST /login nhận request | TenDangNhap='{Input.TenDangNhap}' | ReturnUrl='{ReturnUrl}' | ModelState.IsValid={ModelState.IsValid}");

        if (!ModelState.IsValid)
        {
            AuthDiagnosticLogger.Log(_logger, $"[AUTH-DIAG] [{corrId}] ModelState không hợp lệ.");
            return Page();
        }

        var request = new DangNhapRequest
        {
            TenDangNhap = Input.TenDangNhap,
            MatKhau = Input.MatKhau
        };

        var result = await _dangNhapUseCase.ExecuteAsync(request);
        if (!result.ThanhCong || result.NguoiDung == null)
        {
            AuthDiagnosticLogger.Log(_logger, $"[AUTH-DIAG] [{corrId}] Đăng nhập THẤT BẠI | NguyenNhan='{result.DiagnosticReason}' | ThongBaoHienThi='{result.ThongBao}'");
            ThongBaoLoi = result.ThongBao;
            ModelState.AddModelError(string.Empty, result.ThongBao);
            return Page();
        }

        AuthDiagnosticLogger.Log(_logger, $"[AUTH-DIAG] [{corrId}] Đăng nhập THÀNH CÔNG | User='{result.NguoiDung.TenDangNhap}' | VaiTro='{result.NguoiDung.VaiTro}' | Tiến hành SignInAsync...");

        var expiresUtc = DateTimeOffset.UtcNow.AddHours(8);

        // Tạo Claims danh tính người dùng (tuyệt đối không lưu mật khẩu hoặc hash)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.NguoiDung.NguoiDungId.ToString()),
            new Claim(ClaimTypes.Name, result.NguoiDung.TenDangNhap),
            new Claim("HoTen", result.NguoiDung.HoTen),
            new Claim(ClaimTypes.Role, result.NguoiDung.VaiTro),
            new Claim("AuthExpiresUtc", expiresUtc.ToUnixTimeSeconds().ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = Input.RememberMe,
            ExpiresUtc = expiresUtc
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        AuthDiagnosticLogger.Log(_logger, $"[AUTH-DIAG] [{corrId}] HttpContext.SignInAsync đã hoàn tất | UrlChuyenHuong='{returnUrl}' | Chuyển hướng LocalRedirect");

        // Chuyển hướng an toàn sau đăng nhập
        return LocalRedirect(returnUrl);
    }

    public string NormalizeReturnUrl(string? returnUrl)
    {
        return NormalizeReturnUrl(Url, returnUrl);
    }

    public static string NormalizeReturnUrl(IUrlHelper? urlHelper, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || urlHelper == null || !urlHelper.IsLocalUrl(returnUrl))
        {
            return "/hoa-don";
        }

        var trimmed = returnUrl.Trim();
        if (trimmed.Equals("/", StringComparison.Ordinal)
            || trimmed.Equals("/login", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("/login?", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("/logout", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("/logout?", StringComparison.OrdinalIgnoreCase))
        {
            return "/hoa-don";
        }

        return trimmed;
    }
}
