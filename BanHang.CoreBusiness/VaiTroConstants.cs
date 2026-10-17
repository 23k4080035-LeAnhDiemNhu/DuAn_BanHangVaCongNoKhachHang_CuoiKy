using System.Collections.Generic;

namespace BanHang.CoreBusiness;

/// <summary>
/// Các hằng số vai trò người dùng trong hệ thống (chỉ gồm 2 vai trò theo đặc tả dự án)
/// </summary>
public static class VaiTroConstants
{
    public const string NhanVienBanHang = "NhanVienBanHang";
    public const string KeToanCongNo = "KeToanCongNo";

    public static readonly IReadOnlyList<string> DanhSachVaiTro = new[]
    {
        NhanVienBanHang,
        KeToanCongNo
    };

    public static bool HopLe(string? vaiTro)
    {
        return vaiTro != null && (vaiTro == NhanVienBanHang || vaiTro == KeToanCongNo);
    }
}
