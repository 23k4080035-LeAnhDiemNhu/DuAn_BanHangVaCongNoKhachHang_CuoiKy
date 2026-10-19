using System;
using System.Threading.Tasks;

namespace BanHang.UseCases.BaoCaoTuoiNoScreen;

public class XuatBaoCaoTuoiNoExcelResult
{
    public bool ThanhCong { get; set; }
    public string? ThongBaoLoi { get; set; }
    public string TenFile { get; set; } = string.Empty;
    public byte[]? FileBytes { get; set; }
    public bool DuLieuBiThayDoi { get; set; }
}

public interface IXuatBaoCaoTuoiNoExcelUseCase
{
    Task<XuatBaoCaoTuoiNoExcelResult> ExecuteAsync(DateTime ngayBaoCaoApDung, BaoCaoTuoiNoResult? snapshotHienTai);
}
