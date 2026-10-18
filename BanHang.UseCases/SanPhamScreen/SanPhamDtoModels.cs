using System.Collections.Generic;

namespace BanHang.UseCases.SanPhamScreen;

public class TaoSanPhamRequest
{
    public string MaSanPham { get; set; } = string.Empty;
    public string TenSanPham { get; set; } = string.Empty;
    public string DonViTinh { get; set; } = "Cái";
    public decimal DonGia { get; set; } = 0;
    public string? MoTa { get; set; }
    public string? HinhAnh { get; set; }
    public bool TrangThai { get; set; } = true;
}

public class TaoSanPhamResult
{
    public bool ThanhCong { get; set; }
    public string ThongBao { get; set; } = string.Empty;
    public int? SanPhamIdMoi { get; set; }
    public string? MaSanPhamMoi { get; set; }
    public Dictionary<string, string> LoiTheoTruong { get; set; } = new();
}

public class CapNhatSanPhamRequest
{
    public int SanPhamId { get; set; }
    public string MaSanPham { get; set; } = string.Empty; // Giữ nguyên, chỉ hiển thị để đối chiếu
    public string TenSanPham { get; set; } = string.Empty;
    public string DonViTinh { get; set; } = "Cái";
    public decimal DonGia { get; set; } = 0;
    public string? MoTa { get; set; }
    public string? HinhAnh { get; set; }
    public bool TrangThai { get; set; } = true;
}

public class CapNhatSanPhamResult
{
    public bool ThanhCong { get; set; }
    public string ThongBao { get; set; } = string.Empty;
    public Dictionary<string, string> LoiTheoTruong { get; set; } = new();
}

public class DoiTrangThaiSanPhamResult
{
    public bool ThanhCong { get; set; }
    public string ThongBao { get; set; } = string.Empty;
    public bool TrangThaiMoi { get; set; }
}
