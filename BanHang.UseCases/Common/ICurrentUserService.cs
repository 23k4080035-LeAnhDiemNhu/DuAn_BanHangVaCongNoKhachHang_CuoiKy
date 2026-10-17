using System.Threading.Tasks;

namespace BanHang.UseCases.Common;

/// <summary>
/// Giao diện lấy thông tin danh tính và vai trò người dùng hiện tại ở tầng UseCases
/// Độc lập hoàn toàn với ASP.NET Core và HttpContext
/// </summary>
public interface ICurrentUserService
{
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetTenDangNhapAsync();
    Task<string?> GetHoTenAsync();
    Task<string?> GetVaiTroAsync();
    Task<bool> IsInRoleAsync(string role);
}
