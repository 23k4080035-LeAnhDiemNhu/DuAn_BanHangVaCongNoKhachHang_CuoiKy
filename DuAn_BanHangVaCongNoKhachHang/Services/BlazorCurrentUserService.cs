using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.Common;
using BanHang.UseCases.PluginInterfaces.DataStore;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DuAn_BanHangVaCongNoKhachHang.Services;

/// <summary>
/// Triển khai ICurrentUserService trong ứng dụng Web Blazor Server (Scoped)
/// - Trong Blazor circuit: lấy ClaimsPrincipal duy nhất từ AuthenticationStateProvider,
///   tuyệt đối không dùng HttpContext cũ làm nguồn danh tính lâu dài.
/// - Với Razor Page HTTP: lấy từ HttpContextAccessor.
/// - Xác minh hết hạn phiên (AuthExpiresUtc) trực tiếp để circuit mở không giữ quyền vô hạn.
/// - Xác thực lại TrangThai (bị khóa) và VaiTro (đổi quyền) trực tiếp từ database (fail-closed khi DB lỗi).
/// </summary>
public class BlazorCurrentUserService : ICurrentUserService
{
    private readonly AuthenticationStateProvider? _authStateProvider;
    private readonly INguoiDungRepository? _nguoiDungRepository;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ILogger<BlazorCurrentUserService>? _logger;

    public BlazorCurrentUserService(
        AuthenticationStateProvider? authStateProvider = null,
        INguoiDungRepository? nguoiDungRepository = null,
        IHttpContextAccessor? httpContextAccessor = null,
        ILogger<BlazorCurrentUserService>? logger = null)
    {
        _authStateProvider = authStateProvider;
        _nguoiDungRepository = nguoiDungRepository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private async Task<ClaimsPrincipal?> GetPrincipalAsync()
    {
        // 1. Trong Blazor circuit: CHỈ lấy từ AuthenticationStateProvider, không dùng HttpContext cũ
        if (_authStateProvider != null)
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                return authState?.User;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Lỗi khi lấy AuthenticationState từ circuit.");
                return null;
            }
        }

        // 2. Dự phòng trong ngữ cảnh thuần HTTP (Razor Pages Login/Logout)
        return _httpContextAccessor?.HttpContext?.User;
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ của thời hạn phiên từ claim AuthExpiresUtc.
    /// Bắt buộc claim AuthExpiresUtc phải có, parse hợp lệ theo UTC và chưa hết hạn.
    /// Thiếu, sai định dạng, hoặc hết hạn đều từ chối xác thực (fail-closed).
    /// </summary>
    private bool IsSessionActiveAndValid(ClaimsPrincipal principal)
    {
        var expClaim = principal.FindFirst("AuthExpiresUtc")?.Value;
        if (string.IsNullOrWhiteSpace(expClaim))
        {
            _logger?.LogWarning("Từ chối xác thực: Thiếu claim bắt buộc 'AuthExpiresUtc' (fail-closed).");
            return false;
        }

        DateTimeOffset expirationUtc;

        // 1. Thử parse dạng Unix timestamp (số nguyên giây)
        if (long.TryParse(expClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixSeconds))
        {
            if (unixSeconds <= 0)
            {
                _logger?.LogWarning("Từ chối xác thực: Claim 'AuthExpiresUtc' có giá trị số không hợp lệ '{Value}' (fail-closed).", expClaim);
                return false;
            }

            try
            {
                expirationUtc = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            }
            catch
            {
                _logger?.LogWarning("Từ chối xác thực: Claim 'AuthExpiresUtc' không thể chuyển sang DateTimeOffset '{Value}' (fail-closed).", expClaim);
                return false;
            }
        }
        // 2. Thử parse dạng chuỗi chuẩn ISO 8601 UTC
        else if (DateTimeOffset.TryParse(expClaim, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDto))
        {
            expirationUtc = parsedDto;
        }
        else
        {
            _logger?.LogWarning("Từ chối xác thực: Claim 'AuthExpiresUtc' sai định dạng '{Value}' (fail-closed).", expClaim);
            return false;
        }

        // 3. Kiểm tra quá hạn so với thời gian hiện tại theo UTC
        if (DateTimeOffset.UtcNow >= expirationUtc)
        {
            _logger?.LogInformation("Phiên làm việc đã hết hạn tại {ExpireUtc} UTC (hiện tại {NowUtc} UTC).", expirationUtc, DateTimeOffset.UtcNow);
            return false;
        }

        return true;
    }

