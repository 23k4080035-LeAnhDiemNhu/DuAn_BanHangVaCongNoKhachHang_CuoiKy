using BanHang.CoreBusiness;

namespace BanHang.UseCases.ThanhToanScreen;

/// <summary>
/// DTO lưu vết từng dòng phân bổ khi người dùng xem trước trên giao diện
/// </summary>
public class ChiTietXemTruocDto
{
    public int HoaDonId { get; set; }
    public decimal SoTienPhanBo { get; set; }
}

/// <summary>
/// Yêu cầu tạo mới phiếu thu tiền
/// </summary>
public class TaoThanhToanRequest
{
    public string MaPhieuThu { get; set; } = string.Empty;
    public int KhachHangId { get; set; }
    public DateTime NgayThanhToan { get; set; } = DateTime.Today;
    public decimal SoTien { get; set; }
    public string PhuongThuc { get; set; } = "ChuyenKhoan"; // 'ChuyenKhoan', 'TienMat'
    public string? GhiChu { get; set; }

    /// <summary>
    /// Danh sách kết quả phân bổ mà người dùng đã xem trước.
    /// Nếu có giá trị, khi lưu sẽ đối soát; nếu dữ liệu thay đổi sẽ cảnh báo người dùng xem lại.
    /// </summary>
    public List<ChiTietXemTruocDto>? DanhSachXemTruoc { get; set; }
}

/// <summary>
/// Kết quả sau khi ghi nhận phiếu thu và phân bổ FIFO
/// </summary>
public class TaoThanhToanResult
{
    public bool ThanhCong { get; set; }
    public string ThongBao { get; set; } = string.Empty;
    public int? ThanhToanIdMoi { get; set; }
    public string MaPhieuThu { get; set; } = string.Empty;
    public decimal? SoTien { get; set; }
    public decimal? TongDaPhanBo { get; set; }
    public decimal? TienChuaPhanBo { get; set; }
    public bool DuLieuThayDoiSoVoiXemTruoc { get; set; }
    public List<ChiTietPhanBoFifo> DanhSachPhanBo { get; set; } = new();
}

/// <summary>
/// Yêu cầu xem trước kết quả phân bổ FIFO trước khi lưu
/// </summary>
public class XemTruocPhanBoRequest
{
    public int KhachHangId { get; set; }
    public DateTime NgayThanhToan { get; set; } = DateTime.Today;
    public decimal SoTien { get; set; }
}

/// <summary>
/// Kết quả xem trước phân bổ FIFO
/// </summary>
public class XemTruocPhanBoResult
{
    public bool ThanhCong { get; set; }
    public string ThongBao { get; set; } = string.Empty;
    public decimal TongCongNoHienTai { get; set; }
    public KetQuaPhanBoFifo? KetQuaFifo { get; set; }
}
