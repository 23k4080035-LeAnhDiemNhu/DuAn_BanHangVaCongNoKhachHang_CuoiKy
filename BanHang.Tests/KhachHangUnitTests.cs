using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.KhachHangScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using Xunit;

namespace BanHang.Tests;

public class KhachHangUnitTests
{
    private class FakeKhachHangRepository : IKhachHangRepository
    {
        public HashSet<string> ExistingCodes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<int, decimal> CustomerDebts { get; } = new();

        public Task<IEnumerable<KhachHang>> LayDanhSachKhachHangAsync()
        {
            return Task.FromResult(Enumerable.Empty<KhachHang>());
        }

        public Task<KhachHang?> LayKhachHangTheoIdAsync(int khachHangId)
        {
            return Task.FromResult<KhachHang?>(new KhachHang { KhachHangId = khachHangId, TenKhachHang = "Test", MaKhachHang = "TEST", TrangThai = true });
        }

        public Task<bool> KiemTraTrungMaKhachHangAsync(string maKhachHang, int? loaiTruKhachHangId = null)
        {
            return Task.FromResult(ExistingCodes.Contains(maKhachHang));
        }

        public Task<decimal> LayCongNoHienTaiCuaKhachHangAsync(int khachHangId)
        {
            CustomerDebts.TryGetValue(khachHangId, out var debt);
            return Task.FromResult(debt);
        }

        public Task<TaoKhachHangResult> ThemKhachHangAsync(TaoKhachHangRequest request)
        {
            ExistingCodes.Add(request.MaKhachHang);
            return Task.FromResult(new TaoKhachHangResult { ThanhCong = true, KhachHangIdMoi = 10 });
        }

        public Task<CapNhatKhachHangResult> CapNhatKhachHangAsync(CapNhatKhachHangRequest request)
        {
            return Task.FromResult(new CapNhatKhachHangResult { ThanhCong = true });
        }

        public Task<DoiTrangThaiKhachHangResult> DoiTrangThaiKhachHangAsync(int khachHangId, bool trangThaiMoi)
        {
            return Task.FromResult(new DoiTrangThaiKhachHangResult { ThanhCong = true });
        }
    }

    private readonly FakeKhachHangRepository _fakeRepo;
    private readonly ThemKhachHangUseCase _themUseCase;
    private readonly CapNhatKhachHangUseCase _capNhatUseCase;
    private readonly DoiTrangThaiKhachHangUseCase _doiTrangThaiUseCase;

    public KhachHangUnitTests()
    {
        _fakeRepo = new FakeKhachHangRepository();
        var ktcnUser = FakeCurrentUserService.KeToanCongNo();
        _themUseCase = new ThemKhachHangUseCase(_fakeRepo, ktcnUser);
        _capNhatUseCase = new CapNhatKhachHangUseCase(_fakeRepo, ktcnUser);
        _doiTrangThaiUseCase = new DoiTrangThaiKhachHangUseCase(_fakeRepo, ktcnUser);
    }

    [Fact]
    public async Task Test01_ThemKhachHang_HopLe_TrimKhoangTrang_ThanhCong()
    {
        var req = new TaoKhachHangRequest
        {
            MaKhachHang = "  KH_NEW  ",
            TenKhachHang = "  Công ty ABC  ",
            SoDienThoai = " 0901234567 ",
            Email = " abc@gmail.com ",
            DiaChi = " 123 Đường Lê Lợi ",
            HanMucCongNo = 10000000.50m
        };

        var result = await _themUseCase.ExecuteAsync(req);

        Assert.True(result.ThanhCong);
        Assert.Equal("KH_NEW", req.MaKhachHang);
        Assert.Equal("Công ty ABC", req.TenKhachHang);
        Assert.Equal("0901234567", req.SoDienThoai);
        Assert.Equal("abc@gmail.com", req.Email);
        Assert.Equal("123 Đường Lê Lợi", req.DiaChi);
    }

    [Theory]
    [InlineData("", "Tên hợp lệ", "MaKhachHang")]
    [InlineData("   ", "Tên hợp lệ", "MaKhachHang")]
    [InlineData("KH001", "", "TenKhachHang")]
    [InlineData("KH001", "   ", "TenKhachHang")]
    public async Task Test02_ThemKhachHang_ThieuMaHoacTen_BaoLoiTheoTruong(string ma, string ten, string truongLoiExpected)
    {
        var req = new TaoKhachHangRequest
        {
            MaKhachHang = ma,
            TenKhachHang = ten,
            HanMucCongNo = 1000000
        };

        var result = await _themUseCase.ExecuteAsync(req);

        Assert.False(result.ThanhCong);
        Assert.True(result.LoiTheoTruong.ContainsKey(truongLoiExpected));
    }