    private async Task<NguoiDung?> GetValidatedDbUserAsync(ClaimsPrincipal? principal)
    {
        if (principal == null || principal.Identity?.IsAuthenticated != true)
        {
            AuthDiagnosticLogger.Log(_logger, "[AUTH-DIAG] BlazorCurrentUserService: Principal is null hoặc chưa xác thực (Identity.IsAuthenticated != true).");
            return null;
        }

        // Bắt buộc claim AuthExpiresUtc phải có, parse hợp lệ theo UTC và chưa hết hạn
        if (!IsSessionActiveAndValid(principal))
        {
            AuthDiagnosticLogger.Log(_logger, $"[AUTH-DIAG] BlazorCurrentUserService: Phiên làm việc của '{principal.Identity.Name}' từ chối do claim AuthExpiresUtc thiếu, sai định dạng hoặc đã hết hạn.");
            _logger?.LogInformation("Phiên làm việc của người dùng '{User}' không hợp lệ hoặc đã hết hạn.", principal.Identity.Name);
            return null;
        }

        string? username = principal.Identity.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            AuthDiagnosticLogger.Log(_logger, "[AUTH-DIAG] BlazorCurrentUserService: Tên người dùng trong claim Identity.Name bị rỗng.");
            return null;
        }

        if (_nguoiDungRepository == null)
        {
            // Nếu không có repo (ví dụ mock test), chấp nhận principal nếu chưa hết hạn
            return new NguoiDung
            {
                TenDangNhap = username,
                VaiTro = principal.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
                TrangThai = true
            };
        }

        try
        {
            var dbUser = await _nguoiDungRepository.LayTheoTenDangNhapAsync(username);
            if (dbUser == null)
            {
                AuthDiagnosticLogger.Log(_logger, $"[AUTH-DIAG] BlazorCurrentUserService: Tài khoản '{username}' KHÔNG tìm thấy trong database.");
                _logger?.LogWarning("Tài khoản '{User}' không tồn tại trong cơ sở dữ liệu.", username);
                return null;
            }
            if (!dbUser.TrangThai)
            {
                AuthDiagnosticLogger.Log(_logger, $"[AUTH-DIAG] BlazorCurrentUserService: Tài khoản '{username}' bị khóa (TrangThai=0) trong database.");
                _logger?.LogWarning("Tài khoản '{User}' đã bị xóa hoặc tạm khóa trong cơ sở dữ liệu.", username);
                return null;
            }
            AuthDiagnosticLogger.Log(_logger, $"[AUTH-DIAG] BlazorCurrentUserService: Xác minh thành công tài khoản '{username}' từ DB. VaiTro='{dbUser.VaiTro}'.");
            return dbUser;
        }
        catch (Exception ex)
        {
            AuthDiagnosticLogger.Log(_logger, $"[AUTH-DIAG] BlazorCurrentUserService: Lỗi DB khi kiểm tra tài khoản '{username}': {ex.GetType().Name} - {ex.Message}");
            _logger?.LogError(ex, "Lỗi kết nối cơ sở dữ liệu khi xác minh tài khoản '{User}'. Áp dụng chính sách fail-closed.", username);
            // Fail-closed an toàn: không cấp quyền khi DB gặp sự cố
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var principal = await GetPrincipalAsync();
        var dbUser = await GetValidatedDbUserAsync(principal);
        return dbUser != null;
    }

    public async Task<string?> GetTenDangNhapAsync()
    {
        var principal = await GetPrincipalAsync();
        var dbUser = await GetValidatedDbUserAsync(principal);
        return dbUser?.TenDangNhap;
    }

    public async Task<string?> GetHoTenAsync()
    {
        var principal = await GetPrincipalAsync();
        var dbUser = await GetValidatedDbUserAsync(principal);
        return dbUser?.HoTen ?? principal?.FindFirst("HoTen")?.Value;
    }

    public async Task<string?> GetVaiTroAsync()
    {
        var principal = await GetPrincipalAsync();
        var dbUser = await GetValidatedDbUserAsync(principal);
        return dbUser?.VaiTro;
    }

    public async Task<bool> IsInRoleAsync(string role)
    {
        if (string.IsNullOrWhiteSpace(role)) return false;

        var principal = await GetPrincipalAsync();
        var dbUser = await GetValidatedDbUserAsync(principal);
        if (dbUser == null)
        {
            return false;
        }

        // Kiểm tra dựa trên vai trò THỰC TẾ mới nhất trong cơ sở dữ liệu
        return string.Equals(dbUser.VaiTro, role, StringComparison.OrdinalIgnoreCase);
    }
}
