using BanHang.UseCases.HoaDonScreen;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BanHang.Tests;

public class HoaDonNhapStateServiceTests
{
    [Fact]
    public void Test01_BanDauChuaCoNhap_CoDuLieuNhapTraVeFalse()
    {
        var service = new HoaDonNhapStateService();

        Assert.False(service.CoDuLieuNhap);

        var data = service.LayDuLieuNhap();
        Assert.NotNull(data);
        Assert.Equal(0, data.KhachHangId);
        Assert.Empty(data.ChiTietDongs);
        Assert.Null(data.GhiChu);
    }

    [Fact]
    public void Test02_CapNhatVaKhoiPhucDuLieuNhap_ChinhXac()
    {
        var service = new HoaDonNhapStateService();
        var ngayLap = new DateTime(2026, 10, 20);
        var hanThanhToan = new DateTime(2026, 11, 5);
        var chiTiets = new List<DongHangNhapDto>
        {
            new() { SanPhamId = 101, SoLuong = 3 },
            new() { SanPhamId = 102, SoLuong = 7 }
        };

        service.CapNhatNhap(
            khachHangId: 15,
            ngayLap: ngayLap,
            hanThanhToan: hanThanhToan,
            ghiChu: "Đơn mua nợ đại lý chi nhánh 2",
            chiTiets: chiTiets);

        Assert.True(service.CoDuLieuNhap);

        var draft = service.LayDuLieuNhap();
        Assert.Equal(15, draft.KhachHangId);
        Assert.Equal(ngayLap, draft.NgayLap);
        Assert.Equal(hanThanhToan, draft.HanThanhToan);
        Assert.Equal("Đơn mua nợ đại lý chi nhánh 2", draft.GhiChu);
        Assert.Equal(2, draft.ChiTietDongs.Count);
        Assert.Equal(101, draft.ChiTietDongs[0].SanPhamId);
        Assert.Equal(3, draft.ChiTietDongs[0].SoLuong);
        Assert.Equal(102, draft.ChiTietDongs[1].SanPhamId);
        Assert.Equal(7, draft.ChiTietDongs[1].SoLuong);
    }

    [Fact]
    public void Test03_ThanhCongHoacHuy_GoiXoaNhap_LamSachTrangThai()
    {
        var service = new HoaDonNhapStateService();
        service.CapNhatNhap(
            khachHangId: 8,
            ngayLap: DateTime.Today,
            hanThanhToan: DateTime.Today.AddDays(30),
            ghiChu: "Tạm lưu",
            chiTiets: new List<DongHangNhapDto> { new() { SanPhamId = 5, SoLuong = 2 } });

        Assert.True(service.CoDuLieuNhap);

        // Lưu thành công hoặc người dùng bấm Hủy -> gọi XoaNhap
        service.XoaNhap();

        Assert.False(service.CoDuLieuNhap);
        var draft = service.LayDuLieuNhap();
        Assert.Equal(0, draft.KhachHangId);
        Assert.Empty(draft.ChiTietDongs);
    }

    [Fact]
    public void Test04_LuuThatBai_GiuNguyenDuLieuNhapDeChinhSua()
    {
        var service = new HoaDonNhapStateService();
        service.CapNhatNhap(
            khachHangId: 20,
            ngayLap: DateTime.Today,
            hanThanhToan: DateTime.Today.AddDays(15),
            ghiChu: "Đơn hàng thử nghiệm giữ nháp khi thất bại",
            chiTiets: new List<DongHangNhapDto> { new() { SanPhamId = 88, SoLuong = 100 } });

        // Khi lưu thất bại (vượt hạn mức, hết hàng...), service KHÔNG gọi XoaNhap()
        Assert.True(service.CoDuLieuNhap);
        var draft = service.LayDuLieuNhap();
        Assert.Equal(20, draft.KhachHangId);
        Assert.Single(draft.ChiTietDongs);
        Assert.Equal(100, draft.ChiTietDongs[0].SoLuong);
    }

