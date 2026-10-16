using System;
using System.Collections.Generic;

namespace BanHang.CoreBusiness;

/// <summary>
/// Model đầu vào cho việc phân tích tuổi nợ của một hóa đơn tại một mốc thời điểm.
/// </summary>
public class HoaDonTuoiNoInput
{
    public int HoaDonId { get; set; }
    public string MaHoaDon { get; set; } = string.Empty;
    public DateTime NgayLap { get; set; }
    public DateTime HanThanhToan { get; set; }
    public decimal TongTien { get; set; }
    public decimal TongDaPhanBoCoHieuLuc { get; set; }

    /// <summary>
    /// Dư nợ còn lại tại ngày báo cáo = TongTien - TongDaPhanBoCoHieuLuc
    /// </summary>
    public decimal DuNoTaiNgay => TongTien - TongDaPhanBoCoHieuLuc;
}

/// <summary>
/// Domain service phân tích tuổi nợ công nợ khách hàng (4 buckets: 0-30, 31-60, 61-90, Trên 90 ngày).
/// Thuần túy logic nghiệp vụ, độc lập hoàn toàn với SQL và UI.
/// </summary>
public static class PhanTichTuoiNoService
{
    /// <summary>
    /// Tính số ngày quá hạn tại ngày báo cáo: max(0, NgayBaoCao.Date - HanThanhToan.Date)
    /// Khoản chưa đến hạn (HanThanhToan >= NgayBaoCao) có số ngày quá hạn = 0.
    /// </summary>
    public static int TinhSoNgayQuaHan(DateTime hanThanhToan, DateTime ngayBaoCao)
    {
        return Math.Max(0, (ngayBaoCao.Date - hanThanhToan.Date).Days);
    }

    /// <summary>
    /// Phân bổ danh sách hóa đơn của khách hàng vào 4 nhóm tuổi nợ.
    /// </summary>
    public static void PhanBoVaoNhomTuoiNo(
        BaoCaoTuoiNoItem item,
        IEnumerable<HoaDonTuoiNoInput> hoaDons,
        DateTime ngayBaoCao)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(hoaDons);

        item.TuoiNo_0_30 = 0;
        item.TuoiNo_31_60 = 0;
        item.TuoiNo_61_90 = 0;
        item.TuoiNo_Tren_90 = 0;
        item.TongCongNo = 0;

        foreach (var hd in hoaDons)
        {
            // Chỉ tính hóa đơn có ngày lập trước ngày kế tiếp ngày báo cáo
            if (hd.NgayLap.Date > ngayBaoCao.Date)
            {
                continue;
            }

            // Quy tắc 6: Nếu phát hiện tổng phân bổ vượt tổng hóa đơn, báo dữ liệu không nhất quán
            if (hd.TongDaPhanBoCoHieuLuc > hd.TongTien)
            {
                throw new InvalidOperationException(
                    $"Dữ liệu hóa đơn {hd.MaHoaDon} không nhất quán: Tổng phân bổ có hiệu lực ({hd.TongDaPhanBoCoHieuLuc:N2} đ) vượt tổng tiền hóa đơn ({hd.TongTien:N2} đ).");
            }

            var duNo = hd.DuNoTaiNgay;
            if (duNo <= 0)
            {
                continue; // Hóa đơn đã thanh toán xong tính đến ngày báo cáo
            }

            int soNgayQuaHan = TinhSoNgayQuaHan(hd.HanThanhToan, ngayBaoCao);

            // Quy ước đề 18:
            // 0 - 30 ngày (bao gồm khoản chưa đến hạn và khoản quá hạn tối đa 30 ngày)
            if (soNgayQuaHan <= 30)
            {
                item.TuoiNo_0_30 += duNo;
            }
            else if (soNgayQuaHan <= 60)
            {
                item.TuoiNo_31_60 += duNo;
            }
            else if (soNgayQuaHan <= 90)
            {
                item.TuoiNo_61_90 += duNo;
            }
            else
            {
                item.TuoiNo_Tren_90 += duNo;
            }
        }

        // Tổng các nhóm phải bằng tổng công nợ
        item.TongCongNo = item.TuoiNo_0_30 + item.TuoiNo_31_60 + item.TuoiNo_61_90 + item.TuoiNo_Tren_90;
    }
}
