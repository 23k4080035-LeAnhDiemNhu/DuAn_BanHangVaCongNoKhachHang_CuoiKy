namespace BanHang.UseCases.PluginInterfaces.Security;

/// <summary>
/// Giao diện xử lý băm và kiểm tra mật khẩu sử dụng cơ chế bảo mật chuẩn của hệ thống
/// </summary>
public interface IPasswordHasherService
{
    string HashPassword(string plainPassword);
    bool VerifyPassword(string hashedPassword, string providedPassword);
}
