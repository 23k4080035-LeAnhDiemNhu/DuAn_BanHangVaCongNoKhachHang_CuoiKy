using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.SanPhamScreen;
using Xunit;

namespace BanHang.Tests;

public class SanPhamUnitTests
{
    private class FakeSanPhamRepository : ISanPhamRepository
    {
        public HashSet<string> ExistingCodes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<int, SanPham> Products { get; } = new();

        public Task<IEnumerable<SanPham>> LayDanhSachSanPhamHoatDongAsync()
        {
            return Task.FromResult(Products.Values.Where(p => p.TrangThai));
        }

        public Task<IEnumerable<SanPham>> LayDanhSachTatCaSanPhamAsync()
        {
            return Task.FromResult(Products.Values.AsEnumerable());
        }

        public Task<SanPham?> LaySanPhamTheoIdAsync(int sanPhamId)
        {
            Products.TryGetValue(sanPhamId, out var sp);
            return Task.FromResult<SanPham?>(sp);
        }

        public Task<bool> KiemTraTrungMaSanPhamAsync(string maSanPham)
        {
            return Task.FromResult(ExistingCodes.Contains(maSanPham));
        }

        public Task<TaoSanPhamResult> ThemSanPhamAsync(TaoSanPhamRequest request)
        {
            var code = string.IsNullOrWhiteSpace(request.MaSanPham) ? "SP001" : request.MaSanPham;
            ExistingCodes.Add(code);
            return Task.FromResult(new TaoSanPhamResult { ThanhCong = true, SanPhamIdMoi = 100, MaSanPhamMoi = code });
        }

        public Task<string> LayMaSanPhamTiepTheoAsync()
        {
            return Task.FromResult("SP001");
        }

        public Task<CapNhatSanPhamResult> CapNhatSanPhamAsync(CapNhatSanPhamRequest request)
        {
            return Task.FromResult(new CapNhatSanPhamResult { ThanhCong = true });
        }

        public Task<DoiTrangThaiSanPhamResult> DoiTrangThaiSanPhamAsync(int sanPhamId, bool trangThaiMoi)
        {
            return Task.FromResult(new DoiTrangThaiSanPhamResult { ThanhCong = true, TrangThaiMoi = trangThaiMoi });
        }
    }

    private readonly FakeSanPhamRepository _fakeRepo;
    private readonly ThemSanPhamUseCase _themUseCase;
    private readonly CapNhatSanPhamUseCase _capNhatUseCase;
    private readonly DoiTrangThaiSanPhamUseCase _doiTrangThaiUseCase;

    public SanPhamUnitTests()
    {
        _fakeRepo = new FakeSanPhamRepository();
        var ktcnUser = FakeCurrentUserService.KeToanCongNo();
        _themUseCase = new ThemSanPhamUseCase(_fakeRepo, ktcnUser);
        _capNhatUseCase = new CapNhatSanPhamUseCase(_fakeRepo, ktcnUser);
        _doiTrangThaiUseCase = new DoiTrangThaiSanPhamUseCase(_fakeRepo, ktcnUser);
    }

    [Fact]
    public async Task Test01_ThemSanPham_HopLe_TrimKhoangTrang_ThanhCong()
    {
        var req = new TaoSanPhamRequest
        {
            MaSanPham = "  SP_TEST_TRIM  ",
            TenSanPham = "  Màn hình Gaming 240Hz  ",
            DonViTinh = "  Cái  ",
            DonGia = 8500000.50m,
            MoTa = "  Màn hình chuyên game  ",
            HinhAnh = "  /images/sp1.png  ",
            TrangThai = true
        };

        var result = await _themUseCase.ExecuteAsync(req);

        Assert.True(result.ThanhCong);
        Assert.Equal("SP_TEST_TRIM", req.MaSanPham);
        Assert.Equal("Màn hình Gaming 240Hz", req.TenSanPham);
        Assert.Equal("Cái", req.DonViTinh);
        Assert.Equal("Màn hình chuyên game", req.MoTa);
        Assert.Equal("/images/sp1.png", req.HinhAnh);
    }

    [Theory]
    [InlineData("SP01", "", "Cái", "TenSanPham")]
    [InlineData("SP01", "   ", "Cái", "TenSanPham")]
    [InlineData("SP01", "Tên hợp lệ", "", "DonViTinh")]
    [InlineData("SP01", "Tên hợp lệ", "   ", "DonViTinh")]
    public async Task Test02_ThemSanPham_ThieuTruongBatBuoc_BaoLoiTheoTruong(string ma, string ten, string dvt, string truongLoiExpected)
    {
        var req = new TaoSanPhamRequest
        {
            MaSanPham = ma,
            TenSanPham = ten,
            DonViTinh = dvt,
            DonGia = 100000
        };

        var result = await _themUseCase.ExecuteAsync(req);

        Assert.False(result.ThanhCong);
        Assert.True(result.LoiTheoTruong.ContainsKey(truongLoiExpected));
    }