    [Fact]
    public void Test05_HaiScopeDocLap_KhongChiaSeDuLieu_BaoDamTinhCoLapCircuit()
    {
        var services = new ServiceCollection();
        services.AddScoped<IHoaDonNhapStateService, HoaDonNhapStateService>();
        var provider = services.BuildServiceProvider();

        // Circuit / Scope 1
        using (var scope1 = provider.CreateScope())
        {
            var state1 = scope1.ServiceProvider.GetRequiredService<IHoaDonNhapStateService>();
            state1.CapNhatNhap(
                khachHangId: 99,
                ngayLap: DateTime.Today,
                hanThanhToan: DateTime.Today.AddDays(7),
                ghiChu: "Nháp Circuit 1",
                chiTiets: new List<DongHangNhapDto> { new() { SanPhamId = 1, SoLuong = 5 } });

            Assert.True(state1.CoDuLieuNhap);
            Assert.Equal(99, state1.LayDuLieuNhap().KhachHangId);
        }

        // Circuit / Scope 2 (hoàn toàn độc lập)
        using (var scope2 = provider.CreateScope())
        {
            var state2 = scope2.ServiceProvider.GetRequiredService<IHoaDonNhapStateService>();

            // Khẳng định Scope 2 không bị ảnh hưởng bởi dữ liệu từ Scope 1 (không dùng static/singleton)
            Assert.False(state2.CoDuLieuNhap);
            var draft2 = state2.LayDuLieuNhap();
            Assert.Equal(0, draft2.KhachHangId);
            Assert.Empty(draft2.ChiTietDongs);
        }
    }

    [Fact]
    public void Test06_VongDoiHuyBo_GoiXoaNhap_SauDoDisposeKhongGhiLaiNhap()
    {
        var service = new HoaDonNhapStateService();

        // 1. Người dùng đang nhập dở dang dữ liệu trên form
        var dongs = new List<DongHangNhapDto> { new() { SanPhamId = 1, SoLuong = 2 } };
        service.CapNhatNhap(10, DateTime.Today, DateTime.Today.AddDays(15), "Ghi chú nháp", dongs);
        Assert.True(service.CoDuLieuNhap);

        // 2. Người dùng nhấn nút 'Hủy bỏ' (HuyBo)
        bool daLuuThanhCong = false;
        bool daHuyBo = false;

        // Mô phỏng hàm HuyBo():
        daHuyBo = true;
        service.XoaNhap();
        // Giả lập NavigationManager.NavigateTo("/hoa-don")

        // 3. Blazor teardown component và kích hoạt Dispose()
        if (!daLuuThanhCong && !daHuyBo)
        {
            service.CapNhatNhap(10, DateTime.Today, DateTime.Today.AddDays(15), "Ghi chú nháp", dongs);
        }

        // 4. Khẳng định: Dispose KHÔNG được phép ghi lại dữ liệu nháp
        Assert.False(service.CoDuLieuNhap);
        var draft = service.LayDuLieuNhap();
        Assert.Equal(0, draft.KhachHangId);
        Assert.Empty(draft.ChiTietDongs);
    }

    [Fact]
    public void Test07_VongDoiLuuThanhCong_GoiXoaNhap_SauDoDisposeKhongGhiLaiNhap()
    {
        var service = new HoaDonNhapStateService();

        // 1. Người dùng nhập dữ liệu dở dang trên form
        var dongs = new List<DongHangNhapDto> { new() { SanPhamId = 2, SoLuong = 5 } };
        service.CapNhatNhap(20, DateTime.Today, DateTime.Today.AddDays(30), "Đơn lưu thành công", dongs);
        Assert.True(service.CoDuLieuNhap);

        // 2. Lưu thành công (LuuHoaDonAsync)
        bool daLuuThanhCong = false;
        bool daHuyBo = false;

        daLuuThanhCong = true;
        service.XoaNhap();
        // Giả lập NavigationManager.NavigateTo("/hoa-don")

        // 3. Blazor teardown component và kích hoạt Dispose()
        if (!daLuuThanhCong && !daHuyBo)
        {
            service.CapNhatNhap(20, DateTime.Today, DateTime.Today.AddDays(30), "Đơn lưu thành công", dongs);
        }

        // 4. Khẳng định: Dispose KHÔNG được phép ghi lại dữ liệu nháp
        Assert.False(service.CoDuLieuNhap);
        var draft = service.LayDuLieuNhap();
        Assert.Equal(0, draft.KhachHangId);
        Assert.Empty(draft.ChiTietDongs);
    }

