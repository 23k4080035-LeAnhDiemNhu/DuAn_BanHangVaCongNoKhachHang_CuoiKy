namespace BanHang.UseCases.ThanhToanScreen;

/// <summary>
/// DTO chứa thông tin mã VietQR và chi tiết chuyển khoản hiển thị trên giao diện
/// </summary>
public class VietQrThanhToanDto
{
    public bool HopLe { get; set; }
    public string? ThongBaoLoi { get; set; }

    public string BankId { get; set; } = string.Empty;
    public string BankBin { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;

    public long SoTienNguyen { get; set; }
    public string SoTienDinhDang { get; set; } = string.Empty;
    public string NoiDungChuyenKhoan { get; set; } = string.Empty;
    public string QrUrl { get; set; } = string.Empty;
}