    [Fact]
    public async Task Test02b_ThemSanPham_DeTrongMa_TuDongCapMa_ThanhCong()
    {
        var req = new TaoSanPhamRequest
        {
            MaSanPham = "",
            TenSanPham = "Sản phẩm tự sinh mã",
            DonViTinh = "Cái",
            DonGia = 250000
        };

        var result = await _themUseCase.ExecuteAsync(req);

        Assert.True(result.ThanhCong);
        Assert.False(result.LoiTheoTruong.ContainsKey("MaSanPham"));
    }

    [Fact]
    public async Task Test03_ThemSanPham_DoDaiVuotSchema_BaoLoi()
    {
        var req = new TaoSanPhamRequest
        {
            MaSanPham = new string('A', 21),       // max 20
            TenSanPham = new string('B', 201),     // max 200
            DonViTinh = new string('C', 51),       // max 50
            MoTa = new string('D', 501),            // max 500
            HinhAnh = new string('E', 301),         // max 300
            DonGia = 100000
        };

        var result = await _themUseCase.ExecuteAsync(req);

        Assert.False(result.ThanhCong);
        Assert.True(result.LoiTheoTruong.ContainsKey("MaSanPham"));
        Assert.True(result.LoiTheoTruong.ContainsKey("TenSanPham"));
        Assert.True(result.LoiTheoTruong.ContainsKey("DonViTinh"));
        Assert.True(result.LoiTheoTruong.ContainsKey("MoTa"));
        Assert.True(result.LoiTheoTruong.ContainsKey("HinhAnh"));
    }

    [Theory]
    [InlineData(-1000)]
    [InlineData(-0.01)]
    public async Task Test04_ThemSanPham_DonGiaAm_BaoLoi(decimal giaAm)
    {
        var req = new TaoSanPhamRequest
        {
            MaSanPham = "SP_GIA_AM",
            TenSanPham = "Sản phẩm giá âm",
            DonViTinh = "Cái",
            DonGia = giaAm
        };

        var result = await _themUseCase.ExecuteAsync(req);

        Assert.False(result.ThanhCong);
        Assert.True(result.LoiTheoTruong.ContainsKey("DonGia"));
    }

    [Fact]
    public async Task Test05_ThemSanPham_DonGiaLeHon2ChuSo_BiTuChoiKhongLamTron()
    {
        var req = new TaoSanPhamRequest
        {
            MaSanPham = "SP_LE",
            TenSanPham = "Sản phẩm giá lẻ",
            DonViTinh = "Cái",
            DonGia = 500000.123m // 3 chữ số thập phân
        };

        var result = await _themUseCase.ExecuteAsync(req);

        Assert.False(result.ThanhCong);
        Assert.True(result.LoiTheoTruong.ContainsKey("DonGia"));
    }

    [Fact]
    public async Task Test06_ThemSanPham_DonGiaVuotGioiHanDecimal18_2_BaoLoi()
    {
        var req = new TaoSanPhamRequest
        {
            MaSanPham = "SP_OVERFLOW",
            TenSanPham = "Sản phẩm giá cực đại",
            DonViTinh = "Cái",
            DonGia = 10000000000000000.00m // > 9999999999999999.99m (vượt 18 số)
        };

        var result = await _themUseCase.ExecuteAsync(req);

        Assert.False(result.ThanhCong);
        Assert.True(result.LoiTheoTruong.ContainsKey("DonGia"));
    }

    [Fact]
    public async Task Test07_ThemSanPham_TrungMa_BaoLoiThanThien()
    {
        _fakeRepo.ExistingCodes.Add("SP_TRUNG");

        var req = new TaoSanPhamRequest
        {
            MaSanPham = "SP_TRUNG",
            TenSanPham = "Sản phẩm trùng mã",
            DonViTinh = "Cái",
            DonGia = 100000
        };

        var result = await _themUseCase.ExecuteAsync(req);

        Assert.False(result.ThanhCong);
        Assert.True(result.LoiTheoTruong.ContainsKey("MaSanPham"));
        Assert.Contains("đã được sử dụng", result.ThongBao);
    }

    [Fact]
    public async Task Test08_CapNhatSanPham_ThieuTenHoacDonViTinh_BaoLoi()
    {
        var req = new CapNhatSanPhamRequest
        {
            SanPhamId = 1,
            MaSanPham = "SP01",
            TenSanPham = "",
            DonViTinh = "",
            DonGia = 100000
        };

        var result = await _capNhatUseCase.ExecuteAsync(req);

        Assert.False(result.ThanhCong);
        Assert.True(result.LoiTheoTruong.ContainsKey("TenSanPham"));
        Assert.True(result.LoiTheoTruong.ContainsKey("DonViTinh"));
    }

    [Fact]
    public async Task Test09_DoiTrangThaiSanPham_IdKhongHopLe_BaoLoi()
    {
        var result = await _doiTrangThaiUseCase.ExecuteAsync(0, false);
        Assert.False(result.ThanhCong);
        Assert.Contains("không hợp lệ", result.ThongBao);
    }
}