    [Fact]
    public void Test08_VongDoiLuuThatBai_SauDoDieuHuong_DisposeBaoTonDuLieuNhap()
    {
        var service = new HoaDonNhapStateService();

        // 1. Người dùng nhập dữ liệu dở dang trên form
        var dongs = new List<DongHangNhapDto> { new() { SanPhamId = 3, SoLuong = 10 } };
        service.CapNhatNhap(30, DateTime.Today, DateTime.Today.AddDays(10), "Đơn lưu thất bại do vượt hạn mức", dongs);
        Assert.True(service.CoDuLieuNhap);

        // 2. Lưu thất bại (LuuHoaDonAsync thất bại)
        bool daLuuThanhCong = false;
        bool daHuyBo = false;

        // Lưu thất bại: daLuuThanhCong = false, daHuyBo = false, KHÔNG gọi XoaNhap()
        // Đồng bộ dữ liệu mới nhất (nếu có cập nhật giá snapshot)
        service.CapNhatNhap(30, DateTime.Today, DateTime.Today.AddDays(10), "Đơn lưu thất bại do vượt hạn mức", dongs);

        // 3. Người dùng tạm rời khỏi trang (bấm 'Quay lại danh sách' hoặc sang menu /khach-hang)
        // Blazor kích hoạt Dispose()
        if (!daLuuThanhCong && !daHuyBo)
        {
            service.CapNhatNhap(30, DateTime.Today, DateTime.Today.AddDays(10), "Đơn lưu thất bại do vượt hạn mức", dongs);
        }

        // 4. Khẳng định: Dữ liệu nháp vẫn được BẢO TỒN NGUYÊN VẸN để người dùng sửa lại
        Assert.True(service.CoDuLieuNhap);
        var draft = service.LayDuLieuNhap();
        Assert.Equal(30, draft.KhachHangId);
        Assert.Equal("Đơn lưu thất bại do vượt hạn mức", draft.GhiChu);
        Assert.Single(draft.ChiTietDongs);
        Assert.Equal(3, draft.ChiTietDongs[0].SanPhamId);
        Assert.Equal(10, draft.ChiTietDongs[0].SoLuong);
    }

    [Fact]
    public void Test09_VongDoiDieuHuongNoiBoTrongCungCircuit_KhoiPhucDayDuDuLieu()
    {
        var service = new HoaDonNhapStateService();

        // 1. Người dùng vào /hoa-don/tao, chọn khách hàng ID = 50, thêm 2 sản phẩm
        var dongs = new List<DongHangNhapDto>
        {
            new() { SanPhamId = 11, SoLuong = 2 },
            new() { SanPhamId = 22, SoLuong = 4 }
        };
        service.CapNhatNhap(50, DateTime.Today, DateTime.Today.AddDays(20), "Ghi chú nội bộ", dongs);

        // 2. Người dùng bấm 'Quay lại danh sách' điều hướng sang /hoa-don trong cùng circuit
        bool daLuuThanhCong = false;
        bool daHuyBo = false;

        // Dispose() chạy khi rời trang
        if (!daLuuThanhCong && !daHuyBo)
        {
            service.CapNhatNhap(50, DateTime.Today, DateTime.Today.AddDays(20), "Ghi chú nội bộ", dongs);
        }

        // 3. Người dùng ở /hoa-don rồi quay lại /hoa-don/tao (cùng circuit)
        Assert.True(service.CoDuLieuNhap);
        var restored = service.LayDuLieuNhap();
        Assert.Equal(50, restored.KhachHangId);
        Assert.Equal("Ghi chú nội bộ", restored.GhiChu);
        Assert.Equal(2, restored.ChiTietDongs.Count);
        Assert.Equal(11, restored.ChiTietDongs[0].SanPhamId);
        Assert.Equal(2, restored.ChiTietDongs[0].SoLuong);
        Assert.Equal(22, restored.ChiTietDongs[1].SanPhamId);
        Assert.Equal(4, restored.ChiTietDongs[1].SoLuong);
    }
}
