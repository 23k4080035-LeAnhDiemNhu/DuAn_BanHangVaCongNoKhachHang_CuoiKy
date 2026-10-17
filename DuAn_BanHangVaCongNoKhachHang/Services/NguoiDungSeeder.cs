using System;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.PluginInterfaces.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DuAn_BanHangVaCongNoKhachHang.Services;

/// <summary>
/// Công cụ khởi tạo an toàn 2 tài khoản demo (nvbh và ktcn)
/// - Nhận mật khẩu qua cấu hình cục bộ (User Secrets/Environment Variables) hoặc tham số
/// - Không in mật khẩu hoặc ghi mật khẩu dạng rõ
/// - Có tính lũy kế (idempotent), không tự đặt lại nếu tài khoản đã tồn tại
/// </summary>
public static class NguoiDungSeeder
{
    public static async Task SeedDemoAccountsAsync(
        INguoiDungRepository repo,
        IPasswordHasherService passwordHasher,
        string? nvbhPassword,
        string? ktcnPassword,
        ILogger? logger = null)
    {
        if (repo == null || passwordHasher == null) return;

        // 1. Khởi tạo tài khoản Nhân viên bán hàng (nvbh)
        if (!string.IsNullOrWhiteSpace(nvbhPassword))
        {
            bool exists = await repo.TonTaiTenDangNhapAsync("nvbh");
            if (!exists)
            {
                string hash = passwordHasher.HashPassword(nvbhPassword.Trim());
                var user = new NguoiDung
                {
                    TenDangNhap = "nvbh",
                    MatKhauHash = hash,
                    HoTen = "Nhân Viên Bán Hàng Demo",
                    VaiTro = VaiTroConstants.NhanVienBanHang,
                    TrangThai = true,
                    NgayTao = DateTime.Now
                };
                await repo.TaoNguoiDungAsync(user);
                logger?.LogInformation("Khởi tạo tài khoản demo '{TenDangNhap}' thành công.", "nvbh");
            }
            else
            {
                logger?.LogInformation("Tài khoản demo '{TenDangNhap}' đã tồn tại, không ghi đè.", "nvbh");
            }
        }

        // 2. Khởi tạo tài khoản Kế toán công nợ (ktcn)
        if (!string.IsNullOrWhiteSpace(ktcnPassword))
        {
            bool exists = await repo.TonTaiTenDangNhapAsync("ktcn");
            if (!exists)
            {
                string hash = passwordHasher.HashPassword(ktcnPassword.Trim());
                var user = new NguoiDung
                {
                    TenDangNhap = "ktcn",
                    MatKhauHash = hash,
                    HoTen = "Kế Toán Công Nợ Demo",
                    VaiTro = VaiTroConstants.KeToanCongNo,
                    TrangThai = true,
                    NgayTao = DateTime.Now
                };
                await repo.TaoNguoiDungAsync(user);
                logger?.LogInformation("Khởi tạo tài khoản demo '{TenDangNhap}' thành công.", "ktcn");
            }
            else
            {
                logger?.LogInformation("Tài khoản demo '{TenDangNhap}' đã tồn tại, không ghi đè.", "ktcn");
            }
        }
    }

    /// <summary>
    /// Đọc mật khẩu từ biến môi trường hoặc User Secrets và tự động seed nếu có cấu hình
    /// </summary>
    public static async Task SeedFromConfigurationAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var repo = services.GetService(typeof(INguoiDungRepository)) as INguoiDungRepository;
        var hasher = services.GetService(typeof(IPasswordHasherService)) as IPasswordHasherService;

        if (repo == null || hasher == null) return;

        string? nvbhPass = configuration["SEED_NVBH_PASSWORD"] ?? configuration["SeedPasswords:NVBH"];
        string? ktcnPass = configuration["SEED_KTCN_PASSWORD"] ?? configuration["SeedPasswords:KTCN"];

        if (!string.IsNullOrWhiteSpace(nvbhPass) || !string.IsNullOrWhiteSpace(ktcnPass))
        {
            await SeedDemoAccountsAsync(repo, hasher, nvbhPass, ktcnPass);
        }
    }
}
