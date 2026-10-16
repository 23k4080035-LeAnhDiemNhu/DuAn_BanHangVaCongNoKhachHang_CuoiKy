using System;
using System.Collections.Generic;
using BanHang.CoreBusiness;
using Xunit;

namespace BanHang.Tests;

public class BaoCaoTuoiNoUnitTests
{
    private readonly DateTime _ngayBaoCao = new DateTime(2026, 10, 16);

    [Fact]
    public void Test01_TinhSoNgayQuaHan_ChuaDenHan_TraVe0()
    {
        var hanThanhToan = new DateTime(2026, 10, 20); // Chưa đến hạn (sau ngày báo cáo 4 ngày)
        int soNgay = PhanTichTuoiNoService.TinhSoNgayQuaHan(hanThanhToan, _ngayBaoCao);
        Assert.Equal(0, soNgay);
    }

    [Fact]
    public void Test02_TinhSoNgayQuaHan_DenHanHomNay_TraVe0()
    {
        var hanThanhToan = new DateTime(2026, 10, 16); // Đúng ngày báo cáo
        int soNgay = PhanTichTuoiNoService.TinhSoNgayQuaHan(hanThanhToan, _ngayBaoCao);
        Assert.Equal(0, soNgay);
    }

    [Theory]
    [InlineData(0, 0)]      // Chưa đến hạn hoặc đến hạn hôm nay -> 0 ngày
    [InlineData(1, 1)]      // Quá hạn 1 ngày
    [InlineData(30, 30)]    // Quá hạn 30 ngày -> Biên trên nhóm 0-30
    [InlineData(31, 31)]    // Quá hạn 31 ngày -> Biên dưới nhóm 31-60
    [InlineData(60, 60)]    // Quá hạn 60 ngày -> Biên trên nhóm 31-60
    [InlineData(61, 61)]    // Quá hạn 61 ngày -> Biên dưới nhóm 61-90
    [InlineData(90, 90)]    // Quá hạn 90 ngày -> Biên trên nhóm 61-90
    [InlineData(91, 91)]    // Quá hạn 91 ngày -> Nhóm trên 90
    [InlineData(120, 120)]  // Quá hạn 120 ngày -> Nhóm trên 90
    public void Test03_BienNgay_SoNgayQuaHan_ChinhXac(int soNgayTru, int soNgayExpected)
    {
        var hanThanhToan = _ngayBaoCao.AddDays(-soNgayTru);
        int soNgay = PhanTichTuoiNoService.TinhSoNgayQuaHan(hanThanhToan, _ngayBaoCao);
        Assert.Equal(soNgayExpected, soNgay);
    }

    [Fact]
    public void Test04_BienNgay_PhanVaoDung4NhomTuoiNo()
    {
        var item = new BaoCaoTuoiNoItem { MaKhachHang = "KH_TEST" };
        var hoaDons = new List<HoaDonTuoiNoInput>
        {
            // Chưa đến hạn -> Nhóm 0-30
            new() { HoaDonId = 1, MaHoaDon = "HD1", NgayLap = _ngayBaoCao.AddDays(-10), HanThanhToan = _ngayBaoCao.AddDays(5), TongTien = 1000000, TongDaPhanBoCoHieuLuc = 0 },
            // Quá hạn 1 ngày -> Nhóm 0-30
            new() { HoaDonId = 2, MaHoaDon = "HD2", NgayLap = _ngayBaoCao.AddDays(-20), HanThanhToan = _ngayBaoCao.AddDays(-1), TongTien = 2000000, TongDaPhanBoCoHieuLuc = 0 },
            // Quá hạn 30 ngày -> Nhóm 0-30
            new() { HoaDonId = 3, MaHoaDon = "HD3", NgayLap = _ngayBaoCao.AddDays(-40), HanThanhToan = _ngayBaoCao.AddDays(-30), TongTien = 3000000, TongDaPhanBoCoHieuLuc = 0 },
            // Quá hạn 31 ngày -> Nhóm 31-60
            new() { HoaDonId = 4, MaHoaDon = "HD4", NgayLap = _ngayBaoCao.AddDays(-50), HanThanhToan = _ngayBaoCao.AddDays(-31), TongTien = 4000000, TongDaPhanBoCoHieuLuc = 0 },
            // Quá hạn 60 ngày -> Nhóm 31-60
            new() { HoaDonId = 5, MaHoaDon = "HD5", NgayLap = _ngayBaoCao.AddDays(-80), HanThanhToan = _ngayBaoCao.AddDays(-60), TongTien = 5000000, TongDaPhanBoCoHieuLuc = 0 },
            // Quá hạn 61 ngày -> Nhóm 61-90
            new() { HoaDonId = 6, MaHoaDon = "HD6", NgayLap = _ngayBaoCao.AddDays(-90), HanThanhToan = _ngayBaoCao.AddDays(-61), TongTien = 6000000, TongDaPhanBoCoHieuLuc = 0 },
            // Quá hạn 90 ngày -> Nhóm 61-90
            new() { HoaDonId = 7, MaHoaDon = "HD7", NgayLap = _ngayBaoCao.AddDays(-120), HanThanhToan = _ngayBaoCao.AddDays(-90), TongTien = 7000000, TongDaPhanBoCoHieuLuc = 0 },
            // Quá hạn 91 ngày -> Nhóm Trên 90
            new() { HoaDonId = 8, MaHoaDon = "HD8", NgayLap = _ngayBaoCao.AddDays(-150), HanThanhToan = _ngayBaoCao.AddDays(-91), TongTien = 8000000, TongDaPhanBoCoHieuLuc = 0 }
        };

        PhanTichTuoiNoService.PhanBoVaoNhomTuoiNo(item, hoaDons, _ngayBaoCao);

        // Nhóm 0-30 = HD1 (1M) + HD2 (2M) + HD3 (3M) = 6M
        Assert.Equal(6000000m, item.TuoiNo_0_30);
        // Nhóm 31-60 = HD4 (4M) + HD5 (5M) = 9M
        Assert.Equal(9000000m, item.TuoiNo_31_60);
        // Nhóm 61-90 = HD6 (6M) + HD7 (7M) = 13M
        Assert.Equal(13000000m, item.TuoiNo_61_90);
        // Nhóm Trên 90 = HD8 (8M) = 8M
        Assert.Equal(8000000m, item.TuoiNo_Tren_90);
        // Tổng công nợ = 6M + 9M + 13M + 8M = 36M
        Assert.Equal(36000000m, item.TongCongNo);
    }

