using System;
using System.Collections.Generic;

namespace BanHang.UseCases.NhatKyEmailScreen;

/// <summary>
/// DTO chứa danh sách nhật ký email đã phân trang và tổng số bản ghi
/// </summary>
public class KetQuaDanhSachNhatKyEmailDto
{
    public IReadOnlyList<NhatKyEmailItemDto> DanhSach { get; set; } = Array.Empty<NhatKyEmailItemDto>();

    public int TongSoBanGhi { get; set; }

    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TongSoTrang => (int)Math.Ceiling((double)TongSoBanGhi / Math.Max(1, PageSize));

    public bool CoTrangTruoc => PageIndex > 1;

    public bool CoTrangSau => PageIndex < TongSoTrang;
}
