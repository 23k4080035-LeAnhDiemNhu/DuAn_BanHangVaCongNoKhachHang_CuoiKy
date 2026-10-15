namespace BanHang.CoreBusiness;

/// <summary>
/// Đại diện cho một dòng dữ liệu trong Báo cáo phân tích tuổi nợ công nợ khách hàng (Aging Schedule - 4 buckets)
/// </summary>
public class BaoCaoTuoiNoItem
{
    public int KhachHangId { get; set; }

    public string MaKhachHang { get; set; } = string.Empty;

    public string TenKhachHang { get; set; } = string.Empty;

    public string? SoDienThoai { get; set; }

    public decimal HanMucCongNo { get; set; } = 0;

    /// <summary>
    /// Tổng công nợ hiện tại mà khách hàng đang nợ
    /// </summary>
    public decimal TongCongNo { get; set; } = 0;

    /// <summary>
    /// Xô nợ 1: Trong hạn hoặc 0 - 30 ngày
    /// </summary>
    public decimal TuoiNo_0_30 { get; set; } = 0;

    /// <summary>
    /// Xô nợ 2: Quá hạn 31 - 60 ngày
    /// </summary>
    public decimal TuoiNo_31_60 { get; set; } = 0;

    /// <summary>
    /// Xô nợ 3: Quá hạn 61 - 90 ngày
    /// </summary>
    public decimal TuoiNo_61_90 { get; set; } = 0;

    /// <summary>
    /// Xô nợ 4: Quá hạn trên 90 ngày (Nguy cơ nợ xấu cao)
    /// </summary>
    public decimal TuoiNo_Tren_90 { get; set; } = 0;

    /// <summary>
    /// Tỷ lệ nợ xấu (quá hạn trên 90 ngày) so với tổng nợ (%)
    /// </summary>
    public double TyLeNoXau => TongCongNo > 0 ? (double)(TuoiNo_Tren_90 / TongCongNo) * 100 : 0;
}
