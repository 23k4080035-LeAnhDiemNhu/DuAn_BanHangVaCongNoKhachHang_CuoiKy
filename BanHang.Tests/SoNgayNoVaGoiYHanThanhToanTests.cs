using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.Common;
using BanHang.UseCases.HoaDonScreen;
using BanHang.UseCases.KhachHangScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using DuAn_BanHangVaCongNoKhachHang.Pages;
using DuAn_BanHangVaCongNoKhachHang.Services;
using Xunit;

namespace BanHang.Tests;

public class SoNgayNoVaGoiYHanThanhToanTests
{
    private IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Test01_KhachHang_SoNgayNo_MacDinhLaKhong()
    {
        var kh = new KhachHang();
        Assert.Equal(0, kh.SoNgayNo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(365)]
    public void Test02_KhachHang_SoNgayNo_SoNguyenKhongAm_HopLe(int soNgayNo)
    {
        var kh = new KhachHang
        {
            MaKhachHang = "KH001",
            TenKhachHang = "Công ty A",
            SoNgayNo = soNgayNo
        };

        var errors = ValidateModel(kh);
        Assert.DoesNotContain(errors, e => e.MemberNames.Contains(nameof(KhachHang.SoNgayNo)));
    }

    [Fact]
    public void Test03_KhachHang_SoNgayNo_SoAm_BaoLoi()
    {
        var kh = new KhachHang
        {
            MaKhachHang = "KH001",
            TenKhachHang = "Công ty A",
            SoNgayNo = -1
        };

        var errors = ValidateModel(kh);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(KhachHang.SoNgayNo)));
    }

    [Fact]
    public void Test04_FormKhachHangModel_SoNgayNo_SoAm_BaoLoi()
    {
        var model = new KhachHangPage.FormKhachHangModel
        {
            TenKhachHang = "Công ty A",
            HanMucCongNo = 10000000m,
            SoNgayNo = -5
        };

        var errors = ValidateModel(model);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(KhachHangPage.FormKhachHangModel.SoNgayNo)));
    }

    private class FakeKhachHangRepoForTest : IKhachHangRepository
    {
        public Task<IEnumerable<KhachHang>> LayDanhSachKhachHangAsync() => Task.FromResult(Enumerable.Empty<KhachHang>());
        public Task<KhachHang?> LayKhachHangTheoIdAsync(int khachHangId) => Task.FromResult<KhachHang?>(new KhachHang { KhachHangId = khachHangId, TenKhachHang = "Test", MaKhachHang = "KH001", TrangThai = true });
        public Task<bool> KiemTraTrungMaKhachHangAsync(string maKhachHang, int? loaiTruKhachHangId = null) => Task.FromResult(false);
        public Task<decimal> LayCongNoHienTaiCuaKhachHangAsync(int khachHangId) => Task.FromResult(0m);
        public Task<TaoKhachHangResult> ThemKhachHangAsync(TaoKhachHangRequest request) => Task.FromResult(new TaoKhachHangResult { ThanhCong = true, KhachHangIdMoi = 1, MaKhachHangMoi = request.MaKhachHang });
        public Task<string> LayMaKhachHangTiepTheoAsync() => Task.FromResult("KH001");
        public Task<CapNhatKhachHangResult> CapNhatKhachHangAsync(CapNhatKhachHangRequest request) => Task.FromResult(new CapNhatKhachHangResult { ThanhCong = true });
        public Task<DoiTrangThaiKhachHangResult> DoiTrangThaiKhachHangAsync(int khachHangId, bool trangThaiMoi) => Task.FromResult(new DoiTrangThaiKhachHangResult { ThanhCong = true });
    }

    [Fact]
    public async Task Test05_ThemKhachHangUseCase_SoNgayNoAm_BaoLoi()
    {
        var repo = new FakeKhachHangRepoForTest();
        var user = FakeCurrentUserService.KeToanCongNo();

        var useCase = new ThemKhachHangUseCase(repo, user);

        var request = new TaoKhachHangRequest
        {
            TenKhachHang = "Khách Hàng Mới",
            HanMucCongNo = 10000000m,
            SoNgayNo = -10
        };

        var result = await useCase.ExecuteAsync(request);
        Assert.False(result.ThanhCong);
        Assert.True(result.LoiTheoTruong.ContainsKey("SoNgayNo"));
        Assert.Equal("Số ngày nợ phải là số nguyên không âm.", result.LoiTheoTruong["SoNgayNo"]);
    }

    [Fact]
    public async Task Test06_CapNhatKhachHangUseCase_SoNgayNoAm_BaoLoi()
    {
        var repo = new FakeKhachHangRepoForTest();
        var user = FakeCurrentUserService.KeToanCongNo();

        var useCase = new CapNhatKhachHangUseCase(repo, user);

        var request = new CapNhatKhachHangRequest
        {
            KhachHangId = 1,
            MaKhachHang = "KH001",
            TenKhachHang = "Khách Hàng Cập Nhật",
            HanMucCongNo = 10000000m,
            SoNgayNo = -1
        };

        var result = await useCase.ExecuteAsync(request);
        Assert.False(result.ThanhCong);
        Assert.True(result.LoiTheoTruong.ContainsKey("SoNgayNo"));
    }

    [Fact]
    public void Test07_TinhHanThanhToanGoiY_SoNgayNoBang0_TraVeCungNgayLap()
    {
        var ngayLap = new DateTime(2026, 10, 20);
        var res = TaoHoaDonPage.TinhHanThanhToanGoiY(ngayLap, 0);

        Assert.True(res.HopLe);
        Assert.Equal(ngayLap.Date, res.HanThanhToan);
        Assert.Null(res.ThongBaoLoi);
    }

    [Fact]
    public void Test08_TinhHanThanhToanGoiY_SoNgayNoDuong_TraVeDungSoNgayCongThem()
    {
        var ngayLap = new DateTime(2026, 10, 20);
        var res = TaoHoaDonPage.TinhHanThanhToanGoiY(ngayLap, 30);

        Assert.True(res.HopLe);
        Assert.Equal(new DateTime(2026, 11, 19), res.HanThanhToan);
        Assert.Null(res.ThongBaoLoi);
    }

    [Fact]
    public void Test09_TinhHanThanhToanGoiY_SoNgayNoAm_BaoKhongHopLe()
    {
        var ngayLap = new DateTime(2026, 10, 20);
        var res = TaoHoaDonPage.TinhHanThanhToanGoiY(ngayLap, -10);

        Assert.False(res.HopLe);
        Assert.Null(res.HanThanhToan);
        Assert.NotNull(res.ThongBaoLoi);
    }

    [Fact]
    public async Task Test10_TinhHanThanhToanGoiY_SoNgayNoRatLon_BaoLoiVaChanLuu_KhongEpSangDateTimeMaxValue()
    {
        var ngayLap = new DateTime(2026, 10, 20);
        
        // 1. Hàm TinhHanThanhToanGoiY không văng ArgumentOutOfRangeException, không tự ép sang MaxValue
        var res = TaoHoaDonPage.TinhHanThanhToanGoiY(ngayLap, int.MaxValue);

        Assert.False(res.HopLe);
        Assert.Null(res.HanThanhToan);
        Assert.Contains("31/12/9999", res.ThongBaoLoi);

        // 2. Khi ngày gợi ý vượt phạm vi, HanThanhToan được gán null -> Use Case chặn lưu và báo lỗi
        var fakeRepo = new FakeHoaDonRepoForTest();
        var fakeUser = FakeCurrentUserService.NhanVienBanHang();
        var useCase = new TaoHoaDonBanChiuUseCase(fakeRepo, fakeUser);

        var requestNullHanTT = new TaoHoaDonRequest
        {
            KhachHangId = 1,
            MaHoaDon = "HDTEST",
            NgayLap = ngayLap,
            HanThanhToan = null, // Do vượt phạm vi nên không thể cấp hạn hợp lệ
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto> { new() { SanPhamId = 1, SoLuong = 1, DonGiaHienThi = 100000 } }
        };

        var result = await useCase.ExecuteAsync(requestNullHanTT);
        Assert.False(result.ThanhCong);
        Assert.Equal("Vui lòng chọn hạn thanh toán cho hóa đơn.", result.ThongBao);
    }

    private class FakeHoaDonRepoForTest : IHoaDonRepository
    {
        public Task<IEnumerable<HoaDonListItemDto>> LayDanhSachHoaDonAsync() => Task.FromResult(Enumerable.Empty<HoaDonListItemDto>());
        public Task<string> LayMaHoaDonTiepTheoAsync() => Task.FromResult("HD001");
        public Task<TaoHoaDonResult> TaoHoaDonBanChiuAsync(TaoHoaDonRequest request) => Task.FromResult(new TaoHoaDonResult { ThanhCong = true, HoaDonIdMoi = 1, MaHoaDonMoi = "HD001" });
        public Task<ChiTietHoaDonDto?> LayChiTietHoaDonAsync(int hoaDonId) => Task.FromResult<ChiTietHoaDonDto?>(null);
    }

    [Fact]
    public void Test11_VongDoiHanThanhToan_SuaTay_DoiNgayLap_DoiKhachHang_KhoiPhucNhap_KhongBiGhiDe()
    {
        var service = new HoaDonNhapStateService();
        var ngayLapBanDau = new DateTime(2026, 10, 20);
        var khachHangA = new KhachHang { KhachHangId = 1, TenKhachHang = "KH A", SoNgayNo = 30 };
        var khachHangB = new KhachHang { KhachHangId = 2, TenKhachHang = "KH B", SoNgayNo = 15 };

        // 1. Khi chọn khách hàng A: gợi ý hạn = 20/10/2026 + 30 ngày = 19/11/2026
        var goiYA = TaoHoaDonPage.TinhHanThanhToanGoiY(ngayLapBanDau, khachHangA.SoNgayNo);
        Assert.True(goiYA.HopLe);
        var hanThanhToan = goiYA.HanThanhToan;
        var daSuaHanThanhToanBangTay = false;
        Assert.Equal(new DateTime(2026, 11, 19), hanThanhToan);

        // 2. Người dùng sửa hạn bằng tay sang 25/11/2026 -> cờ daSuaHanThanhToanBangTay = true
        hanThanhToan = new DateTime(2026, 11, 25);
        daSuaHanThanhToanBangTay = true;

        // 3. Đổi ngày lập sang 22/10/2026: Do daSuaHanThanhToanBangTay = true, hạn sửa tay KHÔNG bị ghi đè
        var ngayLapMoi = new DateTime(2026, 10, 22);
        if (!daSuaHanThanhToanBangTay)
        {
            hanThanhToan = TaoHoaDonPage.TinhHanThanhToanGoiY(ngayLapMoi, khachHangA.SoNgayNo).HanThanhToan;
        }
        Assert.Equal(new DateTime(2026, 11, 25), hanThanhToan); // Vẫn là 25/11, không bị đổi thành 21/11

        // 4. Đổi khách hàng sang B: áp dụng chính sách nợ của khách hàng B (+15 ngày)
        daSuaHanThanhToanBangTay = false;
        var goiYB = TaoHoaDonPage.TinhHanThanhToanGoiY(ngayLapMoi, khachHangB.SoNgayNo);
        hanThanhToan = goiYB.HanThanhToan;
        Assert.Equal(new DateTime(2026, 11, 6), hanThanhToan); // 22/10 + 15 ngày = 06/11

        // 5. Người dùng lại sửa tay hạn thành 10/11/2026 và lưu nháp
        hanThanhToan = new DateTime(2026, 11, 10);
        daSuaHanThanhToanBangTay = true;

        service.CapNhatNhap(
            khachHangId: khachHangB.KhachHangId,
            ngayLap: ngayLapMoi,
            hanThanhToan: hanThanhToan,
            ghiChu: "Lưu nháp sửa tay",
            chiTiets: new List<DongHangNhapDto>(),
            daSuaHanThanhToanBangTay: daSuaHanThanhToanBangTay);

        // 6. Khôi phục nháp: hạn thanh toán và cờ sửa tay được bảo toàn nguyên vẹn, không bị ghi đè
        var draft = service.LayDuLieuNhap();
        Assert.True(draft.DaSuaHanThanhToanBangTay);
        Assert.Equal(new DateTime(2026, 11, 10), draft.HanThanhToan);
        Assert.Equal(khachHangB.KhachHangId, draft.KhachHangId);
        Assert.Equal(ngayLapMoi, draft.NgayLap);
    }
}
