using System.Collections.Generic;
using System.Threading.Tasks;
using BanHang.CoreBusiness;

namespace BanHang.UseCases.PluginInterfaces.DataStore;

/// <summary>
/// Plugin interface lưu và truy vấn nhật ký gửi email nhắc nợ (Audit Trail)
/// </summary>
public interface INhatKyEmailRepository
{
    Task<int> LuuNhatKyAsync(NhatKyEmailNhacNo nhatKy);

    Task<int> GhiNhanYeuCauGuiAsync(NhatKyEmailNhacNo nhatKy);

    Task CapNhatTrangThaiGuiAsync(
        int nhatKyId,
        TrangThaiGuiEmail trangThai,
        string? chiTiet,
        string? maPhanHoi,
        string? emailNhanThucTe = null,
        string? cheDoGui = null);

    Task<NhatKyEmailNhacNo?> LayTheoIdempotencyKeyAsync(string idempotencyKey);

    Task<IEnumerable<NhatKyEmailNhacNo>> LayLichSuTheoKhachHangAsync(int khachHangId);

    Task<(IEnumerable<NhatKyEmailNhacNo> DanhSach, int TongSoBanGhi)> LayDanhSachPhanTrangAsync(
        string? tuKhoa,
        DateTime? tuNgay,
        DateTime? denNgay,
        TrangThaiGuiEmail? trangThaiGui,
        string? nguonSoan,
        string? cheDoGui,
        int pageIndex,
        int pageSize);

    Task<NhatKyEmailNhacNo?> LayTheoIdAsync(int nhatKyId);
}