    [Fact]
    public void Test05_LoaiBoHoaDonLapSauNgayBaoCao()
    {
        var item = new BaoCaoTuoiNoItem { MaKhachHang = "KH_TEST" };
        var hoaDons = new List<HoaDonTuoiNoInput>
        {
            // Hóa đơn lập hôm nay -> Tính
            new() { HoaDonId = 1, MaHoaDon = "HD1", NgayLap = _ngayBaoCao, HanThanhToan = _ngayBaoCao.AddDays(15), TongTien = 5000000, TongDaPhanBoCoHieuLuc = 0 },
            // Hóa đơn lập ngày mai -> Bị loại bỏ
            new() { HoaDonId = 2, MaHoaDon = "HD2", NgayLap = _ngayBaoCao.AddDays(1), HanThanhToan = _ngayBaoCao.AddDays(16), TongTien = 3000000, TongDaPhanBoCoHieuLuc = 0 }
        };

        PhanTichTuoiNoService.PhanBoVaoNhomTuoiNo(item, hoaDons, _ngayBaoCao);

        Assert.Equal(5000000m, item.TongCongNo);
        Assert.Equal(5000000m, item.TuoiNo_0_30);
    }

    [Fact]
    public void Test06_HoaDonTraHet_KhongDuaVaoNhom()
    {
        var item = new BaoCaoTuoiNoItem { MaKhachHang = "KH_TEST" };
        var hoaDons = new List<HoaDonTuoiNoInput>
        {
            // Đã trả hết -> Dư nợ = 0
            new() { HoaDonId = 1, MaHoaDon = "HD1", NgayLap = _ngayBaoCao.AddDays(-40), HanThanhToan = _ngayBaoCao.AddDays(-10), TongTien = 5000000, TongDaPhanBoCoHieuLuc = 5000000 },
            // Chưa trả -> Dư nợ = 2M
            new() { HoaDonId = 2, MaHoaDon = "HD2", NgayLap = _ngayBaoCao.AddDays(-20), HanThanhToan = _ngayBaoCao.AddDays(5), TongTien = 2000000, TongDaPhanBoCoHieuLuc = 0 }
        };

        PhanTichTuoiNoService.PhanBoVaoNhomTuoiNo(item, hoaDons, _ngayBaoCao);

        Assert.Equal(2000000m, item.TongCongNo);
        Assert.Equal(2000000m, item.TuoiNo_0_30);
    }

    [Fact]
    public void Test07_HoaDonTraMotPhan_ChiDuaDuNoConLaiVaoNhom()
    {
        var item = new BaoCaoTuoiNoItem { MaKhachHang = "KH_TEST" };
        var hoaDons = new List<HoaDonTuoiNoInput>
        {
            // Tổng tiền 10M, đã phân bổ 4M -> Dư nợ còn lại 6M, quá hạn 45 ngày (nhóm 31-60)
            new() { HoaDonId = 1, MaHoaDon = "HD1", NgayLap = _ngayBaoCao.AddDays(-60), HanThanhToan = _ngayBaoCao.AddDays(-45), TongTien = 10000000, TongDaPhanBoCoHieuLuc = 4000000 }
        };

        PhanTichTuoiNoService.PhanBoVaoNhomTuoiNo(item, hoaDons, _ngayBaoCao);

        Assert.Equal(6000000m, item.TongCongNo);
        Assert.Equal(6000000m, item.TuoiNo_31_60);
        Assert.Equal(0m, item.TuoiNo_0_30);
    }

    [Fact]
    public void Test08_PhanBoVuotTongTienHoaDon_BaoLoiDuLieuKhongNhatQuan()
    {
        var item = new BaoCaoTuoiNoItem { MaKhachHang = "KH_TEST" };
        var hoaDons = new List<HoaDonTuoiNoInput>
        {
            // Tổng phân bổ 12M > Tổng tiền 10M -> Dữ liệu không nhất quán
            new() { HoaDonId = 1, MaHoaDon = "HD_LOI", NgayLap = _ngayBaoCao.AddDays(-10), HanThanhToan = _ngayBaoCao.AddDays(10), TongTien = 10000000, TongDaPhanBoCoHieuLuc = 12000000 }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            PhanTichTuoiNoService.PhanBoVaoNhomTuoiNo(item, hoaDons, _ngayBaoCao));

        Assert.Contains("không nhất quán", ex.Message);
        Assert.Contains("HD_LOI", ex.Message);
    }
}
