namespace BanHang.CoreBusiness;

/// <summary>
/// DTO đại diện cho hóa đơn còn nợ phục vụ thuật toán phân bổ FIFO
/// </summary>
public class HoaDonNoModel
{
    public int HoaDonId { get; set; }
    public string MaHoaDon { get; set; } = string.Empty;
    public DateTime NgayLap { get; set; }
    public decimal TongTien { get; set; }
    public decimal DaThanhToan { get; set; }
    public decimal ConLai { get; set; }
}

/// <summary>
/// Chi tiết phân bổ tiền thu vào từng hóa đơn cụ thể theo FIFO
/// </summary>
public class ChiTietPhanBoFifo
{
    public int HoaDonId { get; set; }
    public string MaHoaDon { get; set; } = string.Empty;
    public DateTime NgayLap { get; set; }
    public decimal SoNoTruocPhanBo { get; set; }
    public decimal SoTienPhanBo { get; set; }
    public decimal SoNoSauPhanBo { get; set; }
    public bool DaThanhToanXong { get; set; }
}

/// <summary>
/// Kết quả tổng hợp của thuật toán phân bổ thanh toán FIFO
/// </summary>
public class KetQuaPhanBoFifo
{
    public decimal TongTienThanhToan { get; set; }
    public decimal TongDaPhanBo { get; set; }
    public decimal TienChuaPhanBo { get; set; }
    public List<ChiTietPhanBoFifo> DanhSachPhanBo { get; set; } = new();
    public int SoHoaDonBiLoaiBoDoLapSauNgayThu { get; set; }
}

/// <summary>
/// Thuật toán phân bổ thanh toán FIFO độc lập, không phụ thuộc CSDL hay giao diện
/// </summary>
public static class ThuatToanFIFO
{
    /// <summary>
    /// Thực hiện phân bổ số tiền thanh toán vào danh sách hóa đơn theo nguyên tắc FIFO:
    /// - Hóa đơn có NgayLap cũ hơn được thanh toán trước.
    /// - Nếu trùng NgayLap, hóa đơn có HoaDonId nhỏ hơn được thanh toán trước.
    /// - Không phân bổ cho hóa đơn có NgayLap sau NgayThanhToan (phiếu thu ghi lùi ngày).
    /// - Số tiền thanh toán không được làm tròn âm thầm, từ chối nếu có hơn 2 chữ số thập phân.
    /// - Nếu tiền thu vượt tổng nợ, phần dư được giữ là TienChuaPhanBo, không tạo nợ âm.
    /// </summary>
    public static KetQuaPhanBoFifo PhanBo(
        IEnumerable<HoaDonNoModel>? danhSachHoaDon,
        decimal soTienThanhToan,
        DateTime ngayThanhToan)
    {
        // 1. Kiểm tra số tiền thanh toán
        if (soTienThanhToan <= 0)
        {
            throw new QuyTacNghiepVuException("Số tiền thanh toán phải là số dương lớn hơn 0.");
        }

        // Kiểm tra tối đa 2 chữ số thập phân, từ chối nếu có nhiều hơn thay vì âm thầm làm tròn
        if (decimal.Round(soTienThanhToan, 2) != soTienThanhToan)
        {
            throw new QuyTacNghiepVuException("Số tiền thanh toán chỉ được phép có tối đa 2 chữ số thập phân.");
        }

        var ketQua = new KetQuaPhanBoFifo
        {
            TongTienThanhToan = soTienThanhToan,
            TienChuaPhanBo = soTienThanhToan,
            TongDaPhanBo = 0
        };

        if (danhSachHoaDon == null)
        {
            return ketQua;
        }

        var hoaDons = danhSachHoaDon.ToList();

        // 2. Lọc các hóa đơn có ngày lập sau ngày thanh toán (trường hợp phiếu thu ghi lùi ngày)
        // Luồng này sử dụng dư nợ hiện tại và không phân bổ cho hóa đơn lập sau ngày phiếu thu
        var hoaDonsBiLoai = hoaDons.Count(h => h.NgayLap.Date > ngayThanhToan.Date);
        ketQua.SoHoaDonBiLoaiBoDoLapSauNgayThu = hoaDonsBiLoai;

        var hoaDonsHopLe = hoaDons
            .Where(h => h.ConLai > 0 && h.NgayLap.Date <= ngayThanhToan.Date)
            .OrderBy(h => h.NgayLap)
            .ThenBy(h => h.HoaDonId)
            .ToList();

        decimal soTienConLai = soTienThanhToan;

        // 3. Tiến hành phân bổ lần lượt từng hóa đơn theo thứ tự FIFO
        foreach (var hd in hoaDonsHopLe)
        {
            if (soTienConLai <= 0)
            {
                break;
            }

            decimal tienPhanBo = Math.Min(hd.ConLai, soTienConLai);
            if (tienPhanBo > 0)
            {
                decimal noSauPhanBo = hd.ConLai - tienPhanBo;
                ketQua.DanhSachPhanBo.Add(new ChiTietPhanBoFifo
                {
                    HoaDonId = hd.HoaDonId,
                    MaHoaDon = hd.MaHoaDon,
                    NgayLap = hd.NgayLap,
                    SoNoTruocPhanBo = hd.ConLai,
                    SoTienPhanBo = tienPhanBo,
                    SoNoSauPhanBo = noSauPhanBo,
                    DaThanhToanXong = (noSauPhanBo == 0)
                });

                soTienConLai -= tienPhanBo;
            }
        }

        ketQua.TongDaPhanBo = soTienThanhToan - soTienConLai;
        ketQua.TienChuaPhanBo = soTienConLai;

        return ketQua;
    }
}
