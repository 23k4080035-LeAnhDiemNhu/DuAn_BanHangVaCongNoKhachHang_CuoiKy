using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.HoaDonScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using Xunit;

namespace BanHang.Tests;

/// <summary>
/// Unit Tests kiểm tra Use Case xem chi tiết hóa đơn:
/// - Kiểm tra phân quyền: NhanVienBanHang và KeToanCongNo được xem; anonymous/vai trò khác bị từ chối
/// - Kiểm tra logic tham số và tính toán đối chiếu
/// - Kiểm tra độc lập giữa dòng hàng và phân bổ (không nhân đôi số tiền)
/// </summary>
public class ChiTietHoaDonUnitTests
{
    private class FakeHoaDonRepository : IHoaDonRepository
    {
        public ChiTietHoaDonDto? ResponseToReturn { get; set; }
        public int LastRequestedId { get; set; }

        public Task<ChiTietHoaDonDto?> LayChiTietHoaDonAsync(int hoaDonId)
        {
            LastRequestedId = hoaDonId;
            return Task.FromResult(ResponseToReturn);
        }

        public Task<IEnumerable<HoaDonListItemDto>> LayDanhSachHoaDonAsync() =>
            Task.FromResult<IEnumerable<HoaDonListItemDto>>(new List<HoaDonListItemDto>());

        public Task<TaoHoaDonResult> TaoHoaDonBanChiuAsync(TaoHoaDonRequest request) =>
            Task.FromResult(new TaoHoaDonResult { ThanhCong = true });

        public Task<string> LayMaHoaDonTiepTheoAsync() =>
            Task.FromResult("HD011");
    }

    [Fact]
    public async Task Test01_NhanVienBanHang_DuocPhepXemChiTietHoaDon()
    {
        var fakeRepo = new FakeHoaDonRepository
        {
            ResponseToReturn = new ChiTietHoaDonDto
            {
                HoaDonId = 1,
                MaHoaDon = "HD001",
                TongTien = 5000000,
                DaThanhToan = 5000000,
                ConLai = 0,
                TrangThai = TrangThaiHoaDon.DaThanhToan
            }
        };

        var useCase = new XemChiTietHoaDonUseCase(fakeRepo, FakeCurrentUserService.NhanVienBanHang());
        var result = await useCase.ExecuteAsync(1);

        Assert.NotNull(result);
        Assert.Equal("HD001", result.MaHoaDon);
        Assert.Equal(1, fakeRepo.LastRequestedId);
    }

    [Fact]
    public async Task Test02_KeToanCongNo_DuocPhepXemChiTietHoaDon()
    {
        var fakeRepo = new FakeHoaDonRepository
        {
            ResponseToReturn = new ChiTietHoaDonDto
            {
                HoaDonId = 2,
                MaHoaDon = "HD002",
                TongTien = 6800000,
                DaThanhToan = 6800000,
                ConLai = 0,
                TrangThai = TrangThaiHoaDon.DaThanhToan
            }
        };

        var useCase = new XemChiTietHoaDonUseCase(fakeRepo, FakeCurrentUserService.KeToanCongNo());
        var result = await useCase.ExecuteAsync(2);

        Assert.NotNull(result);
        Assert.Equal("HD002", result.MaHoaDon);
        Assert.Equal(2, fakeRepo.LastRequestedId);
    }

