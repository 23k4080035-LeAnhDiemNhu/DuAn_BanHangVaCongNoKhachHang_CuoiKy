namespace BanHang.UseCases.PluginInterfaces.Security;

/// <summary>
/// Giao diện quản lý giới hạn số lần thử đăng nhập sai để ngăn chặn tấn công dò mật khẩu
/// </summary>
public interface ILoginRateLimiter
{
    bool IsLockedOut(string tenDangNhap);
    void RecordFailedAttempt(string tenDangNhap);
    void ResetAttempts(string tenDangNhap);
}
