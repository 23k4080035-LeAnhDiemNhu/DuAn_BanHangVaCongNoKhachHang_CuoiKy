using System;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.Common;

namespace BanHang.Tests;

/// <summary>
/// Dịch vụ CurrentUser giả lập dùng cho Unit Tests và Integration Tests
/// </summary>
public class FakeCurrentUserService : ICurrentUserService
{
    private readonly bool _isAuthenticated;
    private readonly string? _tenDangNhap;
    private readonly string? _hoTen;
    private readonly string? _vaiTro;

    public FakeCurrentUserService(
        bool isAuthenticated = true,
        string? vaiTro = VaiTroConstants.KeToanCongNo,
        string? tenDangNhap = "test_user",
        string? hoTen = "Test User")
    {
        _isAuthenticated = isAuthenticated;
        _vaiTro = vaiTro;
        _tenDangNhap = tenDangNhap;
        _hoTen = hoTen;
    }

    public static FakeCurrentUserService KeToanCongNo(string tenDangNhap = "ktcn") => new(true, VaiTroConstants.KeToanCongNo, tenDangNhap, $"Kế Toán ({tenDangNhap})");
    public static FakeCurrentUserService NhanVienBanHang() => new(true, VaiTroConstants.NhanVienBanHang, "nvbh", "Nhân Viên Bán Hàng Demo");
    public static FakeCurrentUserService Anonymous() => new(false, null, null, null);
    public static FakeCurrentUserService Role(string role) => new(true, role, "custom_user", "Custom User");

    public Task<bool> IsAuthenticatedAsync() => Task.FromResult(_isAuthenticated);

    public Task<string?> GetTenDangNhapAsync() => Task.FromResult(_tenDangNhap);

    public Task<string?> GetHoTenAsync() => Task.FromResult(_hoTen);

    public Task<string?> GetVaiTroAsync() => Task.FromResult(_vaiTro);

    public Task<bool> IsInRoleAsync(string role)
    {
        if (!_isAuthenticated || string.IsNullOrEmpty(_vaiTro))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(string.Equals(_vaiTro, role, StringComparison.OrdinalIgnoreCase));
    }
}
