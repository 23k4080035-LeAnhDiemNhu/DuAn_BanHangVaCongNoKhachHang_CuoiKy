using System;
using System.Linq;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.Common;
using BanHang.UseCases.PluginInterfaces.Reports;

namespace BanHang.UseCases.BaoCaoTuoiNoScreen;

public class XuatBaoCaoTuoiNoExcelUseCase : IXuatBaoCaoTuoiNoExcelUseCase
{
    private readonly IXemBaoCaoTuoiNoUseCase _xemBaoCaoTuoiNoUseCase;
    private readonly IBaoCaoTuoiNoExcelExporter _excelExporter;
    private readonly ICurrentUserService _currentUserService;

    public XuatBaoCaoTuoiNoExcelUseCase(
        IXemBaoCaoTuoiNoUseCase xemBaoCaoTuoiNoUseCase,
        IBaoCaoTuoiNoExcelExporter excelExporter,
        ICurrentUserService currentUserService)
    {
        _xemBaoCaoTuoiNoUseCase = xemBaoCaoTuoiNoUseCase ?? throw new ArgumentNullException(nameof(xemBaoCaoTuoiNoUseCase));
        _excelExporter = excelExporter ?? throw new ArgumentNullException(nameof(excelExporter));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<XuatBaoCaoTuoiNoExcelResult> ExecuteAsync(DateTime ngayBaoCaoApDung, BaoCaoTuoiNoResult? snapshotHienTai)
    {
        // 1. Kiểm tra quyền máy chủ: Chỉ dành cho Kế toán công nợ (KeToanCongNo)
        await AuthorizeHelper.RequireRoleAsync(_currentUserService, VaiTroConstants.KeToanCongNo);

        // 2. Tái sử dụng cách tính báo cáo hiện có từ IXemBaoCaoTuoiNoUseCase theo đúng ngày áp dụng
        var duLieuServer = await _xemBaoCaoTuoiNoUseCase.ExecuteAsync(ngayBaoCaoApDung);

        // 3. Nếu màn hình đang có snapshot hiển thị, kiểm tra xem dữ liệu CSDL có bị thay đổi không
        if (snapshotHienTai != null && KiemTraDuLieuCoKhacNhau(duLieuServer, snapshotHienTai))
        {
            return new XuatBaoCaoTuoiNoExcelResult
            {
                ThanhCong = false,
                DuLieuBiThayDoi = true,
                ThongBaoLoi = "Dữ liệu công nợ trên hệ thống đã thay đổi so với bảng báo cáo đang xem trên màn hình. Vui lòng bấm 'Xem báo cáo' để làm mới số liệu trước khi xuất Excel."
            };
        }

        // 4. Kết xuất file Excel thông qua plugin exporter
        var bytes = _excelExporter.ExportBaoCaoTuoiNo(duLieuServer);
        var tenFile = $"BaoCaoTuoiNo_{ngayBaoCaoApDung:yyyyMMdd}.xlsx";

        return new XuatBaoCaoTuoiNoExcelResult
        {
            ThanhCong = true,
            TenFile = tenFile,
            FileBytes = bytes
        };
    }

    private static bool KiemTraDuLieuCoKhacNhau(BaoCaoTuoiNoResult server, BaoCaoTuoiNoResult client)
    {
        if (server.NgayBaoCao.Date != client.NgayBaoCao.Date) return true;
        if (server.TienThuChuaPhanBo != client.TienThuChuaPhanBo) return true;
        if (server.Tong_CongNo != client.Tong_CongNo) return true;
        if (server.Tong_TuoiNo_0_30 != client.Tong_TuoiNo_0_30) return true;
        if (server.Tong_TuoiNo_31_60 != client.Tong_TuoiNo_31_60) return true;
        if (server.Tong_TuoiNo_61_90 != client.Tong_TuoiNo_61_90) return true;
        if (server.Tong_TuoiNo_Tren_90 != client.Tong_TuoiNo_Tren_90) return true;
        if (server.DanhSach.Count != client.DanhSach.Count) return true;

        var clientDict = client.DanhSach.ToDictionary(k => k.KhachHangId);
        foreach (var s in server.DanhSach)
        {
            if (!clientDict.TryGetValue(s.KhachHangId, out var c)) return true;
            if (s.TongCongNo != c.TongCongNo ||
                s.TuoiNo_0_30 != c.TuoiNo_0_30 ||
                s.TuoiNo_31_60 != c.TuoiNo_31_60 ||
                s.TuoiNo_61_90 != c.TuoiNo_61_90 ||
                s.TuoiNo_Tren_90 != c.TuoiNo_Tren_90)
            {
                return true;
            }
        }

        return false;
    }
}
