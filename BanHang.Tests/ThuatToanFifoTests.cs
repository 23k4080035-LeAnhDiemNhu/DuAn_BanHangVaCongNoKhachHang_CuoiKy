using BanHang.CoreBusiness;

namespace BanHang.Tests;

public class ThuatToanFifoTests
{
    [Fact]
    public void Test01_TraMotPhanMotHoaDon()
    {
        // 1 hóa đơn 10M, thanh toán 4M
        var hoaDons = new List<HoaDonNoModel>
        {
            new HoaDonNoModel { HoaDonId = 1, MaHoaDon = "HD001", NgayLap = DateTime.Today, TongTien = 10000000, DaThanhToan = 0, ConLai = 10000000 }
        };

        var ketQua = ThuatToanFIFO.PhanBo(hoaDons, 4000000, DateTime.Today);

        Assert.Equal(4000000m, ketQua.TongDaPhanBo);
        Assert.Equal(0m, ketQua.TienChuaPhanBo);
        Assert.Single(ketQua.DanhSachPhanBo);
        var pb = ketQua.DanhSachPhanBo[0];
        Assert.Equal(1, pb.HoaDonId);
        Assert.Equal(4000000m, pb.SoTienPhanBo);
        Assert.Equal(6000000m, pb.SoNoSauPhanBo);
        Assert.False(pb.DaThanhToanXong);
    }

    [Fact]
    public void Test02_TraVuaDuMotHoaDon()
    {
        // 1 hóa đơn 10M, thanh toán 10M
        var hoaDons = new List<HoaDonNoModel>
        {
            new HoaDonNoModel { HoaDonId = 1, MaHoaDon = "HD001", NgayLap = DateTime.Today, TongTien = 10000000, DaThanhToan = 0, ConLai = 10000000 }
        };

        var ketQua = ThuatToanFIFO.PhanBo(hoaDons, 10000000, DateTime.Today);

        Assert.Equal(10000000m, ketQua.TongDaPhanBo);
        Assert.Equal(0m, ketQua.TienChuaPhanBo);
        Assert.Single(ketQua.DanhSachPhanBo);
        var pb = ketQua.DanhSachPhanBo[0];
        Assert.Equal(10000000m, pb.SoTienPhanBo);
        Assert.Equal(0m, pb.SoNoSauPhanBo);
        Assert.True(pb.DaThanhToanXong);
    }

    [Fact]
    public void Test03_TraNhieuHoaDonTheoThuTuCuDenMoi()
    {
        // HD1: 3M (01/10), HD2: 5M (05/10), HD3: 4M (10/10). Tổng = 12M.
        // Thanh toán 7M
        var hoaDons = new List<HoaDonNoModel>
        {
            new HoaDonNoModel { HoaDonId = 2, MaHoaDon = "HD002", NgayLap = new DateTime(2026, 10, 5), TongTien = 5000000, ConLai = 5000000 },
            new HoaDonNoModel { HoaDonId = 1, MaHoaDon = "HD001", NgayLap = new DateTime(2026, 10, 1), TongTien = 3000000, ConLai = 3000000 },
            new HoaDonNoModel { HoaDonId = 3, MaHoaDon = "HD003", NgayLap = new DateTime(2026, 10, 10), TongTien = 4000000, ConLai = 4000000 }
        };

        var ketQua = ThuatToanFIFO.PhanBo(hoaDons, 7000000, new DateTime(2026, 10, 15));

        Assert.Equal(7000000m, ketQua.TongDaPhanBo);
        Assert.Equal(0m, ketQua.TienChuaPhanBo);
        Assert.Equal(2, ketQua.DanhSachPhanBo.Count);

        // HD1 trả trước 3M -> hết nợ
        Assert.Equal(1, ketQua.DanhSachPhanBo[0].HoaDonId);
        Assert.Equal(3000000m, ketQua.DanhSachPhanBo[0].SoTienPhanBo);
        Assert.True(ketQua.DanhSachPhanBo[0].DaThanhToanXong);

        // HD2 trả tiếp 4M -> còn nợ 1M
        Assert.Equal(2, ketQua.DanhSachPhanBo[1].HoaDonId);
        Assert.Equal(4000000m, ketQua.DanhSachPhanBo[1].SoTienPhanBo);
        Assert.Equal(1000000m, ketQua.DanhSachPhanBo[1].SoNoSauPhanBo);
        Assert.False(ketQua.DanhSachPhanBo[1].DaThanhToanXong);
    }

