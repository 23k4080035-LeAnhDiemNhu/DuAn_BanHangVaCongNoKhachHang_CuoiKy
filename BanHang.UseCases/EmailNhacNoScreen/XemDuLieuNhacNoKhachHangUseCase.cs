using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.Common;
using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.EmailNhacNoScreen;

public class XemDuLieuNhacNoKhachHangUseCase : IXemDuLieuNhacNoKhachHangUseCase
{
    private readonly IBaoCaoTuoiNoRepository _baoCaoRepo;
    private readonly IKhachHangRepository _khachHangRepo;
    private readonly ICurrentUserService _currentUserService;

    public XemDuLieuNhacNoKhachHangUseCase(
        IBaoCaoTuoiNoRepository baoCaoRepo,
        IKhachHangRepository khachHangRepo,
        ICurrentUserService currentUserService)
    {
        _baoCaoRepo = baoCaoRepo ?? throw new ArgumentNullException(nameof(baoCaoRepo));
        _khachHangRepo = khachHangRepo ?? throw new ArgumentNullException(nameof(khachHangRepo));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<DuLieuKhachHangNhacNoDto> ExecuteAsync(int khachHangId, DateTime ngayChotBaoCao)
    {
        await AuthorizeHelper.RequireRoleAsync(_currentUserService, VaiTroConstants.KeToanCongNo);

        var khachHang = await _khachHangRepo.LayKhachHangTheoIdAsync(khachHangId)
            ?? throw new QuyTacNghiepVuException($"Không tìm thấy khách hàng với mã ID {khachHangId}.");

        var duLieuDoc = await _baoCaoRepo.LayDuLieuBaoCaoAsync(ngayChotBaoCao.Date);

        var rawRows = duLieuDoc.RawData
            .Where(r => r.KhachHangId == khachHangId)
            .ToList();

        var itemTuoiNo = new BaoCaoTuoiNoItem
        {
            KhachHangId = khachHang.KhachHangId,
            MaKhachHang = khachHang.MaKhachHang,
            TenKhachHang = khachHang.TenKhachHang,
            SoDienThoai = khachHang.SoDienThoai,
            HanMucCongNo = khachHang.HanMucCongNo
        };

        var hoaDonInputs = rawRows
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

        PhanTichTuoiNoService.PhanBoVaoNhomTuoiNo(itemTuoiNo, hoaDonInputs, ngayChotBaoCao.Date);

        var danhSachHoaDon = hoaDonInputs
            .Where(h => h.DuNoTaiNgay > 0)
            .Select(h => new HoaDonNhacNoItemDto
            {
                HoaDonId = h.HoaDonId,
                MaHoaDon = h.MaHoaDon,
                NgayLap = h.NgayLap,
                HanThanhToan = h.HanThanhToan,
                TongTien = h.TongTien,
                TongDaPhanBoCoHieuLuc = h.TongDaPhanBoCoHieuLuc,
                SoNgayQuaHan = PhanTichTuoiNoService.TinhSoNgayQuaHan(h.HanThanhToan, ngayChotBaoCao.Date)
            })
            .OrderBy(h => h.HanThanhToan)
            .ToList();

        var hash = TinhHashDuLieuCongNo(khachHangId, ngayChotBaoCao.Date, itemTuoiNo.TongCongNo, danhSachHoaDon);

        return new DuLieuKhachHangNhacNoDto
        {
            KhachHangId = khachHang.KhachHangId,
            MaKhachHang = khachHang.MaKhachHang,
            TenKhachHang = khachHang.TenKhachHang,
            Email = khachHang.Email,
            SoDienThoai = khachHang.SoDienThoai,
            NgayChotBaoCao = ngayChotBaoCao.Date,
            HanMucCongNo = khachHang.HanMucCongNo,
            TongCongNo = itemTuoiNo.TongCongNo,
            TuoiNo_0_30 = itemTuoiNo.TuoiNo_0_30,
            TuoiNo_31_60 = itemTuoiNo.TuoiNo_31_60,
            TuoiNo_61_90 = itemTuoiNo.TuoiNo_61_90,
            TuoiNo_Tren_90 = itemTuoiNo.TuoiNo_Tren_90,
            TienThuChuaPhanBo = duLieuDoc.TienThuChuaPhanBo,
            DanhSachHoaDon = danhSachHoaDon,
            HashDuLieuCongNo = hash
        };
    }

    public static string TinhHashDuLieuCongNo(int khachHangId, DateTime ngayChot, decimal tongNo, List<HoaDonNhacNoItemDto> hoaDons)
    {
        var sb = new StringBuilder();
        sb.Append($"{khachHangId}|{ngayChot:yyyy-MM-dd}|{tongNo:F2}|");
        foreach (var hd in hoaDons.OrderBy(h => h.HoaDonId))
        {
            sb.Append($"{hd.HoaDonId}:{hd.DuNoTaiNgayChot:F2}:{hd.SoNgayQuaHan};");
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }
}
