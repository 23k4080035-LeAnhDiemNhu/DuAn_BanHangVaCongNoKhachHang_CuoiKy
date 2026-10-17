using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.BaoCaoTuoiNoScreen;

public class XemBaoCaoTuoiNoUseCase : IXemBaoCaoTuoiNoUseCase
{
    private readonly IBaoCaoTuoiNoRepository _baoCaoRepo;
    private readonly Common.ICurrentUserService _currentUserService;

    public XemBaoCaoTuoiNoUseCase(
        IBaoCaoTuoiNoRepository baoCaoRepo,
        Common.ICurrentUserService currentUserService)
    {
        _baoCaoRepo = baoCaoRepo ?? throw new ArgumentNullException(nameof(baoCaoRepo));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<BaoCaoTuoiNoResult> ExecuteAsync(DateTime ngayBaoCao)
    {
        await Common.AuthorizeHelper.RequireRoleAsync(_currentUserService, CoreBusiness.VaiTroConstants.KeToanCongNo);
        var duLieuDoc = await _baoCaoRepo.LayDuLieuBaoCaoAsync(ngayBaoCao);

        var result = new BaoCaoTuoiNoResult
        {
            NgayBaoCao = ngayBaoCao.Date,
            TienThuChuaPhanBo = duLieuDoc.TienThuChuaPhanBo,
            DanhSach = new List<BaoCaoTuoiNoItem>()
        };

        // Nhóm theo từng khách hàng
        var groupedCustomers = duLieuDoc.RawData.GroupBy(r => new
        {
            r.KhachHangId,
            r.MaKhachHang,
            r.TenKhachHang,
            r.SoDienThoai,
            r.HanMucCongNo
        });

        foreach (var group in groupedCustomers)
        {
            var item = new BaoCaoTuoiNoItem
            {
                KhachHangId = group.Key.KhachHangId,
                MaKhachHang = group.Key.MaKhachHang,
                TenKhachHang = group.Key.TenKhachHang,
                SoDienThoai = group.Key.SoDienThoai,
                HanMucCongNo = group.Key.HanMucCongNo
            };

            var hoaDonInputs = group
                .Where(r => r.HoaDonId.HasValue)
                .Select(r => new HoaDonTuoiNoInput
                {
                    HoaDonId = r.HoaDonId!.Value,
                    MaHoaDon = r.MaHoaDon ?? string.Empty,
                    NgayLap = r.NgayLap ?? DateTime.MinValue,
                    HanThanhToan = r.HanThanhToan ?? DateTime.MinValue,
                    TongTien = r.TongTien ?? 0,
                    TongDaPhanBoCoHieuLuc = r.TongDaPhanBoCoHieuLuc ?? 0
                })
                .ToList();

            PhanTichTuoiNoService.PhanBoVaoNhomTuoiNo(item, hoaDonInputs, ngayBaoCao);

            result.DanhSach.Add(item);
        }

        return result;
    }
}
