using System;
using BanHang.CoreBusiness;

namespace BanHang.UseCases.NhatKyEmailScreen;

/// <summary>
/// DTO chứa các tiêu chí tìm kiếm và lọc nhật ký email nhắc nợ
/// </summary>
public class BoLocNhatKyEmailDto
{
    public string? TuKhoa { get; set; }

    public DateTime? TuNgay { get; set; }

    public DateTime? DenNgay { get; set; }

    public TrangThaiGuiEmail? TrangThaiGui { get; set; }

    public string? NguonSoan { get; set; }

    public string? CheDoGui { get; set; }

    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
