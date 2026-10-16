using System;
using System.Collections.Generic;
using System.Linq;
using BanHang.CoreBusiness;

namespace BanHang.UseCases.BaoCaoTuoiNoScreen;

/// <summary>
/// Kết quả tổng hợp Báo cáo phân tích tuổi nợ công nợ khách hàng (4 buckets) tại ngày chốt.
/// </summary>
public class BaoCaoTuoiNoResult
{
    public DateTime NgayBaoCao { get; set; }

    public List<BaoCaoTuoiNoItem> DanhSach { get; set; } = new();

    public decimal Tong_TuoiNo_0_30 => DanhSach.Sum(x => x.TuoiNo_0_30);

    public decimal Tong_TuoiNo_31_60 => DanhSach.Sum(x => x.TuoiNo_31_60);

    public decimal Tong_TuoiNo_61_90 => DanhSach.Sum(x => x.TuoiNo_61_90);

    public decimal Tong_TuoiNo_Tren_90 => DanhSach.Sum(x => x.TuoiNo_Tren_90);

    public decimal Tong_CongNo => DanhSach.Sum(x => x.TongCongNo);

    /// <summary>
    /// Tiền thu chưa phân bổ có hiệu lực đến ngày báo cáo (hiển thị riêng phục vụ đối soát)
    /// </summary>
    public decimal TienThuChuaPhanBo { get; set; }
}
