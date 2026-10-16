using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BanHang.UseCases.PluginInterfaces.DataStore;

/// <summary>
/// Raw DTO đọc từ SQL phục vụ báo cáo tuổi nợ tại thời điểm chốt.
/// </summary>
public class KhachHangHoaDonTuoiNoRawDto
{
    public int KhachHangId { get; set; }
    public string MaKhachHang { get; set; } = string.Empty;
    public string TenKhachHang { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public decimal HanMucCongNo { get; set; }
    public int? HoaDonId { get; set; }
    public string? MaHoaDon { get; set; }
    public DateTime? NgayLap { get; set; }
    public DateTime? HanThanhToan { get; set; }
    public decimal? TongTien { get; set; }
    public decimal? TongDaPhanBoCoHieuLuc { get; set; }
}

/// <summary>
/// DTO chứa toàn bộ dữ liệu báo cáo tuổi nợ được đọc trong cùng một transaction nhất quán.
/// </summary>
public class DuLieuBaoCaoTuoiNoDto
{
    public IEnumerable<KhachHangHoaDonTuoiNoRawDto> RawData { get; set; } = new List<KhachHangHoaDonTuoiNoRawDto>();
    public decimal TienThuChuaPhanBo { get; set; }
}

/// <summary>
/// Plugin interface truy xuất dữ liệu báo cáo tuổi nợ từ DataStore.
/// Không phụ thuộc vào Dapper, SqlConnection hay SqlTransaction.
/// </summary>
public interface IBaoCaoTuoiNoRepository
{
    Task<DuLieuBaoCaoTuoiNoDto> LayDuLieuBaoCaoAsync(DateTime ngayBaoCao);
    Task<IEnumerable<KhachHangHoaDonTuoiNoRawDto>> LayDuLieuTuoiNoRawAsync(DateTime ngayBaoCao);
    Task<decimal> LayTongTienThuChuaPhanBoAsync(DateTime ngayBaoCao);
}
