using BanHang.UseCases.BaoCaoTuoiNoScreen;

namespace BanHang.UseCases.PluginInterfaces.Reports;

/// <summary>
/// Plugin interface định nghĩa chức năng kết xuất dữ liệu Báo cáo tuổi nợ ra file Excel (.xlsx).
/// Tầng CoreBusiness và UseCases không phụ thuộc vào thư viện bên thứ ba (ClosedXML).
/// </summary>
public interface IBaoCaoTuoiNoExcelExporter
{
    byte[] ExportBaoCaoTuoiNo(BaoCaoTuoiNoResult baoCao);
}
