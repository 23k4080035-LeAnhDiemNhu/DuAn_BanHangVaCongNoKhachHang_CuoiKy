using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BanHang.CoreBusiness;

namespace BanHang.UseCases.PluginInterfaces.AI;

/// <summary>
/// DTO chứa dữ liệu tối thiểu và an toàn gửi cho dịch vụ AI để hỗ trợ diễn đạt email nhắc nợ
/// </summary>
public class DuLieuSoanEmailDto
{
    public int KhachHangId { get; set; }

    public string MaKhachHang { get; set; } = string.Empty;

    public string TenKhachHang { get; set; } = string.Empty;

    public DateTime NgayChotBaoCao { get; set; }

    public decimal TongCongNo { get; set; }

    public decimal TuoiNo_0_30 { get; set; }

    public decimal TuoiNo_31_60 { get; set; }

    public decimal TuoiNo_61_90 { get; set; }

    public decimal TuoiNo_Tren_90 { get; set; }

    /// <summary>
    /// Tiền thu chưa phân bổ (nếu có, AI phải trình bày riêng, TUYỆT ĐỐI không tự trừ vào nợ)
    /// </summary>
    public decimal TienThuChuaPhanBo { get; set; }

    public GiongVanEmail GiongVan { get; set; } = GiongVanEmail.TieuChuan;

    /// <summary>
    /// Yêu cầu chỉnh cách viết của kế toán (Ví dụ: "ngắn hơn", "lịch sự hơn", "nhấn mạnh khoản quá hạn")
    /// </summary>
    public string? YeuCauChinhCachViet { get; set; }

    public List<HoaDonTomTatDto> DanhSachHoaDon { get; set; } = new();
}

public class HoaDonTomTatDto
{
    public string MaHoaDon { get; set; } = string.Empty;
    public DateTime NgayLap { get; set; }
    public DateTime HanThanhToan { get; set; }
    public decimal DuNoTaiNgayChot { get; set; }
    public int SoNgayQuaHan { get; set; }
    public bool LaTrongHan => SoNgayQuaHan <= 0;
}

public class KetQuaSoanThaoAiDto
{
    public bool ThanhCong { get; set; }
    public string TieuDe { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public string? ThongBaoLoi { get; set; }
    public bool SuDungFallbackTemplate { get; set; }

    /// <summary>
    /// Nguồn soạn thảo: "AI thật", "Mô phỏng", "Mẫu có sẵn"
    /// </summary>
    public string NguonSoan { get; set; } = "AI thật";
}

/// <summary>
/// Hợp đồng dịch vụ AI hỗ trợ soạn thảo email nhắc nợ (tầng adapter triển khai IChatClient)
/// </summary>
public interface IAiEmailDraftingService
{
    Task<KetQuaSoanThaoAiDto> SoanThaoEmailAsync(DuLieuSoanEmailDto duLieu, CancellationToken cancellationToken = default);
}
