using System.Threading;
using System.Threading.Tasks;
using BanHang.CoreBusiness;

namespace BanHang.UseCases.PluginInterfaces.Email;

public class ThongTinEmailGuiDto
{
    public string NguoiNhanEmail { get; set; } = string.Empty;
    public string TenNguoiNhan { get; set; } = string.Empty;
    public string TieuDe { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
}

public class KetQuaGuiEmailRawDto
{
    public bool ThanhCong { get; set; }
    public TrangThaiGuiEmail TrangThai { get; set; }
    public string? MaPhanHoiDichVu { get; set; }
    public string? ThongBaoChiTiet { get; set; }
    public string? EmailNhanThucTe { get; set; }
}

/// <summary>
/// Hợp đồng dịch vụ gửi email hạ tầng (Adapter triển khai SMTP hoặc Dev Mock)
/// </summary>
public interface IEmailSender
{
    Task<KetQuaGuiEmailRawDto> GuiEmailAsync(ThongTinEmailGuiDto emailDto, CancellationToken cancellationToken = default);
}
