using BanHang.UseCases.PluginInterfaces.Security;
using Microsoft.AspNetCore.Identity;

namespace DuAn_BanHangVaCongNoKhachHang.Services;

/// <summary>
/// Triển khai IPasswordHasherService sử dụng PasswordHasher chuẩn của ASP.NET Core Identity (PBKDF2, HMAC-SHA512)
/// </summary>
public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<object> _hasher = new();
    private static readonly object _dummyUser = new();

    public string HashPassword(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
        {
            throw new System.ArgumentException("Mật khẩu không được để trống.", nameof(plainPassword));
        }

        return _hasher.HashPassword(_dummyUser, plainPassword);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword))
        {
            return false;
        }

        var result = _hasher.VerifyHashedPassword(_dummyUser, hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
