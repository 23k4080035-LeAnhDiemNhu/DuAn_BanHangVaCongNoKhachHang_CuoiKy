using BanHang.CoreBusiness;
using BanHang.UseCases.HoaDonScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.Tests;

public class QuyTacHanMucAndValidationTests
{
    private class FakeHoaDonRepository : IHoaDonRepository
    {
        public Task<IEnumerable<HoaDonListItemDto>> LayDanhSachHoaDonAsync()
        {
            return Task.FromResult(Enumerable.Empty<HoaDonListItemDto>());
        }

        public Task<TaoHoaDonResult> TaoHoaDonBanChiuAsync(TaoHoaDonRequest request)
        {
            return Task.FromResult(new TaoHoaDonResult
            {
                ThanhCong = true,
                HoaDonIdMoi = 999,
                TongTien = 1000000,
                ThongBao = "Thành công (Fake)"
            });
        }

        public Task<ChiTietHoaDonDto?> LayChiTietHoaDonAsync(int hoaDonId)
        {
            return Task.FromResult<ChiTietHoaDonDto?>(null);
        }
    }

    [Theory]
    [InlineData(3000000, 10000000, 5000000, false)] // 3M + 5M = 8M < 10M -> Hợp lệ (không vượt hạn mức)
    [InlineData(3000000, 10000000, 7000000, false)] // 3M + 7M = 10M == 10M -> Bằng hạn mức -> Hợp lệ (không vượt hạn mức)
    [InlineData(3000000, 10000000, 7000001, true)]  // 3M + 7.000.001 = 10.000.001 > 10M -> Vượt hạn mức
    [InlineData(0, 5000000, 5000000, false)]        // 0 + 5M == 5M -> Hợp lệ
    [InlineData(0, 5000000, 5000001, true)]         // 0 + 5.000.001 > 5M -> Vượt hạn mức
    public void KiemTraHanMuc_QuyTacTinhDung(decimal soDuCongNo, decimal hanMucCongNo, decimal donMoi, bool expectedVuotHanMuc)
    {
        // Arrange
        var kh = new KhachHang
        {
            KhachHangId = 1,
            MaKhachHang = "KH001",
            TenKhachHang = "Khách Hàng Test",
            HanMucCongNo = hanMucCongNo,
            SoDuCongNo = soDuCongNo
        };

        // Act
        var ketQua = kh.KiemTraVuotHanMucKhiMua(donMoi);

        // Assert
        Assert.Equal(expectedVuotHanMuc, ketQua);
    }

    [Fact]
    public async Task TaoHoaDon_KhiKhongCoDongHang_BaoLoi()
    {
        var repo = new FakeHoaDonRepository();
        var useCase = new TaoHoaDonBanChiuUseCase(repo, FakeCurrentUserService.NhanVienBanHang());

        var request = new TaoHoaDonRequest
        {
            MaHoaDon = "HDTEST",
            KhachHangId = 1,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(7),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>()
        };

        var result = await useCase.ExecuteAsync(request);

        Assert.False(result.ThanhCong);
        Assert.Contains("ít nhất một dòng", result.ThongBao);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task TaoHoaDon_KhiSoLuongKhongHopLe_BaoLoi(int soLuong)
    {
        var repo = new FakeHoaDonRepository();
        var useCase = new TaoHoaDonBanChiuUseCase(repo, FakeCurrentUserService.NhanVienBanHang());

        var request = new TaoHoaDonRequest
        {
            MaHoaDon = "HDTEST",
            KhachHangId = 1,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(7),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = 1, SoLuong = soLuong, DonGiaHienThi = 100000 }
            }
        };

        var result = await useCase.ExecuteAsync(request);

        Assert.False(result.ThanhCong);
        Assert.Contains("nguyên dương", result.ThongBao);
    }

    [Fact]
    public async Task TaoHoaDon_KhiHanThanhToanTruocNgayLap_BaoLoi()
    {
        var repo = new FakeHoaDonRepository();
        var useCase = new TaoHoaDonBanChiuUseCase(repo, FakeCurrentUserService.NhanVienBanHang());

        var request = new TaoHoaDonRequest
        {
            MaHoaDon = "HDTEST",
            KhachHangId = 1,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(-1), // Trước ngày lập
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = 1, SoLuong = 2, DonGiaHienThi = 100000 }
            }
        };

        var result = await useCase.ExecuteAsync(request);

        Assert.False(result.ThanhCong);
        Assert.Contains("Hạn thanh toán không được trước ngày lập", result.ThongBao);
    }

    [Fact]
    public async Task TaoHoaDon_KhiHanThanhToanChuaChon_BaoLoi()
    {
        var repo = new FakeHoaDonRepository();
        var useCase = new TaoHoaDonBanChiuUseCase(repo, FakeCurrentUserService.NhanVienBanHang());

        var request = new TaoHoaDonRequest
        {
            MaHoaDon = "HDTEST",
            KhachHangId = 1,
            NgayLap = DateTime.Today,
            HanThanhToan = null, // Chưa chọn hạn thanh toán
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = 1, SoLuong = 2, DonGiaHienThi = 100000 }
            }
        };

        var result = await useCase.ExecuteAsync(request);

        Assert.False(result.ThanhCong);
        Assert.Contains("chọn hạn thanh toán", result.ThongBao);
    }

    [Fact]
    public async Task TaoHoaDon_KhiMaHoaDonRongHoacQuaDai_BaoLoi()
    {
        var repo = new FakeHoaDonRepository();
        var useCase = new TaoHoaDonBanChiuUseCase(repo, FakeCurrentUserService.NhanVienBanHang());

        // Test trống
        var reqRong = new TaoHoaDonRequest
        {
            MaHoaDon = "   ",
            KhachHangId = 1,
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = 1, SoLuong = 1, DonGiaHienThi = 100000 }
            }
        };
        var resRong = await useCase.ExecuteAsync(reqRong);
        Assert.False(resRong.ThanhCong);
        Assert.Contains("nhập mã hóa đơn", resRong.ThongBao);

        // Test quá 20 ký tự
        var reqDai = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_QUY_TAC_MA_HOA_DON_QUA_20_KY_TU",
            KhachHangId = 1,
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = 1, SoLuong = 1, DonGiaHienThi = 100000 }
            }
        };
        var resDai = await useCase.ExecuteAsync(reqDai);
        Assert.False(resDai.ThanhCong);
        Assert.Contains("tối đa 20 ký tự", resDai.ThongBao);
    }

    [Fact]
    public async Task TaoHoaDon_KhiChonTrungSanPham_BaoLoi()
    {
        var repo = new FakeHoaDonRepository();
        var useCase = new TaoHoaDonBanChiuUseCase(repo, FakeCurrentUserService.NhanVienBanHang());

        var request = new TaoHoaDonRequest
        {
            MaHoaDon = "HDTEST",
            KhachHangId = 1,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(7),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = 1, SoLuong = 2, DonGiaHienThi = 100000 },
                new TaoChiTietHoaDonDto { SanPhamId = 1, SoLuong = 3, DonGiaHienThi = 100000 } // Trùng SanPhamId = 1
            }
        };

        var result = await useCase.ExecuteAsync(request);

        Assert.False(result.ThanhCong);
        Assert.Contains("trùng", result.ThongBao);
    }
}