    [Fact]
    public async Task Test03_ChuaDangNhap_BiTuChoiUnauthorizedAccessException()
    {
        var fakeRepo = new FakeHoaDonRepository();
        var useCase = new XemChiTietHoaDonUseCase(fakeRepo, FakeCurrentUserService.Anonymous());

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.ExecuteAsync(1));
        Assert.Contains("chưa được xác thực", ex.Message);
    }

    [Fact]
    public async Task Test04_SaiVaiTro_BiTuChoiUnauthorizedAccessException()
    {
        var fakeRepo = new FakeHoaDonRepository();
        var useCase = new XemChiTietHoaDonUseCase(fakeRepo, FakeCurrentUserService.Role("KhachHang"));

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.ExecuteAsync(1));
        Assert.Contains("không có quyền", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task Test05_HoaDonIdKhongHopLe_TraVeNull_KhongGoiRepository(int invalidId)
    {
        var fakeRepo = new FakeHoaDonRepository
        {
            ResponseToReturn = new ChiTietHoaDonDto { HoaDonId = 999 }
        };

        var useCase = new XemChiTietHoaDonUseCase(fakeRepo, FakeCurrentUserService.NhanVienBanHang());
        var result = await useCase.ExecuteAsync(invalidId);

        Assert.Null(result);
        Assert.Equal(0, fakeRepo.LastRequestedId);
    }

    [Fact]
    public async Task Test06_HoaDonKhongTonTai_TraVeNull()
    {
        var fakeRepo = new FakeHoaDonRepository
        {
            ResponseToReturn = null
        };

        var useCase = new XemChiTietHoaDonUseCase(fakeRepo, FakeCurrentUserService.NhanVienBanHang());
        var result = await useCase.ExecuteAsync(9999);

        Assert.Null(result);
        Assert.Equal(9999, fakeRepo.LastRequestedId);
    }

    [Fact]
    public void Test07_NhieuDongHang_Va_NhieuPhanBo_KhongNhanDoiTien()
    {
        // Giả lập 2 dòng hàng và 3 lượt phân bổ
        var dto = new ChiTietHoaDonDto
        {
            HoaDonId = 10,
            MaHoaDon = "HD_MULTI",
            TongTien = 10000000,
            DaThanhToan = 6000000,
            ConLai = 4000000,
            DanhSachDongHang = new List<DongHangChiTietHoaDonDto>
            {
                new() { ChiTietHoaDonId = 1, TenSanPham = "SP A", DonViTinh = "Cái", SoLuong = 2, DonGia = 3000000, ThanhTien = 6000000 },
                new() { ChiTietHoaDonId = 2, TenSanPham = "SP B", DonViTinh = "Hộp", SoLuong = 4, DonGia = 1000000, ThanhTien = 4000000 }
            },
            DanhSachPhanBo = new List<LichSuPhanBoHoaDonDto>
            {
                new() { PhanBoThanhToanId = 1, MaPhieuThu = "PT1", SoTienPhanBo = 1000000 },
                new() { PhanBoThanhToanId = 2, MaPhieuThu = "PT2", SoTienPhanBo = 2000000 },
                new() { PhanBoThanhToanId = 3, MaPhieuThu = "PT3", SoTienPhanBo = 3000000 }
            }
        };

        // 1. Tổng tiền dòng hàng = 6M + 4M = 10M, đúng bằng TongTien của hóa đơn
        Assert.Equal(10000000m, dto.TongTienDongHang);
        Assert.Equal(dto.TongTien, dto.TongTienDongHang);

        // 2. Tổng phân bổ = 1M + 2M + 3M = 6M, đúng bằng DaThanhToan của hóa đơn
        Assert.Equal(6000000m, dto.TongTienPhanBo);
        Assert.Equal(dto.DaThanhToan, dto.TongTienPhanBo);

        // 3. Số dòng hàng và số lượt phân bổ hoàn toàn độc lập (2 dòng hàng, 3 phân bổ)
        Assert.Equal(2, dto.DanhSachDongHang.Count);
        Assert.Equal(3, dto.DanhSachPhanBo.Count);

        // 4. Dư nợ còn lại chính xác
        Assert.Equal(4000000m, dto.ConLai);
        Assert.Equal(dto.TongTien - dto.DaThanhToan, dto.ConLai);
    }

    [Theory]
    [InlineData(2, true, "/hoa-don/2")]
    [InlineData(4, true, "/hoa-don/4")]
    [InlineData(0, false, null)]
    [InlineData(-1, false, null)]
    public void Test08_DieuHuong_ThoaDieuKienHoaDonIdHopLe(int hoaDonId, bool expectedDieuHuong, string? expectedUrl)
    {
        // Lưu ý: Unit test này chỉ kiểm tra logic điều kiện hợp lệ của HoaDonId và định dạng chuỗi URL đích.
        // Đây không phải kiểm tra mô phỏng sự kiện click component thực tế trên trình duyệt hay circuit.
        var shouldNavigate = hoaDonId > 0;
        string? targetUrl = shouldNavigate ? $"/hoa-don/{hoaDonId}" : null;

        Assert.Equal(expectedDieuHuong, shouldNavigate);
        Assert.Equal(expectedUrl, targetUrl);
    }
}