    [Fact]
    public void Test04_TienThuVuotTongNo_GiuTienDuChuaPhanBo_KhongTaoNoAm()
    {
        // HD1: 2M, HD2: 3M. Tổng nợ 5M.
        // Khách nộp 8M -> Phân bổ 5M, dư 3M chưa phân bổ
        var hoaDons = new List<HoaDonNoModel>
        {
            new HoaDonNoModel { HoaDonId = 1, MaHoaDon = "HD001", NgayLap = DateTime.Today, TongTien = 2000000, ConLai = 2000000 },
            new HoaDonNoModel { HoaDonId = 2, MaHoaDon = "HD002", NgayLap = DateTime.Today.AddDays(1), TongTien = 3000000, ConLai = 3000000 }
        };

        var ketQua = ThuatToanFIFO.PhanBo(hoaDons, 8000000, DateTime.Today.AddDays(5));

        Assert.Equal(5000000m, ketQua.TongDaPhanBo);
        Assert.Equal(3000000m, ketQua.TienChuaPhanBo); // 3M dư
        Assert.Equal(2, ketQua.DanhSachPhanBo.Count);
        Assert.All(ketQua.DanhSachPhanBo, pb => Assert.True(pb.DaThanhToanXong));
    }

    [Fact]
    public void Test05_KhachHangKhongCoNo()
    {
        // Không có hóa đơn nợ nào
        var hoaDons = new List<HoaDonNoModel>();

        var ketQua = ThuatToanFIFO.PhanBo(hoaDons, 2000000, DateTime.Today);

        Assert.Equal(0m, ketQua.TongDaPhanBo);
        Assert.Equal(2000000m, ketQua.TienChuaPhanBo);
        Assert.Empty(ketQua.DanhSachPhanBo);
    }

    [Fact]
    public void Test06_TrungNgayLap_SapXepTheoHoaDonIdTangDan()
    {
        // Cùng ngày lập, HD có Id nhỏ hơn phải được trả trước
        var hoaDons = new List<HoaDonNoModel>
        {
            new HoaDonNoModel { HoaDonId = 20, MaHoaDon = "HD020", NgayLap = new DateTime(2026, 10, 1), TongTien = 5000000, ConLai = 5000000 },
            new HoaDonNoModel { HoaDonId = 5,  MaHoaDon = "HD005", NgayLap = new DateTime(2026, 10, 1), TongTien = 5000000, ConLai = 5000000 }
        };

        var ketQua = ThuatToanFIFO.PhanBo(hoaDons, 6000000, new DateTime(2026, 10, 1));

        Assert.Equal(2, ketQua.DanhSachPhanBo.Count);
        // HD 5 phải được phân bổ trước 5M
        Assert.Equal(5, ketQua.DanhSachPhanBo[0].HoaDonId);
        Assert.Equal(5000000m, ketQua.DanhSachPhanBo[0].SoTienPhanBo);
        Assert.True(ketQua.DanhSachPhanBo[0].DaThanhToanXong);

        // HD 20 nhận 1M còn lại
        Assert.Equal(20, ketQua.DanhSachPhanBo[1].HoaDonId);
        Assert.Equal(1000000m, ketQua.DanhSachPhanBo[1].SoTienPhanBo);
        Assert.Equal(4000000m, ketQua.DanhSachPhanBo[1].SoNoSauPhanBo);
    }

    [Fact]
    public void Test07_LoaiBoHoaDonLapSauNgayThu_KhiGhiLuiNgay()
    {
        // Ngày thu: 05/10
        // HD1: lập 01/10 (3M) -> Hợp lệ
        // HD2: lập 10/10 (4M) -> Lập SAU ngày thu -> Không được phân bổ
        var ngayThu = new DateTime(2026, 10, 5);
        var hoaDons = new List<HoaDonNoModel>
        {
            new HoaDonNoModel { HoaDonId = 1, MaHoaDon = "HD001", NgayLap = new DateTime(2026, 10, 1), TongTien = 3000000, ConLai = 3000000 },
            new HoaDonNoModel { HoaDonId = 2, MaHoaDon = "HD002", NgayLap = new DateTime(2026, 10, 10), TongTien = 4000000, ConLai = 4000000 }
        };

        var ketQua = ThuatToanFIFO.PhanBo(hoaDons, 5000000, ngayThu);

        Assert.Equal(1, ketQua.SoHoaDonBiLoaiBoDoLapSauNgayThu);
        Assert.Single(ketQua.DanhSachPhanBo);
        Assert.Equal(1, ketQua.DanhSachPhanBo[0].HoaDonId);
        Assert.Equal(3000000m, ketQua.DanhSachPhanBo[0].SoTienPhanBo);
        Assert.Equal(2000000m, ketQua.TienChuaPhanBo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100000)]
    [InlineData(100000.001)] // Quá 2 chữ số thập phân
    public void Test08_SoTienKhongHopLe_NemNgoaiLe(decimal soTien)
    {
        var hoaDons = new List<HoaDonNoModel>
        {
            new HoaDonNoModel { HoaDonId = 1, MaHoaDon = "HD001", NgayLap = DateTime.Today, TongTien = 5000000, ConLai = 5000000 }
        };

        Assert.Throws<QuyTacNghiepVuException>(() =>
        {
            ThuatToanFIFO.PhanBo(hoaDons, soTien, DateTime.Today);
        });
    }
}