    [Fact]
    public async Task Test03_ThemKhachHang_DoDaiVuotSchema_BaoLoi()
    {
        var req = new TaoKhachHangRequest
        {
            MaKhachHang = new string('A', 21),       // max 20
            TenKhachHang = new string('B', 151),     // max 150
            SoDienThoai = new string('1', 21),      // max 20
            Email = new string('c', 101),           // max 100
            DiaChi = new string('D', 251),          // max 250
            HanMucCongNo = 1000000
        };

        var result = await _themUseCase.ExecuteAsync(req);

        Assert.False(result.ThanhCong);
        Assert.True(result.LoiTheoTruong.ContainsKey("MaKhachHang"));
        Assert.True(result.LoiTheoTruong.ContainsKey("TenKhachHang"));
        Assert.True(result.LoiTheoTruong.ContainsKey("SoDienThoai"));
        Assert.True(result.LoiTheoTruong.ContainsKey("Email"));
        Assert.True(result.LoiTheoTruong.ContainsKey("DiaChi"));
    }

    [Theory]
    [InlineData(-1000)]
    [InlineData(-0.01)]
    public async Task Test04_ThemKhachHang_HanMucAm_BaoLoi(decimal hanMucAm)
    {
        var req = new TaoKhachHangRequest
        {
            MaKhachHang = "KH_AM",
            TenKhachHang = "Khách hàng hạn mức âm",
            HanMucCongNo = hanMucAm
        };

        var result = await _themUseCase.ExecuteAsync(req);

        Assert.False(result.ThanhCong);
        Assert.True(result.LoiTheoTruong.ContainsKey("HanMucCongNo"));
    }

    [Fact]
    public async Task Test05_ThemKhachHang_HanMucLeHon2ChuSo_BiTuChoi()
    {
        var req = new TaoKhachHangRequest
        {
            MaKhachHang = "KH_LE",
            TenKhachHang = "Khách hàng lẻ tiền",
            HanMucCongNo = 1000.123m // 3 chữ số thập phân
        };

        var result = await _themUseCase.ExecuteAsync(req);

        Assert.False(result.ThanhCong);
        Assert.True(result.LoiTheoTruong.ContainsKey("HanMucCongNo"));
    }

    [Fact]
    public async Task Test06_ThemKhachHang_EmailSaiDinhDang_BaoLoi()
    {
        var req = new TaoKhachHangRequest
        {
            MaKhachHang = "KH_MAIL",
            TenKhachHang = "Khách hàng sai mail",
            Email = "khong-phai-email-hop-le",
            HanMucCongNo = 1000000
        };

        var result = await _themUseCase.ExecuteAsync(req);

        Assert.False(result.ThanhCong);
        Assert.True(result.LoiTheoTruong.ContainsKey("Email"));
    }

    [Fact]
    public async Task Test07_ThemKhachHang_TrungMa_BaoLoiDeHieu()
    {
        _fakeRepo.ExistingCodes.Add("KH_TRUNG");

        var req = new TaoKhachHangRequest
        {
            MaKhachHang = "KH_TRUNG",
            TenKhachHang = "Khách hàng trùng",
            HanMucCongNo = 1000000
        };

        var result = await _themUseCase.ExecuteAsync(req);

        Assert.False(result.ThanhCong);
        Assert.True(result.LoiTheoTruong.ContainsKey("MaKhachHang"));
        Assert.Contains("đã được sử dụng", result.ThongBao);
    }

    [Fact]
    public async Task Test08_CapNhatKhachHang_TrungMaVoiKhachKhac_BiTuChoi()
    {
        _fakeRepo.ExistingCodes.Add("KH_TRUNG_KHAC");

        var req = new CapNhatKhachHangRequest
        {
            KhachHangId = 5,
            MaKhachHang = "KH_TRUNG_KHAC",
            TenKhachHang = "Công ty ABC",
            HanMucCongNo = 5000000
        };

        var result = await _capNhatUseCase.ExecuteAsync(req);

        Assert.False(result.ThanhCong);
        Assert.True(result.LoiTheoTruong.ContainsKey("MaKhachHang"));
    }

    [Fact]
    public async Task Test09_DoiTrangThaiKhachHang_IdKhongHopLe_BaoLoi()
    {
        var result = await _doiTrangThaiUseCase.ExecuteAsync(0, false);
        Assert.False(result.ThanhCong);
    }
}
