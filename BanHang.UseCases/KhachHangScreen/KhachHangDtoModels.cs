using System.Collections.Generic;

namespace BanHang.UseCases.KhachHangScreen;

public class TaoKhachHangRequest
{
    public string MaKhachHang { get; set; } = string.Empty;
    public string TenKhachHang { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }
    public string? DiaChi { get; set; }
    public decimal HanMucCongNo { get; set; } = 0;
    public int SoNgayNo { get; set; } = 0;
    public bool TrangThai { get; set; } = true;
}

public class TaoKhachHangResult
{
    public bool ThanhCong { get; set; }
    public string ThongBao { get; set; } = string.Empty;
    public int? KhachHangIdMoi { get; set; }
    public string? MaKhachHangMoi { get; set; }
    public Dictionary<string, string> LoiTheoTruong { get; set; } = new();
}

public class CapNhatKhachHangRequest
{
    public int KhachHangId { get; set; }
    public string MaKhachHang { get; set; } = string.Empty;
    public string TenKhachHang { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }
    public string? DiaChi { get; set; }
    public decimal HanMucCongNo { get; set; } = 0;
    public int SoNgayNo { get; set; } = 0;
    public bool TrangThai { get; set; } = true;

    /// <summary>
    /// Đánh dấu người dùng đã xác nhận hạ hạn mức công nợ xuống thấp hơn số dư công nợ hiện tại
    /// </summary>
    public bool XacNhanHaHanMucDuoiNo { get; set; } = false;
}

public class CapNhatKhachHangResult
{
    public bool ThanhCong { get; set; }
    public string ThongBao { get; set; } = string.Empty;
    public bool CanXacNhanHaHanMuc { get; set; } = false;
    public decimal CongNoHienTai { get; set; } = 0;
    public Dictionary<string, string> LoiTheoTruong { get; set; } = new();
}

public class DoiTrangThaiKhachHangResult
{
    public bool ThanhCong { get; set; }
    public string ThongBao { get; set; } = string.Empty;
    public bool TrangThaiMoi { get; set; }
}
