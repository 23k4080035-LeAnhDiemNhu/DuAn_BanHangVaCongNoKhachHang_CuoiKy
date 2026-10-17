using System;
using System.Threading.Tasks;

namespace BanHang.UseCases.Common;

/// <summary>
/// Trợ giúp kiểm tra quyền hạn phía máy chủ trong tầng UseCase
/// Đảm bảo tính nhất quán: Anonymous bị từ chối; đúng vai trò được phép; sai vai trò bị từ chối.
/// </summary>
public static class AuthorizeHelper
{
    public static async Task RequireRoleAsync(ICurrentUserService currentUserService, params string[] allowedRoles)
    {
        if (currentUserService == null || !await currentUserService.IsAuthenticatedAsync())
        {
            throw new UnauthorizedAccessException("Người dùng chưa được xác thực. Vui lòng đăng nhập.");
        }

        foreach (var role in allowedRoles)
        {
            if (await currentUserService.IsInRoleAsync(role))
            {
                return;
            }
        }

        throw new UnauthorizedAccessException("Người dùng không có quyền thực hiện thao tác này.");
    }
}
